// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.ResultIsolation.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        [TestMethod]
        public void RootPostCallbacksReceiveIndependentNestedResultsAndOperations()
        {
            var snapshot = Snapshot();
            var hook = ModuleFixture(snapshot, 211, hook: true);
            var verifier = ModuleFixture(snapshot, 212);
            void AssertOriginalAndMutate(ScriptBuilder script, string role, int replacement)
            {
                AssertPhase(script, role, "postExecute");
                // target result is [[7]]; both callbacks must observe its original nested Array.
                script.Emit(OpCode.LDARG2).EmitPush(0).Emit(OpCode.PICKITEM).EmitPush(0).Emit(OpCode.PICKITEM)
                    .EmitPush(7).Emit(OpCode.NUMEQUAL).Emit(OpCode.ASSERT);
                script.Emit(OpCode.LDARG2).EmitPush(0).Emit(OpCode.PICKITEM).EmitPush(0).EmitPush(replacement).Emit(OpCode.SETITEM);
                script.Emit(OpCode.LDARG2).EmitPush(1).Emit(OpCode.PICKITEM).EmitPush(0).Emit(OpCode.PICKITEM)
                    .EmitPush(1).Emit(OpCode.NUMEQUAL).Emit(OpCode.ASSERT);
                script.Emit(OpCode.LDARG2).EmitPush(1).Emit(OpCode.PICKITEM).EmitPush(0).EmitPush(replacement).Emit(OpCode.SETITEM);
                // The operation argument is also independently deserialized for each callback.
                script.Emit(OpCode.LDARG1).EmitPush(2).Emit(OpCode.PICKITEM).EmitPush(0).Emit(OpCode.PICKITEM)
                    .EmitPush(0).Emit(OpCode.PICKITEM).EmitPush(7).Emit(OpCode.NUMEQUAL).Emit(OpCode.ASSERT);
                script.Emit(OpCode.LDARG1).EmitPush(2).Emit(OpCode.PICKITEM).EmitPush(0).Emit(OpCode.PICKITEM)
                    .EmitPush(0).EmitPush(replacement).Emit(OpCode.SETITEM);
            }
            ReplaceBody(snapshot, hook, "postExecute", script => AssertOriginalAndMutate(script, "hook", 99));
            ReplaceBody(snapshot, verifier, "postExecute", script => AssertOriginalAndMutate(script, "verifier", 88));
            var id = RegisterWithModules(snapshot, verifier, hook);
            using var targetScript = new ScriptBuilder();
            targetScript.Emit(OpCode.INITSLOT, new byte[] { 0, 1 });
            targetScript.EmitPush(2).Emit(OpCode.NEWBUFFER).Emit(OpCode.DUP).EmitPush(0).EmitPush(1).Emit(OpCode.SETITEM);
            targetScript.Emit(OpCode.LDARG0).EmitPush(2).Emit(OpCode.PACK).Emit(OpCode.RET);
            var target = TestUtils.GetContract(targetScript.ToArray(), TestUtils.CreateManifest("result", ContractParameterType.Array, ContractParameterType.Array));
            target.Id = 213; snapshot.AddContract(target.Hash, target);
            var result = (Array)Success(snapshot, "executeUserOp", [id, Operation(target.Hash, "result", [new object[] { 7 }])]);
            Assert.AreEqual(new BigInteger(7), ((Array)result[0])[0].GetInteger());
            Assert.IsInstanceOfType<Neo.VM.Types.Buffer>(result[1]);
            Assert.AreEqual((byte)1, result[1].GetSpan()[0]);
            Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TargetWhitelistCannotWaiveNativeResultOrInspectionFees(bool modules)
        {
            var snapshot = Snapshot();
            var id = RegisterWithModules(snapshot, modules ? ModuleFixture(snapshot, 215) : null,
                modules ? ModuleFixture(snapshot, 216, hook: true) : null);
            var target = TestUtils.GetContract(new byte[] { (byte)OpCode.PUSH7, (byte)OpCode.RET },
                TestUtils.CreateManifest("result", ContractParameterType.Integer));
            target.Id = 217; snapshot.AddContract(target.Hash, target);
            var whitelisted = snapshot.CloneCache();
            var committee = NativeContract.NEO.GetCommittee(whitelisted);
            var committeeHash = Contract.CreateMultiSigContract(committee.Length / 2 + 1, committee).ScriptHash;
            using (var setup = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(committeeHash), whitelisted, settings: Settings))
            {
                setup.LoadScript(new byte[] { (byte)OpCode.RET });
                NativeContract.Policy.SetWhitelistFeeContract(setup, target.Hash, "result", 0, 0);
                setup.SnapshotCache.Commit();
            }
            using var direct = Invoke(snapshot.CloneCache(), "result", [], target: target.Hash);
            using var discountedDirect = Invoke(whitelisted.CloneCache(), "result", [], target: target.Hash);
            using var operation = Invoke(snapshot.CloneCache(), "executeUserOp", [id, Operation(target.Hash, "result", [])]);
            using var discountedOperation = Invoke(whitelisted.CloneCache(), "executeUserOp", [id, Operation(target.Hash, "result", [])]);
            foreach (var engine in new[] { direct, discountedDirect, operation, discountedOperation })
            {
                Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
                Assert.AreEqual(new BigInteger(7), engine.ResultStack.Peek().GetInteger());
            }
            long directDiscount = direct.FeeConsumed - discountedDirect.FeeConsumed;
            long operationDiscount = operation.FeeConsumed - discountedOperation.FeeConsumed;
            Assert.IsGreaterThan(0L, directDiscount, "The target's legitimate fee exemption must remain active.");
            Assert.IsLessThanOrEqualTo(1L, System.Math.Abs(operationDiscount - directDiscount),
                "Only target fees may be waived; native result copies and resumed module inspection must still be charged.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NonSerializableTargetResultFaultsWithoutConsumingNonce(bool cycle)
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            using var targetScript = new ScriptBuilder();
            if (cycle) targetScript.Emit(OpCode.NEWARRAY0).Emit(OpCode.DUP).Emit(OpCode.DUP).Emit(OpCode.APPEND);
            else targetScript.EmitSysCall(ApplicationEngine.System_Storage_GetReadOnlyContext);
            targetScript.Emit(OpCode.RET);
            var target = TestUtils.GetContract(targetScript.ToArray(), TestUtils.CreateManifest("result", ContractParameterType.Any));
            target.Id = 214; snapshot.AddContract(target.Hash, target);
            using var failed = Invoke(snapshot, "executeUserOp", [id, Operation(target.Hash, "result", [])]);
            Assert.AreEqual(VMState.FAULT, failed.State);
            Assert.AreEqual(0, failed.Notifications.Count);
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
            using var again = failed.GetState<SmartAccountInvocationContext>().EnterAccount(id);
        }
    }
}
