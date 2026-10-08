// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartAccountInvocationContext.cs file belongs to the neo project and is free
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
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SmartAccountInvocationContext
    {
        private static UInt160 Address(byte value) => new(Enumerable.Repeat(value, 20).ToArray());
        private static void Rebind(UInt160 hash, byte value)
        {
            MemoryReader reader = new(Address(value).ToArray());
            hash.Deserialize(ref reader);
        }
        private static readonly UInt160 Account = Address(1), OtherAccount = Address(2), Root = Address(3), Child = Address(4);
        private static ApplicationEngine Engine(TriggerType trigger = TriggerType.Application) =>
            ApplicationEngine.Create(trigger, null, TestBlockchain.GetTestSnapshotCache(), settings: TestProtocolSettings.Default, gas: 1_000_000);
        private static SmartAccountInvocationContext Authority(ApplicationEngine engine) => engine.GetState(() => new SmartAccountInvocationContext());
        private static ExecutionContext Context(ApplicationEngine engine, UInt160 hash, ExecutionContext parent = null, bool deployed = true) =>
            engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: s =>
            {
                s.ScriptHash = hash;
                var contract = TestUtils.GetContract(new byte[] { (byte)OpCode.RET }, TestUtils.CreateManifest("call", ContractParameterType.Void));
                contract.Hash = hash;
                s.Contract = deployed ? contract : null;
                s.CallingContext = parent;
            });
        private static void Query(ApplicationEngine engine, ExecutionContext caller) => Context(engine, SmartAccountProtocol.ServiceHash, caller);
        private static bool Module(SmartAccountInvocationContext authority, ApplicationEngine engine, UInt160 account = null,
            UInt160 module = null, SmartAccountModuleKind kind = SmartAccountModuleKind.Verifier,
            SmartAccountCallbackPhase phase = SmartAccountCallbackPhase.Validation) =>
            authority.IsModuleAuthorized(engine, account ?? Account, kind, module ?? Root, phase);

        [TestMethod]
        public void AccountLockRejectsReentryAndOwnsIdentity()
        {
            var authority = new SmartAccountInvocationContext();
            UInt160 identity = Address(1);
            var outer = authority.EnterAccount(identity);
            Rebind(identity, 9);
            Assert.ThrowsExactly<InvalidOperationException>(() => authority.EnterAccount(Account));
            var other = authority.EnterAccount(OtherAccount);
            Assert.ThrowsExactly<InvalidOperationException>(() => outer.Dispose());
            other.Dispose(); outer.Dispose(); outer.Dispose();
            using var again = authority.EnterAccount(Account);
        }

        [TestMethod]
        public void ExactAccountKindPhaseModuleAndCoreQueryAreRequired()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var callback = Context(engine, Root);
            using var grant = authority.EnterModule(Account, SmartAccountModuleKind.Verifier, Root,
                SmartAccountCallbackPhase.Validation, callback.GetState<ExecutionContextState>());
            Assert.IsFalse(Module(authority, engine));
            Assert.IsFalse(authority.IsWitnessAuthorized(engine, SmartAccountProtocol.GetAccountAddress(Account)));
            Query(engine, callback);
            Assert.IsTrue(Module(authority, engine));
            Assert.IsFalse(Module(authority, engine, account: OtherAccount));
            Assert.IsFalse(Module(authority, engine, module: Child));
            Assert.IsFalse(Module(authority, engine, kind: SmartAccountModuleKind.Hook));
            Assert.IsFalse(Module(authority, engine, phase: SmartAccountCallbackPhase.PostExecute));
            Assert.IsFalse(Module(authority, engine, phase: (SmartAccountCallbackPhase)99));
            Assert.IsFalse(Module(authority, engine, kind: (SmartAccountModuleKind)99));
            Assert.IsFalse(authority.IsTargetAuthorized(engine, Account));
            grant.Dispose();
            Assert.IsFalse(Module(authority, engine));
        }

        [TestMethod]
        public void FreshSameHashCallCannotReuseSuspendedRootGrant()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var original = Context(engine, Root);
            using var grant = authority.EnterModule(Account, SmartAccountModuleKind.Verifier, Root,
                SmartAccountCallbackPhase.Validation, original.GetState<ExecutionContextState>());
            Query(engine, Context(engine, Root, original));
            Assert.IsFalse(Module(authority, engine));
            Query(engine, original.Clone(0));
            Assert.IsTrue(Module(authority, engine), "Initialization and internal calls share the original execution state.");
        }

        [TestMethod]
        public void OnlyRegisteredDirectLeafChildrenInheritExecutionPhase()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var original = Context(engine, Root);
            using var grant = authority.EnterModule(Account, SmartAccountModuleKind.Verifier, Root,
                SmartAccountCallbackPhase.Validation, original.GetState<ExecutionContextState>(), [Child]);
            var child = Context(engine, Child, original);
            Query(engine, child);
            Assert.IsTrue(Module(authority, engine, module: Child));
            Query(engine, Context(engine, Child, child));
            Assert.IsFalse(Module(authority, engine, module: Child));
            Query(engine, Context(engine, Child, Context(engine, Address(8), original)));
            Assert.IsFalse(Module(authority, engine, module: Child));
            Query(engine, Context(engine, Address(8), original));
            Assert.IsFalse(Module(authority, engine, module: Address(8)));
            Query(engine, Context(engine, Child, original, deployed: false));
            Assert.IsFalse(Module(authority, engine, module: Child));
        }

        [TestMethod]
        public void ConfigurationAndCleanupNeverDelegateToAnEntireRoster()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var original = Context(engine, Root);
            foreach (var phase in new[] { SmartAccountCallbackPhase.Configuration, SmartAccountCallbackPhase.Cleanup })
            {
                Assert.ThrowsExactly<ArgumentException>(() => authority.EnterModule(Account, SmartAccountModuleKind.Verifier,
                    Root, phase, original.GetState<ExecutionContextState>(), [Child]));
                using var grant = authority.EnterModule(Account, SmartAccountModuleKind.Verifier, Root,
                    phase, original.GetState<ExecutionContextState>());
                Query(engine, original); Assert.IsTrue(Module(authority, engine, phase: phase));
                Query(engine, Context(engine, Child, original)); Assert.IsFalse(Module(authority, engine, module: Child, phase: phase));
            }
        }

        [TestMethod]
        public void TargetGrantIsNotAModuleGrantOrADescendantWitness()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var target = Context(engine, Root);
            using var grant = authority.EnterTarget(Account, Root, target.GetState<ExecutionContextState>());
            UInt160 proxy = SmartAccountProtocol.GetAccountAddress(Account);
            Assert.IsTrue(authority.IsWitnessAuthorized(engine, proxy));
            Assert.IsFalse(authority.IsWitnessAuthorized(engine, SmartAccountProtocol.GetAccountAddress(OtherAccount)));
            Query(engine, target);
            Assert.IsTrue(authority.IsTargetAuthorized(engine, Account));
            Assert.IsFalse(Module(authority, engine));
            Assert.IsFalse(authority.IsWitnessAuthorized(engine, proxy));
            var nested = Context(engine, Root, target);
            Assert.IsFalse(authority.IsWitnessAuthorized(engine, proxy));
            Query(engine, nested);
            Assert.IsFalse(authority.IsTargetAuthorized(engine, Account));
            Query(engine, target);
            grant.Dispose();
            Assert.IsFalse(authority.IsTargetAuthorized(engine, Account));
        }

        [TestMethod]
        public void VerificationOnlyPermitsValidationContext()
        {
            foreach (var trigger in new[] { TriggerType.Verification, TriggerType.OnPersist, TriggerType.PostPersist })
            {
                using var engine = Engine(trigger); var authority = Authority(engine);
                using var account = authority.EnterAccount(Account);
                var caller = Context(engine, Root);
                foreach (var phase in new[] { SmartAccountCallbackPhase.Validation, SmartAccountCallbackPhase.PostExecute, SmartAccountCallbackPhase.Configuration, SmartAccountCallbackPhase.Cleanup })
                {
                    using var grant = authority.EnterModule(Account, SmartAccountModuleKind.Verifier, Root,
                        phase, caller.GetState<ExecutionContextState>());
                    Query(engine, caller);
                    Assert.AreEqual(trigger == TriggerType.Verification && phase == SmartAccountCallbackPhase.Validation,
                        Module(authority, engine, phase: phase));
                }
                using var target = authority.EnterTarget(Account, Root, caller.GetState<ExecutionContextState>());
                Query(engine, caller);
                Assert.IsFalse(authority.IsTargetAuthorized(engine, Account));
                Assert.IsFalse(authority.IsWitnessAuthorized(engine, SmartAccountProtocol.GetAccountAddress(Account)));
            }
        }

        [TestMethod]
        public void NestedAccountsRestoreOuterGrantAndCannotDisposeOutOfOrder()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var outer = authority.EnterAccount(Account);
            var caller = Context(engine, Root);
            using var first = authority.EnterTarget(Account, Root, caller.GetState<ExecutionContextState>());
            using var inner = authority.EnterAccount(OtherAccount);
            var secondCaller = Context(engine, Child, caller);
            using var second = authority.EnterTarget(OtherAccount, Child, secondCaller.GetState<ExecutionContextState>());
            Query(engine, caller);
            Assert.IsFalse(authority.IsTargetAuthorized(engine, Account));
            Assert.ThrowsExactly<InvalidOperationException>(() => first.Dispose());
            Assert.ThrowsExactly<InvalidOperationException>(() => inner.Dispose());
            second.Dispose(); inner.Dispose();
            Query(engine, caller);
            Assert.IsTrue(authority.IsTargetAuthorized(engine, Account));
        }

        [TestMethod]
        public void ResetRevokesAllLeasesAndLateDisposalCannotRevokeNewGrant()
        {
            using var engine = Engine(); var authority = Authority(engine);
            var oldAccount = authority.EnterAccount(Account);
            var caller = Context(engine, Root);
            var oldGrant = authority.EnterTarget(Account, Root, caller.GetState<ExecutionContextState>());
            authority.Reset();
            Query(engine, caller); Assert.IsFalse(authority.IsTargetAuthorized(engine, Account));
            using var newAccount = authority.EnterAccount(Account);
            using var newGrant = authority.EnterTarget(Account, Root, caller.GetState<ExecutionContextState>());
            oldGrant.Dispose(); oldAccount.Dispose();
            Query(engine, caller); Assert.IsTrue(authority.IsTargetAuthorized(engine, Account));
        }

        [TestMethod]
        public void EngineFaultRevokesPendingNativeContinuationAuthority()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var caller = Context(engine, Root);
            using var grant = authority.EnterTarget(Account, Root, caller.GetState<ExecutionContextState>());
            engine.LoadScript(new byte[] { (byte)OpCode.ABORT });
            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Query(engine, caller); Assert.IsFalse(authority.IsTargetAuthorized(engine, Account));
            using var replacement = authority.EnterAccount(Account);
        }

        [TestMethod]
        public void InvalidIdentitiesPhasesKindsAndRostersCannotInstallAuthority()
        {
            using var engine = Engine(); var authority = Authority(engine);
            Assert.ThrowsExactly<ArgumentNullException>(() => authority.EnterAccount(null));
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterAccount(UInt160.Zero));
            var caller = Context(engine, Root); var state = caller.GetState<ExecutionContextState>();
            Assert.ThrowsExactly<InvalidOperationException>(() => authority.EnterTarget(Account, Root, state));
            using var account = authority.EnterAccount(Account);
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterTarget(Account, Child, state));
            Assert.ThrowsExactly<ArgumentNullException>(() => authority.EnterTarget(Account, Root, null));
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterModule(Account, (SmartAccountModuleKind)99, Root, SmartAccountCallbackPhase.Validation, state));
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterModule(Account, SmartAccountModuleKind.Verifier, Root, SmartAccountCallbackPhase.PreExecute, state));
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterModule(Account, SmartAccountModuleKind.Hook, Root, SmartAccountCallbackPhase.Validation, state));
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterModule(Account, SmartAccountModuleKind.Hook, Root, (SmartAccountCallbackPhase)99, state));
            foreach (var roster in new[] { new[] { UInt160.Zero }, new[] { Root }, new[] { Child, Child }, new[] { NativeContract.GAS.Hash }, Enumerable.Range(20, 4).Select(n => Address((byte)n)).ToArray() })
                Assert.ThrowsExactly<ArgumentException>(() => authority.EnterModule(Account, SmartAccountModuleKind.Verifier, Root, SmartAccountCallbackPhase.Validation, state, roster));
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterModule(Account, SmartAccountModuleKind.Hook, Root, SmartAccountCallbackPhase.PreExecute, state, Enumerable.Range(20, 9).Select(n => Address((byte)n)).ToArray()));
            state.Contract = null;
            Assert.ThrowsExactly<ArgumentException>(() => authority.EnterTarget(Account, Root, state));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RealVmTargetGrantExcludesNestedContractsAndDynamicScripts(bool huyao)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var leaf = TestUtils.GetContract(new byte[] { (byte)OpCode.NOP, (byte)OpCode.RET }, TestUtils.CreateManifest("call", ContractParameterType.Void));
            snapshot.AddContract(leaf.Hash, leaf);
            using var code = new ScriptBuilder();
            code.EmitDynamicCall(leaf.Hash, "call").Emit(OpCode.DROP)
                .Emit(OpCode.NEWARRAY0).EmitPush(CallFlags.ReadOnly).EmitPush(new byte[] { (byte)OpCode.NOP, (byte)OpCode.RET })
                .EmitSysCall(ApplicationEngine.System_Runtime_LoadScript).Emit(OpCode.DROP).Emit(OpCode.RET);
            var target = TestUtils.GetContract(code.ToArray(), TestUtils.CreateManifest("call", ContractParameterType.Void));
            snapshot.AddContract(target.Hash, target);
            var diagnostic = new Mock<IDiagnostic>();
            var settings = TestProtocolSettings.Default with { Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyao ? 0U : uint.MaxValue) };
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 100_000_000, diagnostic: diagnostic.Object);
            var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var actual = engine.LoadContract(target, target.Manifest.Abi.Methods[0], CallFlags.All);
            using var grant = authority.EnterTarget(Account, target.Hash, actual.GetState<ExecutionContextState>());
            int rootSteps = 0, leafSteps = 0, dynamicSteps = 0;
            diagnostic.Setup(d => d.PreExecuteInstruction(It.IsAny<Instruction>())).Callback<Instruction>(_ =>
            {
                var state = engine.CurrentContext.GetState<ExecutionContextState>();
                bool allowed = authority.IsWitnessAuthorized(engine, SmartAccountProtocol.GetAccountAddress(Account));
                if (ReferenceEquals(state, actual.GetState<ExecutionContextState>())) { rootSteps++; Assert.IsTrue(allowed); }
                else if (state.Contract is null) { dynamicSteps++; Assert.IsFalse(allowed); }
                else { leafSteps++; Assert.IsFalse(allowed); }
            });
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            Assert.IsGreaterThan(0, rootSteps); Assert.IsGreaterThan(0, leafSteps); Assert.IsGreaterThan(0, dynamicSteps);
        }

        [TestMethod]
        public void MaximumRostersAndHookPhasesRemainUsable()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var caller = Context(engine, Root);
            foreach (var kind in new[] { SmartAccountModuleKind.Verifier, SmartAccountModuleKind.Hook })
            {
                var phase = kind == SmartAccountModuleKind.Hook ? SmartAccountCallbackPhase.PreExecute : SmartAccountCallbackPhase.PostExecute;
                var children = Enumerable.Range(20, kind == SmartAccountModuleKind.Hook ? 8 : 3).Select(n => Address((byte)n)).ToArray();
                using var grant = authority.EnterModule(Account, kind, Root, phase, caller.GetState<ExecutionContextState>(), children);
                Query(engine, Context(engine, children[^1], caller));
                Assert.IsTrue(Module(authority, engine, module: children[^1], kind: kind, phase: phase));
            }
        }

        [TestMethod]
        public void HashArgumentsAndChildRosterCannotRebindALiveGrant()
        {
            using var engine = Engine(); var authority = Authority(engine);
            using var account = authority.EnterAccount(Account);
            var caller = Context(engine, Root);
            UInt160 rootInput = Address(3), childInput = Address(4);
            UInt160[] children = [childInput];
            using var grant = authority.EnterModule(Account, SmartAccountModuleKind.Verifier, rootInput,
                SmartAccountCallbackPhase.Validation, caller.GetState<ExecutionContextState>(), children);
            Rebind(rootInput, 42); Rebind(childInput, 43); children[0] = Address(9);
            Query(engine, Context(engine, Child, caller));
            Assert.IsTrue(Module(authority, engine, module: Child));
            Query(engine, Context(engine, childInput, caller));
            Assert.IsFalse(Module(authority, engine, module: childInput));
        }
    }
}
