// Copyright (C) 2015-2026 The Neo Project.
// Licensed under the MIT software license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        [TestMethod]
        public void RecoveryEpochAbiAndRegisteredStateDomainAreExact()
        {
            var snapshot = Snapshot(); var id = Register(snapshot, Recovery);
            Assert.AreEqual(new BigInteger(2), Success(snapshot, "getVersion", []).GetInteger());
            var state = (Array)Success(snapshot, "getAccount", [id]);
            Assert.AreEqual(14, state.Count);
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getAuthorityEpoch", [id]).GetInteger());
            byte[] domain = Success(snapshot, "getAuthorizationDomain", [id]).GetSpan().ToArray();
            Assert.AreEqual((byte)2, domain[29]);
            Assert.AreEqual(90, domain.Length);
            foreach (string method in new[] { "getAuthorityEpoch", "getAuthorizationDomain" })
            {
                using var unknown = Invoke(snapshot, method, [Next]);
                Assert.AreEqual(VMState.FAULT, unknown.State);
                var abi = NativeContract.AccountManagement.GetContractState(Settings, 1).Manifest.Abi.GetMethod(method, 1);
                Assert.IsTrue(abi.Safe);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RecoveryEpochDetachesFaultingModulesAndRetainsNonceHistory(bool frozen)
        {
            var snapshot = Snapshot();
            var verifier = ModuleFixture(snapshot, 180, composite: true, abortCleanup: true);
            var child = ModuleFixture(snapshot, 181, abortCleanup: true);
            var hook = ModuleFixture(snapshot, 182, hook: true, abortCleanup: true);
            var id = RegisterWithModules(snapshot, verifier, hook);
            object[] configure = [id, child.Hash, "configure", new object[] { 17 }];
            Success(snapshot, "callVerifierChild", configure);
            Success(snapshot, "callVerifierChild", configure, 1000 + SmartAccountState.ModuleChangeDelayMs);
            Success(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])]);
            var otherChannel = new BigInteger(3);
            var op = Operation(NativeContract.StdLib.Hash, "serialize", [8]); op[3] = otherChannel << 64;
            Success(snapshot, "executeUserOp", [id, op]);
            var original = (Array)Success(snapshot, "getAccount", [id]);
            Success(snapshot, "callHook", [id, "configure", new object[] { 19 }]);
            if (frozen) Success(snapshot, "freeze", [id], signers: [Recovery]);
            Success(snapshot, "proposeRecovery", [id, Next], signers: [Recovery]);
            Success(snapshot, "executeRecovery", [id], 1000 + SmartAccountState.CustodyRecoveryDelayMs, []);
            var state = (Array)Success(snapshot, "getAccount", [id]);
            Assert.IsInstanceOfType<Null>(state[5]); Assert.IsInstanceOfType<Null>(state[6]);
            Assert.AreEqual(BigInteger.One, state[13].GetInteger());
            Assert.AreEqual(new BigInteger(frozen ? 1 : 0), state[7].GetInteger());
            Assert.AreSequenceEqual(original[1].GetSpan().ToArray(), state[1].GetSpan().ToArray());
            Assert.AreSequenceEqual(original[2].GetSpan().ToArray(), state[2].GetSpan().ToArray());
            Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, otherChannel]).GetInteger());
            Assert.AreEqual(new BigInteger(17), new BigInteger(snapshot[ConfigKey(child, id)].Value.Span));
            foreach (byte role in new byte[] { 0, 1 })
                foreach (byte prefix in new byte[] { 0x30, 0x40 })
                    Assert.IsFalse(snapshot.Contains(new KeyBuilder(NativeContract.AccountManagement.Id, prefix).Add(id).Add(role)));
            if (frozen)
            {
                Fault(snapshot, "unfreeze", [id], signers: [Next]);
                Fault(snapshot, "unfreeze", [id], signers: [Recovery]);
                Success(snapshot, "unfreeze", [id], signers: [Next, Recovery]);
            }
            Fault(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7], 1)]);
            Success(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7], 1)], signers: [Next]);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void RecoveryEpochDoesNotConsultRevokedCodeOrDependencyStorage(int mutation)
        {
            var snapshot = Snapshot(); var verifier = ModuleFixture(snapshot, 184, abortCleanup: true);
            var id = RegisterWithModules(snapshot, verifier);
            Success(snapshot, "proposeRecovery", [id, Next], signers: [Recovery]);
            if (mutation == 0) snapshot.DeleteContract(verifier.Hash);
            else if (mutation == 1)
            {
                verifier.Manifest.Name += "Changed";
                ReplaceFixture(snapshot, verifier);
            }
            else
                foreach (byte role in new byte[] { 0, 1 })
                    snapshot.Add(new KeyBuilder(NativeContract.AccountManagement.Id, 0x40).Add(id).Add(role), new StorageItem(new byte[] { 0xff }));
            using var engine = Invoke(snapshot, "executeRecovery", [id], 1000 + SmartAccountState.CustodyRecoveryDelayMs, []);
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            Assert.AreEqual(1, engine.Notifications.Count);
            var notification = engine.Notifications.Single();
            Assert.AreEqual("RecoveryExecuted", notification.EventName);
            Assert.AreEqual(5, notification.State.Count);
            Assert.AreEqual(BigInteger.One, notification.State[4].GetInteger());
            engine.SnapshotCache.Commit();
            var state = (Array)Success(snapshot, "getAccount", [id]);
            Assert.IsInstanceOfType<Null>(state[5]);
            foreach (byte role in new byte[] { 0, 1 })
                Assert.IsFalse(snapshot.Contains(new KeyBuilder(NativeContract.AccountManagement.Id, 0x40).Add(id).Add(role)));
        }

        [TestMethod]
        public void RecoveryEpochRevokesPriorDigestWithoutResettingIdentity()
        {
            var snapshot = Snapshot(); var id = Register(snapshot, Recovery);
            var op = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
            var before = Success(snapshot, "getOperationDigest", [id, op]).GetSpan().ToArray();
            Success(snapshot, "proposeRecovery", [id, Next], signers: [Recovery]);
            Success(snapshot, "executeRecovery", [id], 1000 + SmartAccountState.CustodyRecoveryDelayMs, []);
            var after = Success(snapshot, "getOperationDigest", [id, op]).GetSpan().ToArray();
            Assert.IsFalse(before.SequenceEqual(after));
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
        }

        [TestMethod]
        public void RecoveryEpochPolicyChangeRejectsPriorDigestBoundAuthorization()
        {
            var snapshot = Snapshot(); var verifier = ModuleFixture(snapshot, 183);
            ReplaceBody(snapshot, verifier, "validateSignature", script =>
            {
                AssertPhase(script, "verifier", "validation");
                script.Emit(OpCode.LDARG1).Emit(OpCode.LDARG0).EmitPush(2).Emit(OpCode.PACK)
                    .EmitPush(CallFlags.ReadOnly).EmitPush("getOperationDigest").EmitPush(NativeContract.AccountManagement.Hash)
                    .EmitSysCall(ApplicationEngine.System_Contract_Call);
                script.Emit(OpCode.LDARG1).EmitPush(5).Emit(OpCode.PICKITEM).Emit(OpCode.EQUAL);
            });
            var id = RegisterWithModules(snapshot, verifier);
            var op = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
            op[5] = Success(snapshot, "getOperationDigest", [id, op]).GetSpan().ToArray();
            Success(snapshot, "proposeRecoveryAddress", [id, Next]);
            Success(snapshot, "activateRecoveryAddress", [id], 1000 + SmartAccountState.ModuleChangeDelayMs);
            using (var stale = Invoke(snapshot, "executeUserOp", [id, op]))
                Rejected(stale, "exactly Boolean true");
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            op[5] = Success(snapshot, "getOperationDigest", [id, op]).GetSpan().ToArray();
            Success(snapshot, "executeUserOp", [id, op]);
            Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, 0]).GetInteger());
        }
    }
}
