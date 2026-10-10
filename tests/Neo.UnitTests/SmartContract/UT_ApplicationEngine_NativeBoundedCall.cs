// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_NativeBoundedCall.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_ApplicationEngine_NativeBoundedCall
    {
        private static readonly UInt160 Caller = NativeContract.ContractManagement.Hash;
        private static ProtocolSettings Settings(bool huyao = false) => TestProtocolSettings.Default with
        {
            Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_SmartAccountV1, 0)
                .SetItem(Hardfork.HF_Huyao, huyao ? 0U : uint.MaxValue)
        };
        private static Block BlockAt(uint index) => new()
        {
            Header = new Header
            {
                Index = index,
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                Witness = new Witness { InvocationScript = ReadOnlyMemory<byte>.Empty, VerificationScript = ReadOnlyMemory<byte>.Empty }
            },
            Transactions = []
        };
        private static ContractState Add(DataCache snapshot, byte[] script, ContractParameterType result = ContractParameterType.Any, bool safe = false)
        {
            var contract = TestUtils.GetContract(script, TestUtils.CreateManifest("call", result));
            contract.Manifest.Abi.Methods[0].Safe = safe;
            snapshot.DeleteContract(contract.Hash);
            snapshot.AddContract(contract.Hash, contract);
            return contract;
        }
        private static ApplicationEngine Engine(DataCache snapshot, bool huyao = false, CallFlags flags = CallFlags.All, long gas = 100_000_000, TriggerType trigger = TriggerType.Application)
        {
            var engine = ApplicationEngine.Create(trigger, null, snapshot, settings: Settings(huyao), gas: gas);
            engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => { state.ScriptHash = Caller; state.CallFlags = flags; });
            return engine;
        }
        private static ContractTask<StackItem> Call(ApplicationEngine engine, ContractState target, long gas = 100_000, CallFlags flags = CallFlags.All) =>
            engine.CallFromNativeContractWithGasLimitAsync(Caller, target.Hash, "call", flags, gas);
        private static BigInteger Femto(long gas) => gas * ApplicationEngine.FeeFactor * ApplicationEngine.OpcodePriceMultiplier;

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RawReturnAndVoidAreDeliveredOnceWithoutStackLeak(bool huyao)
        {
            foreach (var (opcode, declared, expected) in new[] {
                (OpCode.PUSHF, ContractParameterType.Boolean, StackItemType.Boolean),
                (OpCode.PUSH1, ContractParameterType.Boolean, StackItemType.Integer),
                (OpCode.PUSHNULL, ContractParameterType.Any, StackItemType.Any),
                (OpCode.RET, ContractParameterType.Void, StackItemType.Any) })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache();
                var target = Add(snapshot, [(byte)opcode, (byte)OpCode.RET], declared);
                using var engine = Engine(snapshot, huyao);
                var task = Call(engine, target); int continued = 0;
                task.GetAwaiter().OnCompleted(() => { continued++; Assert.AreEqual(Caller, engine.CurrentScriptHash); });
                Assert.IsFalse(task.GetAwaiter().IsCompleted);
                Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
                Assert.AreEqual(expected, task.GetAwaiter().GetResult().Type);
                Assert.AreEqual(1, continued);
                Assert.AreEqual(0, engine.ResultStack.Count);
            }
        }

        private sealed class EngineWithoutLedger : ApplicationEngine
        {
            internal EngineWithoutLedger(ProtocolSettings settings) : base(TriggerType.Verification, null, null, null, settings, 10_000_000) { }
        }

        [TestMethod]
        public void MissingSettingsOrLedgerCannotActivateSmartAccount()
        {
            using var unknownSettings = new EngineWithoutLedger(null);
            Assert.IsFalse(unknownSettings.IsHardforkEnabled(Hardfork.HF_SmartAccountV1));
            using var unknownHeight = new EngineWithoutLedger(Settings());
            Assert.IsFalse(unknownHeight.IsHardforkEnabled(Hardfork.HF_SmartAccountV1));
        }

        [TestMethod]
        public void NativeDiagnosticIsNotifiedOfBoundedDispatch()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var target = Add(snapshot, [(byte)OpCode.PUSHT, (byte)OpCode.RET]);
            var diagnostic = new Mock<IDiagnostic>();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: Settings(), diagnostic: diagnostic.Object);
            engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => state.ScriptHash = Caller);
            _ = Call(engine, target);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            diagnostic.Verify(d => d.CallFromNative(target.Hash, "call", It.Is<StackItem[]>(args => args.Length == 0)), Times.Once);
        }

        [TestMethod]
        public void CallerIdentityAndArgumentOrderReachCallback()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_GetCallingScriptHash);
            script.EmitPush(3).Emit(OpCode.PACK).Emit(OpCode.RET);
            var target = Add(snapshot, script.ToArray(), ContractParameterType.Array);
            target.Manifest.Abi.Methods[0].Parameters = [new() { Name = "a", Type = ContractParameterType.Integer }, new() { Name = "b", Type = ContractParameterType.Integer }];
            snapshot.DeleteContract(target.Hash); snapshot.AddContract(target.Hash, target);
            using var engine = Engine(snapshot);
            var task = engine.CallFromNativeContractWithGasLimitAsync(Caller, target.Hash, "call", CallFlags.ReadOnly, 100_000, 11, 22);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            var result = (Array)task.GetAwaiter().GetResult();
            Assert.AreSequenceEqual(Caller.ToArray(), result[0].GetSpan().ToArray());
            Assert.AreEqual(new BigInteger(11), result[1].GetInteger());
            Assert.AreEqual(new BigInteger(22), result[2].GetInteger());
        }

        [TestMethod]
        public void OmittedAndFutureActivationFailBeforeScheduling()
        {
            foreach (var settings in new[] { TestProtocolSettings.Default, Settings() with { Hardforks = Settings().Hardforks.SetItem(Hardfork.HF_SmartAccountV1, uint.MaxValue) } })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache(); var target = Add(snapshot, [(byte)OpCode.RET], ContractParameterType.Void);
                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings);
                var root = engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => state.ScriptHash = Caller);
                Assert.ThrowsExactly<InvalidOperationException>(() => Call(engine, target));
                Assert.AreSame(root, engine.CurrentContext);
            }
        }

        [TestMethod]
        [DataRow(TriggerType.Application)]
        [DataRow(TriggerType.Verification)]
        public void PublicBoundedSyscallRejectsFutureHeightWithoutPersistingBlock(TriggerType trigger)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var target = Add(snapshot, [(byte)OpCode.RET], ContractParameterType.Void);
            var settings = Settings() with { Hardforks = Settings().Hardforks.SetItem(Hardfork.HF_SmartAccountV1, uint.MaxValue) };
            using var engine = ApplicationEngine.Create(trigger, null, snapshot, settings: settings);
            using var script = new ScriptBuilder();
            script.Emit(OpCode.NEWARRAY0).EmitPush(1000).EmitPush(CallFlags.ReadOnly).EmitPush("call").EmitPush(target.Hash)
                .EmitSysCall(ApplicationEngine.System_Contract_CallWithGasLimit);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }

        [TestMethod]
        public void ActivationUsesLedgerOrPersistingIndexWithoutChangingLegacyRules()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            uint height = NativeContract.Ledger.CurrentIndex(snapshot);
            foreach (uint activation in new[] { height, height + 1 })
            {
                var settings = Settings() with { Hardforks = Settings().Hardforks.SetItem(Hardfork.HF_SmartAccountV1, activation) };
                using var engine = ApplicationEngine.Create(TriggerType.Verification, null, snapshot.CloneCache(), settings: settings);
                Assert.AreEqual(activation == height, engine.IsHardforkEnabled(Hardfork.HF_SmartAccountV1));
                // Legacy no-block hardfork behavior is deliberately not altered by this profile.
                Assert.IsTrue(engine.IsHardforkEnabled(Hardfork.HF_Huyao));
                using var atBlock = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(),
                    BlockAt(activation), settings);
                Assert.IsTrue(atBlock.IsHardforkEnabled(Hardfork.HF_SmartAccountV1));
                if (activation > 0)
                {
                    using var beforeBlock = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(),
                        BlockAt(activation - 1), settings);
                    Assert.IsFalse(beforeBlock.IsHardforkEnabled(Hardfork.HF_SmartAccountV1));
                }
            }
        }

        [TestMethod]
        public void NonNativeMismatchedCallerAndMissingPermissionsAreRejected()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var target = Add(snapshot, [(byte)OpCode.RET], ContractParameterType.Void);
            using var engine = Engine(snapshot);
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.CallFromNativeContractWithGasLimitAsync(NativeContract.GAS.Hash, target.Hash, "call", CallFlags.All, 1000));
            engine.CurrentContext.GetState<ExecutionContextState>().ScriptHash = target.Hash;
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.CallFromNativeContractWithGasLimitAsync(target.Hash, target.Hash, "call", CallFlags.All, 1000));
            foreach (CallFlags flags in new[] { CallFlags.None, CallFlags.ReadStates, CallFlags.AllowCall })
            {
                using var limited = Engine(snapshot.CloneCache(), flags: flags);
                var before = limited.CurrentContext;
                Assert.ThrowsExactly<InvalidOperationException>(() => Call(limited, target));
                Assert.AreSame(before, limited.CurrentContext);
            }
        }

        [TestMethod]
        public void InvalidLimitsFlagsMethodsAndTargetsCannotScheduleCode()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var target = Add(snapshot, [(byte)OpCode.RET], ContractParameterType.Void);
            using var engine = Engine(snapshot, gas: 10_000_000); var root = engine.CurrentContext;
            foreach (long gas in new[] { 0L, -1L }) Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Call(engine, target, gas));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Call(engine, target, flags: (CallFlags)16));
            Assert.ThrowsExactly<ArgumentException>(() => engine.CallFromNativeContractWithGasLimitAsync(Caller, target.Hash, "_initialize", CallFlags.All, 1));
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.CallFromNativeContractWithGasLimitAsync(Caller, target.Hash, "missing", CallFlags.All, 1));
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.CallFromNativeContractWithGasLimitAsync(Caller, UInt160.Zero, "call", CallFlags.All, 1));
            Assert.AreSame(root, engine.CurrentContext);
            using var excessive = Engine(snapshot.CloneCache(), gas: 10_000_000);
            Assert.ThrowsExactly<InvalidOperationException>(() => Call(excessive, target, 10_000_000));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CallbackLoopExhaustsBudgetBeforeTransaction(bool huyao)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var target = Add(snapshot, [(byte)OpCode.JMP, 0], ContractParameterType.Void);
            using var engine = Engine(snapshot, huyao); int continued = 0;
            var task = Call(engine, target, 1000); task.GetAwaiter().OnCompleted(() => continued++);
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.Contains("bounded contract call gas limit", engine.FaultException.Message);
            Assert.AreEqual(0, continued);
            Assert.IsGreaterThan(0L, engine.GasLeft);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadOnlyAndSafeMethodsCannotWrite(bool safe)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var script = new ScriptBuilder();
            script.EmitPush(1).EmitPush(new byte[] { 7 }).EmitSysCall(ApplicationEngine.System_Storage_GetContext)
                .EmitSysCall(ApplicationEngine.System_Storage_Put).Emit(OpCode.RET);
            var target = Add(snapshot, script.ToArray(), ContractParameterType.Void, safe);
            using var engine = Engine(snapshot);
            var task = Call(engine, target, 10_000_000, safe ? CallFlags.All : CallFlags.ReadOnly);
            Assert.AreEqual(CallFlags.ReadOnly, engine.GetCallFlags());
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsFalse(task.GetAwaiter().IsCompleted);
            Assert.IsNull(snapshot.TryGet(new StorageKey { Id = target.Id, Key = new byte[] { 7 } }));
        }

        [TestMethod]
        public void CallerPermissionsAreNeverElevatedAndVerificationWorks()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var script = new ScriptBuilder(); script.EmitSysCall(ApplicationEngine.System_Contract_GetCallFlags).Emit(OpCode.RET);
            var target = Add(snapshot, script.ToArray(), ContractParameterType.Integer);
            using var engine = Engine(snapshot, flags: CallFlags.ReadOnly, trigger: TriggerType.Verification);
            var task = Call(engine, target, flags: CallFlags.All);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(new BigInteger((byte)CallFlags.ReadOnly), task.GetAwaiter().GetResult().GetInteger());
        }

        [TestMethod]
        public void EveryAncestorAndCallerDispatchChargeAreAccounted()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var target = Add(snapshot, [(byte)OpCode.PUSHT, (byte)OpCode.RET]);
            using var engine = Engine(snapshot);
            var ancestor = new ContractCallGasBudget(Femto(2_000_000), null);
            var parent = new ContractCallGasBudget(Femto(4_000_000), ancestor);
            engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget = parent;
            Assert.ThrowsExactly<InvalidOperationException>(() => Call(engine, target, 1_500_000));
            Assert.IsGreaterThan(BigInteger.Zero, ancestor.Consumed);
            Assert.AreEqual(ancestor.Consumed, parent.Consumed);
            using var valid = Engine(snapshot.CloneCache());
            parent = new ContractCallGasBudget(Femto(4_000_000), null);
            valid.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget = parent;
            var task = Call(valid, target, 1000);
            var child = valid.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget;
            Assert.AreSame(parent, child.Parent); Assert.AreEqual(Femto(1000), child.Limit);
            Assert.AreEqual(BigInteger.Zero, child.Consumed);
            BigInteger dispatch = parent.Consumed; Assert.IsGreaterThan(BigInteger.Zero, dispatch);
            Assert.AreEqual(VMState.HALT, valid.Execute());
            Assert.IsGreaterThan(BigInteger.Zero, child.Consumed);
            Assert.IsTrue(parent.Consumed >= dispatch + child.Consumed);
            Assert.IsTrue(task.GetAwaiter().GetResult().GetBoolean());
        }

        [TestMethod]
        public void InitializationAndLoadScriptCannotEscapeNativeOriginBudget()
        {
            foreach (bool initialize in new[] { true, false })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache();
                using var script = new ScriptBuilder();
                if (initialize) script.Emit(OpCode.JMP, new byte[] { 0 });
                else script.Emit(OpCode.NEWARRAY0).EmitPush(CallFlags.ReadOnly).EmitPush(new byte[] { (byte)OpCode.JMP, 0 }).EmitSysCall(ApplicationEngine.System_Runtime_LoadScript);
                script.Emit(OpCode.RET);
                var target = Add(snapshot, script.ToArray(), ContractParameterType.Void);
                if (initialize)
                {
                    target.Manifest.Abi.Methods[0].Offset = 2;
                    target.Manifest.Abi.Methods = [.. target.Manifest.Abi.Methods, new() { Name = "_initialize", Parameters = [], ReturnType = ContractParameterType.Void, Offset = 0, Safe = false }];
                    snapshot.DeleteContract(target.Hash); snapshot.AddContract(target.Hash, target);
                }
                using var engine = Engine(snapshot);
                _ = Call(engine, target, 2_000_000);
                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.Contains("bounded contract call gas limit", engine.FaultException.Message);
                Assert.IsGreaterThan(0L, engine.GasLeft);
            }
        }

        [TestMethod]
        public void CallTokenSharesNativeOriginBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var leaf = Add(snapshot, [(byte)OpCode.JMP, 0], ContractParameterType.Void);
            var outer = Add(snapshot, [(byte)OpCode.CALLT, 0, 0, (byte)OpCode.RET], ContractParameterType.Void);
            outer.Nef.Tokens = [new() { Hash = leaf.Hash, Method = "call", ParametersCount = 0, HasReturnValue = false, CallFlags = CallFlags.ReadOnly }];
            outer.Nef.CheckSum = NefFile.ComputeChecksum(outer.Nef);
            snapshot.DeleteContract(outer.Hash); snapshot.AddContract(outer.Hash, outer);
            using var engine = Engine(snapshot, true); _ = Call(engine, outer, 2_000_000);
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual(leaf.Hash, engine.CurrentScriptHash);
            Assert.Contains("bounded contract call gas limit", engine.FaultException.Message);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CallbackStateAndContinuationStateRollbackTogether(bool postReturnFault)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var script = new ScriptBuilder();
            if (!postReturnFault) script.EmitPush(1).EmitPush(new byte[] { 9 }).EmitSysCall(ApplicationEngine.System_Storage_GetContext).EmitSysCall(ApplicationEngine.System_Storage_Put);
            script.Emit(OpCode.RET);
            var target = Add(snapshot, script.ToArray(), ContractParameterType.Void);
            using var engine = Engine(snapshot, postReturnFault);
            var key = new StorageKey { Id = NativeContract.ContractManagement.Id, Key = new byte[] { 99 } };
            bool continued = false;
            var task = Call(engine, target, postReturnFault ? 1 : 10_000_000);
            task.GetAwaiter().OnCompleted(() =>
            {
                continued = true;
                engine.SnapshotCache.Add(key, new StorageItem(1));
                engine.SendNotification(Caller, "Continuation", new Array());
                if (!postReturnFault) engine.Throw(new InvalidOperationException("Rejected post-callback result"));
            });
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsTrue(continued);
            Assert.IsNull(snapshot.TryGet(key));
            Assert.IsNull(snapshot.TryGet(new StorageKey { Id = target.Id, Key = new byte[] { 9 } }));
            Assert.AreEqual(0, engine.Notifications.Count);
            Assert.IsGreaterThan(0L, engine.FeeConsumed);
        }

        private static void Whitelist(DataCache snapshot, ContractState contract)
        {
            var committee = NativeContract.NEO.GetCommittee(snapshot);
            var committeeHash = Contract.CreateMultiSigContract(committee.Length / 2 + 1, committee).ScriptHash;
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(committeeHash), snapshot, settings: Settings());
            engine.LoadScript(new byte[] { (byte)OpCode.RET });
            NativeContract.Policy.SetWhitelistFeeContract(engine, contract.Hash, "call", 0, 0);
            engine.SnapshotCache.Commit();
        }

        [TestMethod]
        public void WhitelistedCallbackStillExhaustsSafetyBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var target = Add(snapshot, [(byte)OpCode.JMP, 0], ContractParameterType.Void);
            Whitelist(snapshot, target);
            using var engine = Engine(snapshot); _ = Call(engine, target, 1000);
            var budget = engine.CurrentContext.GetState<ExecutionContextState>().ContractCallGasBudget;
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsGreaterThan(BigInteger.Zero, budget.Consumed);
            Assert.Contains("bounded contract call gas limit", engine.FaultException.Message);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ResumedNativeDispatchUsesCallerNotCompletedChildWhitelist(bool huyao)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var first = Add(snapshot, [(byte)OpCode.PUSH1, (byte)OpCode.RET]);
            var second = Add(snapshot, [(byte)OpCode.PUSH2, (byte)OpCode.RET]);
            Whitelist(snapshot, first);
            using var engine = Engine(snapshot, huyao);
            var task = Call(engine, first);
            long dispatch = -1;
            task.GetAwaiter().OnCompleted(() =>
            {
                long before = engine.FeeConsumed;
                _ = Call(engine, second);
                dispatch = engine.FeeConsumed - before;
            });
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual((long)(ApplicationEngine.System_Contract_CallWithGasLimit.FixedPrice * engine.ExecFeePicoFactor / ApplicationEngine.FeeFactor), dispatch);
        }

        [TestMethod]
        public void FailedContinuationDispatchRestoresReturningInstructionPolicy()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var target = Add(snapshot, [(byte)OpCode.PUSH1, (byte)OpCode.RET]); Whitelist(snapshot, target);
            using var baseline = Engine(snapshot.CloneCache(), true); _ = Call(baseline, target);
            Assert.AreEqual(VMState.HALT, baseline.Execute());
            using var engine = Engine(snapshot.CloneCache(), true);
            var task = Call(engine, target);
            task.GetAwaiter().OnCompleted(() => Assert.ThrowsExactly<InvalidOperationException>(() =>
                engine.CallFromNativeContractWithGasLimitAsync(Caller, target.Hash, "missing", CallFlags.All, 1000)));
            Assert.AreEqual(VMState.HALT, engine.Execute());
            long dispatch = (long)(ApplicationEngine.System_Contract_CallWithGasLimit.FixedPrice * engine.ExecFeePicoFactor / ApplicationEngine.FeeFactor);
            Assert.AreEqual(baseline.FeeConsumed + dispatch, engine.FeeConsumed);
        }

        [TestMethod]
        public void InitializerObservesNativeCallerAndReadOnlyFlags()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_GetCallingScriptHash).EmitPush(Caller).Emit(OpCode.EQUAL).Emit(OpCode.ASSERT);
            script.EmitSysCall(ApplicationEngine.System_Contract_GetCallFlags).EmitPush(CallFlags.ReadOnly).Emit(OpCode.EQUAL).Emit(OpCode.ASSERT).Emit(OpCode.RET);
            int offset = script.Length;
            script.EmitSysCall(ApplicationEngine.System_Runtime_GetCallingScriptHash).Emit(OpCode.RET);
            var target = Add(snapshot, script.ToArray(), ContractParameterType.Hash160);
            target.Manifest.Abi.Methods[0].Offset = offset;
            target.Manifest.Abi.Methods = [.. target.Manifest.Abi.Methods, new() { Name = "_initialize", Parameters = [], ReturnType = ContractParameterType.Void, Offset = 0, Safe = false }];
            snapshot.DeleteContract(target.Hash); snapshot.AddContract(target.Hash, target);
            using var engine = Engine(snapshot);
            var task = Call(engine, target, flags: CallFlags.ReadOnly);
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            Assert.AreSequenceEqual(Caller.ToArray(), task.GetAwaiter().GetResult().GetSpan().ToArray());
        }

        [TestMethod]
        public void NativeGasContinuationInheritsOriginatedBudgetAndRollsBackTransfer()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            UInt160 sender = UInt160.Parse("0x0101010101010101010101010101010101010101");
            var recipient = Add(snapshot, [(byte)OpCode.JMP, 0], ContractParameterType.Void);
            var descriptor = recipient.Manifest.Abi.Methods[0]; descriptor.Name = "onNEP17Payment";
            descriptor.Parameters = [new() { Name = "from", Type = ContractParameterType.Hash160 }, new() { Name = "amount", Type = ContractParameterType.Integer }, new() { Name = "data", Type = ContractParameterType.Any }];
            snapshot.DeleteContract(recipient.Hash); snapshot.AddContract(recipient.Hash, recipient);
            snapshot.Add(new KeyBuilder(NativeContract.GAS.Id, 20).Add(sender), new StorageItem(new AccountState { Balance = 1 }));
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(sender), snapshot, settings: Settings(), gas: 100_000_000);
            engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => state.ScriptHash = Caller);
            _ = engine.CallFromNativeContractWithGasLimitAsync(Caller, NativeContract.GAS.Hash, "transfer", CallFlags.All, 20_000_000,
                sender.ToArray(), recipient.Hash.ToArray(), 1, StackItem.Null);
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual(recipient.Hash, engine.CurrentScriptHash, engine.FaultException?.ToString());
            Assert.Contains("bounded contract call gas limit", engine.FaultException.ToString());
            Assert.AreEqual(BigInteger.One, NativeContract.GAS.BalanceOf(snapshot, sender));
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, recipient.Hash));
            Assert.AreEqual(0, engine.Notifications.Count);
        }

        [TestMethod]
        public void NullHostArgumentsAndBlockedContractsFailClosed()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var target = Add(snapshot, [(byte)OpCode.RET], ContractParameterType.Void);
            using var engine = Engine(snapshot);
            Assert.ThrowsExactly<ArgumentNullException>(() => engine.CallFromNativeContractWithGasLimitAsync(null, target.Hash, "call", CallFlags.All, 1));
            Assert.ThrowsExactly<ArgumentNullException>(() => engine.CallFromNativeContractWithGasLimitAsync(Caller, null, "call", CallFlags.All, 1));
            Assert.ThrowsExactly<ArgumentNullException>(() => engine.CallFromNativeContractWithGasLimitAsync(Caller, target.Hash, null, CallFlags.All, 1));
            Assert.ThrowsExactly<ArgumentNullException>(() => engine.CallFromNativeContractWithGasLimitAsync(Caller, target.Hash, "call", CallFlags.All, 1, null));
            var committee = NativeContract.NEO.GetCommittee(snapshot);
            var committeeHash = Contract.CreateMultiSigContract(committee.Length / 2 + 1, committee).ScriptHash;
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeHash), BlockAt(1),
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = target.Hash });
            using var blocked = Engine(snapshot);
            Assert.ThrowsExactly<InvalidOperationException>(() => Call(blocked, target));
        }

        [TestMethod]
        public void ContinuationCanScheduleAnotherBoundedCallback()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var first = Add(snapshot, [(byte)OpCode.PUSH1, (byte)OpCode.RET]);
            var second = Add(snapshot, [(byte)OpCode.PUSH2, (byte)OpCode.RET]);
            using var engine = Engine(snapshot, true);
            ContractTask<StackItem> secondTask = null;
            var firstTask = Call(engine, first);
            firstTask.GetAwaiter().OnCompleted(() => secondTask = Call(engine, second));
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(BigInteger.One, firstTask.GetAwaiter().GetResult().GetInteger());
            Assert.AreEqual(new BigInteger(2), secondTask.GetAwaiter().GetResult().GetInteger());
            Assert.AreEqual(0, engine.ResultStack.Count);
        }
    }
}
