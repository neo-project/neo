// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.Clock.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        private static void PersistClockBlock(DataCache snapshot, ulong timestamp)
        {
            var block = Block(timestamp);
            block.Header.Index = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
            block.Header.PrevHash = NativeContract.Ledger.CurrentHash(snapshot);
            foreach (var trigger in new[] { TriggerType.OnPersist, TriggerType.PostPersist })
            {
                using var engine = ApplicationEngine.Create(trigger, null, snapshot, block, Settings);
                engine.LoadScript(new byte[] { (byte)OpCode.RET });
                var task = trigger == TriggerType.OnPersist
                    ? NativeContract.Ledger.OnPersistAsync(engine)
                    : NativeContract.Ledger.PostPersistAsync(engine);
                Assert.IsTrue(task.GetAwaiter().IsCompleted);
                task.GetAwaiter().GetResult();
                engine.SnapshotCache.Commit();
            }
            Assert.AreEqual(block.Hash, NativeContract.Ledger.CurrentHash(snapshot));
            Assert.AreEqual(timestamp, NativeContract.Ledger.GetHeader(snapshot, block.Hash).Timestamp);
        }

        private static (ContractState Target, StorageKey Marker) ClockTarget(DataCache snapshot)
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_GetTime).Emit(OpCode.DUP)
                .EmitPush("clock").EmitSysCall(ApplicationEngine.System_Storage_GetContext)
                .EmitSysCall(ApplicationEngine.System_Storage_Put).Emit(OpCode.RET);
            var target = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("recordTime", ContractParameterType.Integer));
            target.Id = 850;
            snapshot.AddContract(target.Hash, target);
            return (target, new StorageKey { Id = target.Id, Key = "clock"u8.ToArray() });
        }

        private static Transaction SignClockExecution(DataCache snapshot, UInt160 id, KeyPair custodyKey,
            UInt160 target, ulong deadline, bool batch)
        {
            var operation = new Array([target.ToArray(), "recordTime", new Array(),
                BigInteger.Zero, new BigInteger(deadline), ByteString.Empty]);
            var state = ReadAccountState(snapshot, id);
            byte[] custodyScript = Contract.CreateSignatureRedeemScript(custodyKey.PublicKey);
            var transaction = new Transaction
            {
                Script = SmartAccountEnvelope.CreateApplicationScript(id, batch ? new Array([operation]) : operation,
                    batch, state.AuthorityEpoch, state.ConfigurationNonce),
                SystemFee = 1_000_000_000,
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + 20,
                Signers = [new Signer { Account = custodyScript.ToScriptHash(), Scopes = WitnessScope.Global },
                    new Signer { Account = SmartAccountProtocol.GetAccountAddress(id), Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = [new Witness { VerificationScript = custodyScript, InvocationScript = new byte[66] },
                    new Witness { VerificationScript = SmartAccountProtocol.CreateVerificationScript(id), InvocationScript = ReadOnlyMemory<byte>.Empty }]
            };
            transaction.NetworkFee = transaction.CalculateNetworkFee(snapshot, Settings);
            using var invocation = new ScriptBuilder();
            transaction.Witnesses[0].InvocationScript = invocation.EmitPush(transaction.Sign(custodyKey, Settings.Network)).ToArray();
            return transaction;
        }

        private static (DataCache Snapshot, UInt160 Id, KeyPair CustodyKey, ContractState Target, StorageKey Marker, ulong Timestamp) ClockAccount()
        {
            var snapshot = Snapshot();
            ulong timestamp = NativeContract.Ledger.GetHeader(snapshot, NativeContract.Ledger.CurrentHash(snapshot)).Timestamp + 10_000;
            PersistClockBlock(snapshot, timestamp);
            byte[] secret = new byte[32];
            secret[^1] = 41;
            var key = new KeyPair(secret);
            var custody = Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash();
            var id = new UInt160(Success(snapshot, "registerAccount",
                [custody, new byte[32], UInt160.Zero, UInt160.Zero, UInt160.Zero], timestamp, [custody]).GetSpan());
            var (target, marker) = ClockTarget(snapshot);
            return (snapshot, id, key, target, marker, timestamp);
        }

        private static BigInteger ClockNonce(DataCache snapshot, UInt160 id)
        {
            using var query = Invoke(snapshot, "getNonce", [id, 0]);
            Assert.AreEqual(VMState.HALT, query.State, query.FaultException?.ToString());
            return query.ResultStack.Pop().GetInteger();
        }

        [TestMethod]
        [TestCategory("Clock")]
        [DataRow(false)]
        [DataRow(true)]
        public void ProxyVerificationUsesLatestPersistedTimestampAndIncludesDeadline(bool batch)
        {
            var (snapshot, id, key, target, marker, timestamp) = ClockAccount();
            var transaction = SignClockExecution(snapshot, id, key, target.Hash, timestamp, batch);
            byte[] raw = transaction.ToArray();

            // VerifyWitnesses creates the actual proxy Verification engine without a proposed block.
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateIndependent(Settings));
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, 150_000_000), "A deadline equal to the ledger timestamp is valid.");
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateDependent(Settings, snapshot, null, []));
            Assert.AreEqual(BigInteger.Zero, ClockNonce(snapshot, id));
            Assert.IsFalse(snapshot.Contains(marker), "Verification must not execute the target.");

            PersistClockBlock(snapshot, timestamp + 1);
            var expired = raw.AsSerializable<Transaction>();
            Assert.AreSequenceEqual(raw, expired.ToArray());
            Assert.AreEqual(VerifyResult.Succeed, expired.VerifyStateIndependent(Settings));
            Assert.IsFalse(expired.VerifyWitnesses(Settings, snapshot, 150_000_000), "The identical transaction expires when the persisted ledger advances by one millisecond.");
            Assert.AreEqual(BigInteger.Zero, ClockNonce(snapshot, id));
            Assert.IsFalse(snapshot.Contains(marker));

            var current = SignClockExecution(snapshot, id, key, target.Hash, timestamp + 1, batch);
            Assert.IsTrue(current.VerifyWitnesses(Settings, snapshot, 150_000_000), "The latest ledger deadline remains inclusive.");
            Assert.AreEqual(BigInteger.Zero, ClockNonce(snapshot, id));
            Assert.IsFalse(snapshot.Contains(marker));
        }

        [TestMethod]
        [TestCategory("Clock")]
        [DataRow(false)]
        [DataRow(true)]
        public void ProxyVerificationFailsClosedWithoutCurrentLedgerHeader(bool batch)
        {
            var (snapshot, id, key, target, marker, timestamp) = ClockAccount();
            var transaction = SignClockExecution(snapshot, id, key, target.Hash, timestamp, batch);
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, 150_000_000));

            snapshot.Delete(new KeyBuilder(NativeContract.Ledger.Id, 5).Add(NativeContract.Ledger.CurrentHash(snapshot)));
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateIndependent(Settings));
            Assert.IsFalse(transaction.VerifyWitnesses(Settings, snapshot, 150_000_000));
            Assert.AreEqual(BigInteger.Zero, ClockNonce(snapshot, id));
            Assert.IsFalse(snapshot.Contains(marker));
        }

        [TestMethod]
        [TestCategory("Clock")]
        [DataRow(false)]
        [DataRow(true)]
        public void VerifiedOperationExpiresAtLaterApplicationBlockWithoutMutation(bool batch)
        {
            var (snapshot, id, key, target, marker, timestamp) = ClockAccount();
            ulong deadline = timestamp + 1;
            var transaction = SignClockExecution(snapshot, id, key, target.Hash, deadline, batch);
            byte[] raw = transaction.ToArray();
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateIndependent(Settings));
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, 150_000_000));
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateDependent(Settings, snapshot, null, []));

            // Execute the same signed bytes on isolated snapshots at either side of the deadline.
            foreach (ulong offset in new ulong[] { 0, 1 })
            {
                var executionSnapshot = snapshot.CloneCache();
                var proposedBlock = Block(deadline + offset);
                proposedBlock.Header.Index = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
                proposedBlock.Header.PrevHash = NativeContract.Ledger.CurrentHash(snapshot);
                var submitted = raw.AsSerializable<Transaction>();
                Assert.AreSequenceEqual(raw, submitted.ToArray());
                using var engine = ApplicationEngine.Create(TriggerType.Application, submitted, executionSnapshot,
                    proposedBlock, Settings, gas: submitted.SystemFee);
                engine.LoadScript(submitted.Script);
                engine.Execute();
                if (offset == 0)
                {
                    Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
                    var result = engine.ResultStack.Pop();
                    Assert.AreEqual(new BigInteger(deadline), (batch ? ((Array)result)[0] : result).GetInteger());
                    Assert.AreEqual(new BigInteger(deadline), new BigInteger(engine.SnapshotCache[marker].Value.Span));
                    Assert.AreEqual(BigInteger.One, ClockNonce(engine.SnapshotCache, id));
                    Assert.AreEqual(1, engine.Notifications.Count);
                }
                else
                {
                    Rejected(engine, "operation has expired");
                    Assert.AreEqual(BigInteger.Zero, ClockNonce(engine.SnapshotCache, id));
                    Assert.IsFalse(engine.SnapshotCache.Contains(marker), "An expired operation must not reach target storage.");
                    Assert.AreEqual(BigInteger.Zero, ClockNonce(executionSnapshot, id));
                    Assert.IsFalse(executionSnapshot.Contains(marker));
                }
            }
            Assert.AreEqual(BigInteger.Zero, ClockNonce(snapshot, id));
            Assert.IsFalse(snapshot.Contains(marker));
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, 150_000_000), "The unchanged ledger still accepts verification after the later application rejected execution.");
        }
    }
}
