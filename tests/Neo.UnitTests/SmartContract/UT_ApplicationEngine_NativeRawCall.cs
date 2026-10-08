// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_NativeRawCall.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine_NativeBoundedCall
    {
        [TestMethod]
        public void RawDiagnosticReportsDispatchOnce()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var target = Add(snapshot, [(byte)OpCode.PUSHT, (byte)OpCode.RET]);
            var diagnostic = new Mock<IDiagnostic>();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: Settings(), diagnostic: diagnostic.Object);
            engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => state.ScriptHash = Caller);
            var task = engine.CallFromNativeContractRawAsync(Caller, target.Hash, "call", CallFlags.All, false);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(task.GetAwaiter().GetResult().GetBoolean());
            diagnostic.Verify(d => d.CallFromNative(target.Hash, "call", It.Is<StackItem[]>(args => args.Length == 0)), Times.Once);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RawDescendantsRetainBudgetInInitializationLoadScriptAndCallToken(bool huyao)
        {
            foreach (string route in new[] { "initialize", "load-script", "call-token" })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache();
                using var script = new ScriptBuilder();
                if (route == "initialize") script.Emit(OpCode.JMP, new byte[] { 0 });
                else if (route == "load-script") script.Emit(OpCode.NEWARRAY0).EmitPush(CallFlags.ReadOnly)
                    .EmitPush(new byte[] { (byte)OpCode.JMP, 0 }).EmitSysCall(ApplicationEngine.System_Runtime_LoadScript);
                else script.Emit(OpCode.CALLT, new byte[] { 0, 0 });
                script.Emit(OpCode.RET);
                var target = Add(snapshot, script.ToArray(), ContractParameterType.Void);
                UInt160 leafHash = null;
                if (route == "initialize")
                {
                    target.Manifest.Abi.Methods[0].Offset = 2;
                    target.Manifest.Abi.Methods = [.. target.Manifest.Abi.Methods, new() { Name = "_initialize", Parameters = [], ReturnType = ContractParameterType.Void, Offset = 0, Safe = false }];
                }
                else if (route == "call-token")
                {
                    var leaf = Add(snapshot, [(byte)OpCode.JMP, 0], ContractParameterType.Void); leafHash = leaf.Hash;
                    target.Nef.Tokens = [new() { Hash = leaf.Hash, Method = "call", ParametersCount = 0, HasReturnValue = false, CallFlags = CallFlags.ReadOnly }];
                    target.Nef.CheckSum = NefFile.ComputeChecksum(target.Nef);
                }
                snapshot.DeleteContract(target.Hash); snapshot.AddContract(target.Hash, target);
                using var engine = Engine(snapshot, huyao);
                var budget = new ContractCallGasBudget(Femto(2_000_000), null);
                engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget = budget;
                _ = engine.CallFromNativeContractRawAsync(Caller, target.Hash, "call", CallFlags.ReadOnly, true);
                Assert.AreEqual(VMState.FAULT, engine.Execute(), route);
                Assert.Contains("bounded contract call gas limit", engine.FaultException.Message);
                Assert.AreSame(budget, engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget);
                if (leafHash is not null) Assert.AreEqual(leafHash, engine.CurrentScriptHash);
                Assert.IsGreaterThan(0L, engine.GasLeft);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RawFailedContinuationRestoresReturningWhitelistPolicy(bool huyao)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var first = Add(snapshot, [(byte)OpCode.PUSH1, (byte)OpCode.RET]); Whitelist(snapshot, first);
            var blocked = Add(snapshot, [(byte)OpCode.PUSH2, (byte)OpCode.RET]);
            var committee = NativeContract.NEO.GetCommittee(snapshot);
            var committeeHash = Contract.CreateMultiSigContract(committee.Length / 2 + 1, committee).ScriptHash;
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeHash), BlockAt(1),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = blocked.Hash });
            using var baseline = Engine(snapshot.CloneCache(), huyao);
            _ = baseline.CallFromNativeContractRawAsync(Caller, first.Hash, "call", CallFlags.All, false);
            Assert.AreEqual(VMState.HALT, baseline.Execute());
            using var engine = Engine(snapshot.CloneCache(), huyao);
            var task = engine.CallFromNativeContractRawAsync(Caller, first.Hash, "call", CallFlags.All, false);
            task.GetAwaiter().OnCompleted(() => Assert.ThrowsExactly<InvalidOperationException>(() =>
                engine.CallFromNativeContractRawAsync(Caller, blocked.Hash, "call", CallFlags.All, false)));
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            long dispatch = (long)(ApplicationEngine.System_Contract_Call.FixedPrice * engine.ExecFeePicoFactor / ApplicationEngine.FeeFactor);
            Assert.AreEqual(baseline.FeeConsumed + dispatch, engine.FeeConsumed);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RawNativeGasContinuationCannotEscapeAncestorBudget(bool huyao)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            UInt160 sender = UInt160.Parse("0x0101010101010101010101010101010101010101");
            var recipient = Add(snapshot, [(byte)OpCode.JMP, 0], ContractParameterType.Void);
            var descriptor = recipient.Manifest.Abi.Methods[0]; descriptor.Name = "onNEP17Payment";
            descriptor.Parameters = [new() { Name = "from", Type = ContractParameterType.Hash160 }, new() { Name = "amount", Type = ContractParameterType.Integer }, new() { Name = "data", Type = ContractParameterType.Any }];
            snapshot.DeleteContract(recipient.Hash); snapshot.AddContract(recipient.Hash, recipient);
            snapshot.Add(new KeyBuilder(NativeContract.GAS.Id, 20).Add(sender), new StorageItem(new AccountState { Balance = 1 }));
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(sender), snapshot, settings: Settings(huyao), gas: 100_000_000);
            engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => state.ScriptHash = Caller);
            var budget = new ContractCallGasBudget(Femto(20_000_000), null);
            engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget = budget;
            _ = engine.CallFromNativeContractRawAsync(Caller, NativeContract.GAS.Hash, "transfer", CallFlags.All, true,
                sender.ToArray(), recipient.Hash.ToArray(), 1, StackItem.Null);
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual(recipient.Hash, engine.CurrentScriptHash, engine.FaultException?.ToString());
            Assert.AreSame(budget, engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget);
            Assert.Contains("bounded contract call gas limit", engine.FaultException.ToString());
            Assert.AreEqual(BigInteger.One, NativeContract.GAS.BalanceOf(snapshot, sender));
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, recipient.Hash));
            Assert.AreEqual(0, engine.Notifications.Count);
        }

        [TestMethod]
        public void RawAdmissionRequiresActivationIdentityFlagsAndExistingBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var target = Add(snapshot, [(byte)OpCode.RET], ContractParameterType.Void);
            using var engine = Engine(snapshot); var root = engine.CurrentContext;
            ContractTask<StackItem> Raw(UInt160 caller, UInt160 hash, string name, CallFlags flags = CallFlags.All, bool inherit = false) =>
                engine.CallFromNativeContractRawAsync(caller, hash, name, flags, inherit);
            Assert.ThrowsExactly<InvalidOperationException>(() => Raw(NativeContract.GAS.Hash, target.Hash, "call"));
            Assert.ThrowsExactly<InvalidOperationException>(() => Raw(Caller, target.Hash, "call", inherit: true));
            foreach (string name in new[] { null, "", "_initialize" })
                Assert.ThrowsExactly<ArgumentException>(() => Raw(Caller, target.Hash, name));
            Assert.ThrowsExactly<ArgumentException>(() => Raw(Caller, target.Hash, "call", (CallFlags)16));
            Assert.ThrowsExactly<InvalidOperationException>(() => Raw(Caller, UInt160.Zero, "call"));
            Assert.ThrowsExactly<InvalidOperationException>(() => Raw(Caller, target.Hash, "missing"));
            engine.CurrentContext.GetState<ExecutionContextState>().ScriptHash = target.Hash;
            Assert.ThrowsExactly<InvalidOperationException>(() => Raw(target.Hash, target.Hash, "call"));
            Assert.AreSame(root, engine.CurrentContext);
            foreach (CallFlags flags in new[] { CallFlags.None, CallFlags.ReadStates, CallFlags.AllowCall })
            {
                using var limited = Engine(snapshot.CloneCache(), flags: flags);
                var original = limited.CurrentContext;
                Assert.ThrowsExactly<InvalidOperationException>(() => limited.CallFromNativeContractRawAsync(Caller, target.Hash, "call", CallFlags.All, false));
                Assert.AreSame(original, limited.CurrentContext);
            }
            foreach (var settings in new[] { TestProtocolSettings.Default, Settings() with { Hardforks = Settings().Hardforks.SetItem(Hardfork.HF_SmartAccountV1, uint.MaxValue) } })
            {
                using var inactive = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), settings: settings);
                var original = inactive.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: s => s.ScriptHash = Caller);
                Assert.ThrowsExactly<InvalidOperationException>(() => inactive.CallFromNativeContractRawAsync(Caller, target.Hash, "call", CallFlags.All, false));
                Assert.AreSame(original, inactive.CurrentContext);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RawDispatchRetainsExactReturnsFlagsAndEveryAncestorBudget(bool huyao)
        {
            foreach (var (opcode, declared, expected) in new[] {
                (OpCode.PUSHF, ContractParameterType.Boolean, StackItemType.Boolean),
                (OpCode.PUSH1, ContractParameterType.Any, StackItemType.Integer),
                (OpCode.PUSHNULL, ContractParameterType.Any, StackItemType.Any),
                (OpCode.RET, ContractParameterType.Void, StackItemType.Any) })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache();
                var target = Add(snapshot, [(byte)opcode, (byte)OpCode.RET], declared);
                using var engine = Engine(snapshot, huyao, CallFlags.ReadOnly);
                var ancestor = new ContractCallGasBudget(Femto(10_000_000), null);
                var parent = new ContractCallGasBudget(Femto(20_000_000), ancestor);
                engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget = parent;
                var task = engine.CallFromNativeContractRawAsync(Caller, target.Hash, "call", CallFlags.All, true);
                Assert.AreSame(parent, engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget);
                Assert.AreEqual(CallFlags.ReadOnly, engine.GetCallFlags());
                Assert.AreEqual(Caller, engine.CallingScriptHash);
                var dispatch = parent.Consumed;
                Assert.IsGreaterThan(BigInteger.Zero, dispatch); Assert.AreEqual(dispatch, ancestor.Consumed);
                int continued = 0; task.GetAwaiter().OnCompleted(() => { continued++; Assert.AreEqual(Caller, engine.CurrentScriptHash); });
                Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
                Assert.AreEqual(1, continued); Assert.AreEqual(expected, task.GetAwaiter().GetResult().Type);
                Assert.IsTrue(parent.Consumed >= dispatch); Assert.AreEqual(parent.Consumed, ancestor.Consumed);
                Assert.AreEqual(0, engine.ResultStack.Count);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RawContinuationDispatchUsesResumedNativeWhitelistPolicy(bool huyao)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var first = Add(snapshot, [(byte)OpCode.PUSH1, (byte)OpCode.RET]);
            var second = Add(snapshot, [(byte)OpCode.PUSH2, (byte)OpCode.RET]);
            Whitelist(snapshot, first);
            using var engine = Engine(snapshot, huyao); long dispatch = -1;
            var task = engine.CallFromNativeContractRawAsync(Caller, first.Hash, "call", CallFlags.All, false);
            ContractTask<StackItem> next = null;
            task.GetAwaiter().OnCompleted(() =>
            {
                long before = engine.FeeConsumed;
                next = engine.CallFromNativeContractRawAsync(Caller, second.Hash, "call", CallFlags.All, false);
                dispatch = engine.FeeConsumed - before;
            });
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            Assert.AreEqual((long)(ApplicationEngine.System_Contract_Call.FixedPrice * engine.ExecFeePicoFactor / ApplicationEngine.FeeFactor), dispatch);
            Assert.AreEqual(new BigInteger(2), next.GetAwaiter().GetResult().GetInteger());
            Assert.AreEqual(0, engine.ResultStack.Count);
        }

        [TestMethod]
        public void RawReadOnlyAndSafeDispatchCannotPublishStorage()
        {
            foreach (bool safe in new[] { true, false })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache(); using var script = new ScriptBuilder();
                script.EmitPush(1).EmitPush(new byte[] { 7 }).EmitSysCall(ApplicationEngine.System_Storage_GetContext)
                    .EmitSysCall(ApplicationEngine.System_Storage_Put).Emit(OpCode.RET);
                var target = Add(snapshot, script.ToArray(), ContractParameterType.Void, safe);
                using var engine = Engine(snapshot, flags: safe ? CallFlags.All : CallFlags.ReadOnly);
                _ = engine.CallFromNativeContractRawAsync(Caller, target.Hash, "call", CallFlags.All, false);
                Assert.AreEqual(CallFlags.ReadOnly, engine.GetCallFlags());
                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.IsNull(snapshot.TryGet(new StorageKey { Id = target.Id, Key = new byte[] { 7 } }));
            }
        }
    }
}
