// Copyright (C) 2015-2026 The Neo Project.
// This file is distributed under the MIT software license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        private static (DataCache Snapshot, UInt160 Id) PendingIntentFixture()
        {
            var snapshot = Snapshot();
            var verifier = ModuleFixture(snapshot, 245);
            var hook = ModuleFixture(snapshot, 246, hook: true);
            var id = RegisterWithModules(snapshot, verifier, hook);
            Success(snapshot, "callVerifier", [id, "configure", new object[] { 7 }]);
            Success(snapshot, "callHook", [id, "configure", new object[] { 8 }]);
            return (snapshot, id);
        }

        private static byte[] PendingIntentBytes(DataCache snapshot, UInt160 id, string role) =>
            BinarySerializer.Serialize(Success(snapshot, "getPendingModuleCall", [id, role]), 8192, 8192);

        private static byte[] GuardedPendingCancellation(UInt160 id, string role, byte[] reviewed)
        {
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.AccountManagement.Hash, "getPendingModuleCall", CallFlags.ReadOnly, id, role);
            script.EmitPush(1).Emit(OpCode.PACK).EmitPush(CallFlags.None).EmitPush("serialize")
                .EmitPush(NativeContract.StdLib.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call);
            script.EmitPush(reviewed).Emit(OpCode.EQUAL).Emit(OpCode.ASSERT);
            script.EmitDynamicCall(NativeContract.AccountManagement.Hash, "cancelModuleCall", CallFlags.All, id, role);
            return script.ToArray();
        }

        private static ApplicationEngine ExecutePendingCancellation(DataCache snapshot, byte[] script, UInt160[] signers = null)
        {
            var transaction = new Transaction
            {
                Version = 0,
                Script = script,
                Signers = (signers ?? [Custody]).Select(account => new Signer
                {
                    Account = account,
                    Scopes = WitnessScope.CustomContracts,
                    AllowedContracts = [NativeContract.AccountManagement.Hash]
                }).ToArray(),
                Attributes = [],
                Witnesses = []
            };
            var engine = ApplicationEngine.Create(TriggerType.Application, transaction, snapshot, Block(1000), Settings, gas: 10_000_000_000);
            engine.LoadScript(script);
            engine.Execute();
            return engine;
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PendingIntentGuardCancelsMatchingContentOnly(bool hook)
        {
            var (snapshot, id) = PendingIntentFixture();
            string role = hook ? "hook" : "verifier", other = hook ? "verifier" : "hook";
            byte[] reviewed = PendingIntentBytes(snapshot, id, role), otherBefore = PendingIntentBytes(snapshot, id, other);
            byte[] accountBefore = ReadAccountState(snapshot, id).Serialize();
            using var engine = ExecutePendingCancellation(snapshot, GuardedPendingCancellation(id, role, reviewed));
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            engine.SnapshotCache.Commit();
            Assert.AreSequenceEqual(new byte[] { 0 }, PendingIntentBytes(snapshot, id, role));
            Assert.AreSequenceEqual(otherBefore, PendingIntentBytes(snapshot, id, other));
            Assert.AreSequenceEqual(accountBefore, ReadAccountState(snapshot, id).Serialize());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PendingIntentGuardRejectsReplacementWithoutAccountChange(bool hook)
        {
            var (snapshot, id) = PendingIntentFixture();
            string role = hook ? "hook" : "verifier", route = hook ? "callHook" : "callVerifier";
            byte[] reviewed = PendingIntentBytes(snapshot, id, role), accountBefore = ReadAccountState(snapshot, id).Serialize();
            byte[] script = GuardedPendingCancellation(id, role, reviewed);
            Success(snapshot, "cancelModuleCall", [id, role]);
            Success(snapshot, route, [id, "configure", new object[] { 99 }]);
            byte[] replacement = PendingIntentBytes(snapshot, id, role);
            Assert.AreNotEqual(Convert.ToHexString(reviewed), Convert.ToHexString(replacement));
            using var engine = ExecutePendingCancellation(snapshot, script);
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.Contains("ASSERT", engine.FaultException?.ToString());
            Assert.AreSequenceEqual(replacement, PendingIntentBytes(snapshot, id, role));
            Assert.AreSequenceEqual(accountBefore, ReadAccountState(snapshot, id).Serialize());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PendingIntentGuardRejectsDeletedContent(bool hook)
        {
            var (snapshot, id) = PendingIntentFixture();
            string role = hook ? "hook" : "verifier", other = hook ? "verifier" : "hook";
            byte[] script = GuardedPendingCancellation(id, role, PendingIntentBytes(snapshot, id, role));
            byte[] otherBefore = PendingIntentBytes(snapshot, id, other);
            Success(snapshot, "cancelModuleCall", [id, role]);
            using var engine = ExecutePendingCancellation(snapshot, script);
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.Contains("ASSERT", engine.FaultException?.ToString());
            Assert.AreSequenceEqual(new byte[] { 0 }, PendingIntentBytes(snapshot, id, role));
            Assert.AreSequenceEqual(otherBefore, PendingIntentBytes(snapshot, id, other));
        }

        [TestMethod]
        public void PendingIntentGuardRejectsWrongRoleWithoutDeletingEitherSlot()
        {
            var (snapshot, id) = PendingIntentFixture();
            byte[] verifier = PendingIntentBytes(snapshot, id, "verifier"), hook = PendingIntentBytes(snapshot, id, "hook");
            using var engine = ExecutePendingCancellation(snapshot, GuardedPendingCancellation(id, "hook", verifier));
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.Contains("ASSERT", engine.FaultException?.ToString());
            Assert.AreSequenceEqual(verifier, PendingIntentBytes(snapshot, id, "verifier"));
            Assert.AreSequenceEqual(hook, PendingIntentBytes(snapshot, id, "hook"));
        }

        [TestMethod]
        public void PendingIntentGuardStillRequiresCustody()
        {
            var (snapshot, id) = PendingIntentFixture();
            byte[] reviewed = PendingIntentBytes(snapshot, id, "verifier");
            using var engine = ExecutePendingCancellation(snapshot, GuardedPendingCancellation(id, "verifier", reviewed), []);
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.Contains("custody", engine.FaultException?.ToString());
            Assert.AreSequenceEqual(reviewed, PendingIntentBytes(snapshot, id, "verifier"));
        }

        [TestMethod]
        public void PendingIntentGuardCancellationRollsBackWithApplicationFault()
        {
            var (snapshot, id) = PendingIntentFixture();
            byte[] reviewed = PendingIntentBytes(snapshot, id, "verifier");
            byte[] script = [.. GuardedPendingCancellation(id, "verifier", reviewed), (byte)OpCode.ABORT];
            using var engine = ExecutePendingCancellation(snapshot, script);
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.Contains("ABORT", engine.FaultException?.ToString());
            Assert.AreSequenceEqual(reviewed, PendingIntentBytes(snapshot, id, "verifier"));
        }

        [TestMethod]
        public void PendingIntentWithoutGuardCanCancelReplacementContent()
        {
            var (snapshot, id) = PendingIntentFixture();
            byte[] reviewed = PendingIntentBytes(snapshot, id, "verifier");
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.AccountManagement.Hash, "cancelModuleCall", CallFlags.All, id, "verifier");
            Success(snapshot, "cancelModuleCall", [id, "verifier"]);
            Success(snapshot, "callVerifier", [id, "configure", new object[] { 99 }]);
            Assert.AreNotEqual(Convert.ToHexString(reviewed), Convert.ToHexString(PendingIntentBytes(snapshot, id, "verifier")));
            using var engine = ExecutePendingCancellation(snapshot, script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            engine.SnapshotCache.Commit();
            Assert.AreSequenceEqual(new byte[] { 0 }, PendingIntentBytes(snapshot, id, "verifier"));
        }

        [TestMethod]
        public void PendingIntentGuardDefinesContentIdentityAcrossSameBlockReproposal()
        {
            var (snapshot, id) = PendingIntentFixture();
            byte[] reviewed = PendingIntentBytes(snapshot, id, "verifier");
            Success(snapshot, "cancelModuleCall", [id, "verifier"]);
            Success(snapshot, "callVerifier", [id, "configure", new object[] { 7 }]);
            Assert.AreSequenceEqual(reviewed, PendingIntentBytes(snapshot, id, "verifier"));
            using var engine = ExecutePendingCancellation(snapshot, GuardedPendingCancellation(id, "verifier", reviewed));
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            engine.SnapshotCache.Commit();
            Assert.AreSequenceEqual(new byte[] { 0 }, PendingIntentBytes(snapshot, id, "verifier"));
        }

        [TestMethod]
        public void PendingIntentGuardExecutesExactSdkVector()
        {
            // Exact bytes emitted by NativeSmartAccountClient.buildAction for its deterministic transaction fixture.
            const string sdkScript = "0c0876657269666965720c14111111111111111111111111111111111111111112c0150c1467657450656e64696e674d6f64756c6543616c6c0c144117a67f088e2ea046e74bdce906f2ad071d42d941627d5b5211c0100c0973657269616c697a650c14c0ef39cee0e4e925c6c2a06a79e1440dd86fceac41627d5b520cc3400a2101012814111111111111111111111111111111111111111121004002281455555555555555555555555555555555555555552820aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa4002281455555555555555555555555555555555555555552820aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2809736574436f6e6669674002281411111111111111111111111111111111111111112101012101012104015c260521010b97390c0876657269666965720c14111111111111111111111111111111111111111112c01f0c1063616e63656c4d6f64756c6543616c6c0c144117a67f088e2ea046e74bdce906f2ad071d42d941627d5b52";
            const string sdkPending = "400a2101012814111111111111111111111111111111111111111121004002281455555555555555555555555555555555555555552820aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa4002281455555555555555555555555555555555555555552820aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2809736574436f6e6669674002281411111111111111111111111111111111111111112101012101012104015c260521010b";
            var snapshot = Snapshot();
            var id = UInt160.Parse(new string('1', 40));
            var custody = UInt160.Parse(new string('2', 40));
            var module = UInt160.Parse(new string('5', 40));
            Array Binding() => new([module.ToArray(), Convert.FromHexString(new string('a', 64))]);
            var record = new Array([2, id.ToArray(), SmartAccountProtocol.GetAccountAddress(id).ToArray(), custody.ToArray(),
                UInt160.Parse(new string('6', 40)).ToArray(), Binding(), Binding(), 0, 11,
                StackItem.Null, StackItem.Null, StackItem.Null, StackItem.Null, 7]);
            snapshot.Add(new StorageKey { Id = NativeContract.AccountManagement.Id, Key = SmartAccountProtocol.GetAccountKey(id) },
                new StorageItem(BinarySerializer.Serialize(record, 8192, 8192)));
            var key = new KeyBuilder(NativeContract.AccountManagement.Id, 0x30).Add(id).Add((byte)0);
            snapshot.Add(key, new StorageItem(Convert.FromHexString(sdkPending)));
            Assert.AreSequenceEqual(Convert.FromHexString(sdkPending), PendingIntentBytes(snapshot, id, "verifier"));
            Assert.AreSequenceEqual(Convert.FromHexString(sdkScript), GuardedPendingCancellation(id, "verifier", Convert.FromHexString(sdkPending)));
            using var engine = ExecutePendingCancellation(snapshot, Convert.FromHexString(sdkScript), [custody]);
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            engine.SnapshotCache.Commit();
            Assert.IsFalse(snapshot.Contains(key));
            Assert.AreEqual(11UL, ReadAccountState(snapshot, id).ConfigurationNonce);
        }
    }
}
