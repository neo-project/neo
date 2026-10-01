// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_BoundedCall.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System.Collections.Immutable;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine_BoundedCall
    {
        [TestMethod]
        public void CallWithGasLimit_IsInactiveBeforeSmartAccountActivation()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, UInt160.Zero, "test", CallFlags.ReadOnly, 1L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }

        [TestMethod]
        public void CallWithGasLimit_RejectsNonPositiveAndExcessiveLimits()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();

            foreach (long limit in new[] { 0L, -1L })
            {
                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), settings: settings, gas: 100);
                using var script = new ScriptBuilder();
                EmitBoundedCall(script, UInt160.Zero, "test", CallFlags.ReadOnly, limit);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(VMState.FAULT, engine.Execute());
            }

            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), settings: settings, gas: 100))
            using (var script = new ScriptBuilder())
            {
                EmitBoundedCall(script, UInt160.Zero, "test", CallFlags.ReadOnly, 101L);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(VMState.FAULT, engine.Execute());
            }
        }

        [TestMethod]
        public void CallWithGasLimit_StopsAnUnboundedCalleeWithoutConsumingTransactionBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0xfe }, "loop", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, callee.Hash, "loop", CallFlags.ReadOnly, 1L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.Contains("gas limit", engine.FaultException?.Message ?? string.Empty);
            Assert.IsLessThan(1000_00000000L, engine.FeeConsumed);
        }

        [TestMethod]
        public void CallWithGasLimit_PropagatesBudgetThroughAnOrdinaryNestedCall()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0xfe }, "loop", ContractParameterType.Void);
            using var outerScript = new ScriptBuilder();
            EmitOrdinaryCall(outerScript, callee.Hash, "loop", CallFlags.ReadOnly);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, outer.Hash, "call", CallFlags.ReadOnly, 100L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.Contains("gas limit", engine.FaultException?.Message ?? string.Empty);
        }

        [TestMethod]
        public void CallWithGasLimit_WhitelistedCalleeStillConsumesItsBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0xfe }, "loop", ContractParameterType.Void);

            using (var setupEngine = CreateEngineWithCommitteeSigner(snapshot))
            {
                NativeContract.Policy.SetWhitelistFeeContract(setupEngine, callee.Hash, "loop", 0, 0);
                setupEngine.SnapshotCache.Commit();
            }

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, callee.Hash, "loop", CallFlags.ReadOnly, 1L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.Contains("gas limit", engine.FaultException?.Message ?? string.Empty);
        }

        [TestMethod]
        public void CallWithGasLimit_PreservesTheCalleeReturnValue()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.RET }, "value", ContractParameterType.Boolean);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, callee.Hash, "value", CallFlags.ReadOnly, 100L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
        }

        [TestMethod]
        public void CallWithGasLimit_NestedBoundedCallCannotExceedParentBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0xfe }, "loop", ContractParameterType.Void);
            using var outerScript = new ScriptBuilder();
            EmitBoundedCall(outerScript, callee.Hash, "loop", CallFlags.ReadOnly, 2L);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, outer.Hash, "call", CallFlags.ReadOnly, 1L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.Contains("gas limit", engine.FaultException?.Message ?? string.Empty);
        }

        private static ProtocolSettings SmartAccountSettings()
        {
            return TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_SmartAccountV1, 0)
            };
        }

        private static ContractState AddContract(DataCache snapshot, byte[] script, string method, ContractParameterType returnType)
        {
            var contract = TestUtils.GetContract(script, TestUtils.CreateManifest(method, returnType));
            snapshot.DeleteContract(contract.Hash);
            snapshot.AddContract(contract.Hash, contract);
            return contract;
        }

        private static void EmitBoundedCall(ScriptBuilder script, UInt160 contract, string method, CallFlags flags, long gasLimit)
        {
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.PACK);
            script.EmitPush(gasLimit);
            script.EmitPush(flags);
            script.EmitPush(method);
            script.EmitPush(contract);
            script.EmitSysCall(ApplicationEngine.System_Contract_CallWithGasLimit);
        }

        private static void EmitOrdinaryCall(ScriptBuilder script, UInt160 contract, string method, CallFlags flags)
        {
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.PACK);
            script.EmitPush(flags);
            script.EmitPush(method);
            script.EmitPush(contract);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);
        }

        private static ApplicationEngine CreateEngineWithCommitteeSigner(DataCache snapshot)
        {
            var committee = NativeContract.NEO.GetCommittee(snapshot);
            var committeeContract = Contract.CreateMultiSigContract((committee.Length / 2) + 1, committee);
            var tx = new Neo.Network.P2P.Payloads.Transaction
            {
                Version = 0,
                Nonce = 1,
                Signers = [new() { Account = committeeContract.ScriptHash, Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = [new Witness { InvocationScript = new byte[1], VerificationScript = committeeContract.Script }],
                Script = new byte[] { (byte)OpCode.NOP },
                NetworkFee = 0,
                SystemFee = 0,
                ValidUntilBlock = 0
            };
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Gorgon, 1).SetItem(Hardfork.HF_Huyao, uint.MaxValue)
            };
            var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: settings);
            engine.LoadScript(tx.Script);
            return engine;
        }
    }
}
