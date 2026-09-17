// Copyright (C) 2015-2026 The Neo Project.
//
// UT_HardforkDualContractMethods.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_HardforkDualContractMethods
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void SetExecFeeFactorV0_RejectsAboveMaxExecFeeFactor()
        {
            using var engine = CommitteeEngine();
            var ex = Assert.Throws<TargetInvocationException>(() =>
                Invoke(NativeContract.Policy, "SetExecFeeFactorV0", engine, 101ul));
            Assert.IsInstanceOfType<ArgumentOutOfRangeException>(ex.InnerException);

            Invoke(NativeContract.Policy, "SetExecFeeFactorV0", engine, 50ul);
        }

        [TestMethod]
        public void SetExecFeeFactorV1_AllowsAboveMaxExecFeeFactor()
        {
            using var engine = CommitteeEngine();
            Invoke(NativeContract.Policy, "SetExecFeeFactorV1", engine, 101ul);
            var tooLarge = (ulong)(ApplicationEngine.FeeFactor * PolicyContract.MaxExecFeeFactor) + 1;
            var ex = Assert.Throws<TargetInvocationException>(() =>
                Invoke(NativeContract.Policy, "SetExecFeeFactorV1", engine, tooLarge));
            Assert.IsInstanceOfType<ArgumentOutOfRangeException>(ex.InnerException);
        }

        [TestMethod]
        public void BlockAccountV0_StoresEmptyPayload()
        {
            using var engine = CommitteeEngine();
            var account = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            var ok = Invoke<bool>(NativeContract.Policy, "BlockAccountV0", engine, account);
            Assert.IsTrue(ok);
            Assert.AreEqual(0, engine.SnapshotCache[StorageKey.Create(NativeContract.Policy.Id, 15, account)].Value.Length);
        }

        [TestMethod]
        public void BlockAccountV1_StoresTimestamp()
        {
            using var engine = CommitteeEngine();
            var account = UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            var ok = Invoke<bool>(NativeContract.Policy, "BlockAccountV1", engine, account);
            Assert.IsTrue(ok);
            Assert.IsGreaterThan(0, engine.SnapshotCache[StorageKey.Create(NativeContract.Policy.Id, 15, account)].Value.Length);
        }

        [TestMethod]
        public void BlockAccountInternal_TwoArgOverload()
        {
            using var engine = CommitteeEngine();
            var account = UInt160.Parse("0xc400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            var ok = NativeContract.Policy.BlockAccountInternal(engine, account).GetAwaiter().GetResult();
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void DesignateAsRoleV0_NotificationHasTwoStates()
        {
            AssertDesignate(includeNodeLists: false, expectedStates: 2);
        }

        [TestMethod]
        public void DesignateAsRoleV1_NotificationHasFourStates()
        {
            AssertDesignate(includeNodeLists: true, expectedStates: 4);
        }

        [TestMethod]
        public void RegisterCandidateV0_RejectsMissingWitnessBeforeFee()
        {
            using var engine = Engine(UInt160.Zero);
            var pubkey = TestProtocolSettings.Default.StandbyValidators[0];
            var before = engine.FeeConsumed;
            var ok = Invoke<bool>(NativeContract.NEO, "RegisterCandidateV0", engine, pubkey);
            Assert.IsFalse(ok);
            Assert.AreEqual(before, engine.FeeConsumed);
        }

        [TestMethod]
        public void RegisterCandidateV1_ChargesFeeBeforeWitnessCheck()
        {
            using var engine = Engine(UInt160.Zero, gas: 10_000_00000000);
            var pubkey = TestProtocolSettings.Default.StandbyValidators[0];
            var before = engine.FeeConsumed;
            var ok = Invoke<bool>(NativeContract.NEO, "RegisterCandidateV1", engine, pubkey);
            Assert.IsFalse(ok);
            Assert.IsGreaterThan(before, engine.FeeConsumed);
        }

        [TestMethod]
        public void RegisterCandidateV0AndV1_SucceedWithWitness()
        {
            var pubkey = TestProtocolSettings.Default.StandbyValidators[0];
            var hash = Contract.CreateSignatureRedeemScript(pubkey).ToScriptHash();
            using (var engine = Engine(hash, gas: 10_000_00000000))
            {
                Assert.IsTrue(Invoke<bool>(NativeContract.NEO, "RegisterCandidateV0", engine, pubkey));
            }
            using (var engine = Engine(hash, gas: 10_000_00000000))
            {
                Assert.IsTrue(Invoke<bool>(NativeContract.NEO, "RegisterCandidateV1", engine, pubkey));
            }
        }

        private void AssertDesignate(bool includeNodeLists, int expectedStates)
        {
            using var engine = CommitteeEngine();
            var notifications = new List<NotifyEventArgs>();
            engine.Notify += (_, e) => notifications.Add(e);
            var nodes = TestProtocolSettings.Default.StandbyValidators.Take(2).ToArray();
            var name = includeNodeLists ? "DesignateAsRoleV1" : "DesignateAsRoleV0";
            Invoke(NativeContract.RoleManagement, name, engine, Role.Oracle, nodes);
            Assert.HasCount(1, notifications);
            Assert.AreEqual("Designation", notifications[0].EventName);
            Assert.HasCount(expectedStates, notifications[0].State);
            Assert.IsInstanceOfType<Array>(notifications[0].State);
        }

        private ApplicationEngine CommitteeEngine()
        {
            var snapshot = _snapshotCache.CloneCache();
            return Engine(NativeContract.NEO.GetCommitteeAddress(snapshot), snapshot);
        }

        private ApplicationEngine Engine(UInt160 signer, DataCache snapshot = null, long gas = 10_000_00000000)
        {
            snapshot ??= _snapshotCache.CloneCache();
            var block = new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 0,
                    Timestamp = 1,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                Signers = [new() { Account = signer, Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = [new Witness { InvocationScript = new byte[] { 1 }, VerificationScript = System.Array.Empty<byte>() }],
                Script = new byte[] { (byte)OpCode.NOP },
                NetworkFee = 0,
                SystemFee = 0,
                ValidUntilBlock = 0
            };
            var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, block,
                settings: TestProtocolSettings.Default, gas: gas);
            engine.LoadScript(tx.Script);
            return engine;
        }

        private static void Invoke(object instance, string name, params object[] args)
        {
            var result = InvokeRaw(instance, name, args);
            if (result is ContractTask task)
                task.GetAwaiter().GetResult();
        }

        private static T Invoke<T>(object instance, string name, params object[] args)
        {
            var result = InvokeRaw(instance, name, args);
            if (result is ContractTask<T> typed)
                return typed.GetAwaiter().GetResult();
            if (result is ContractTask task)
            {
                task.GetAwaiter().GetResult();
                return default!;
            }
            return (T)result!;
        }

        private static object InvokeRaw(object instance, string name, object[] args)
        {
            var method = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, name);
            return method.Invoke(instance, args);
        }
    }
}
