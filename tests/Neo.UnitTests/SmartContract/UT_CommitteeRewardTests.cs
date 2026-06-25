// Copyright (C) 2015-2026 The Neo Project.
//
// UT_CommitteeRewardTests.cs file belongs to the neo project and is free
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
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_CommitteeRewardTests
    {
        private const byte PrefixCommittee = 14;              // NeoToken central cached committee prefix
        private const byte PrefixCandidate = 33;              // NeoToken registered candidates prefix
        private const byte PrefixVoterRewardPerCommittee = 23; // NeoToken cumulative rewards prefix
        private const long VoteFactor = 100_000_000;
        private NeoSystem _system;

        [TestInitialize]
        public void Setup()
        {
            var validatorKey = new KeyPair(Convert.FromHexString("0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20"));
            var settings = ProtocolSettings.Default with
            {
                StandbyCommittee = [validatorKey.PublicKey],
                ValidatorsCount = 1,
                Hardforks = new Dictionary<Hardfork, uint>() { { Hardfork.HF_Gorgon, 0 } }.ToImmutableDictionary()
            };
            _system = new NeoSystem(settings);
        }

        [TestMethod]
        public void Test_PostPersist_Uses_Live_Votes_With_Fix()
        {
            var snapshot = _system.GetSnapshotCache();
            var validatorKey = new KeyPair(Convert.FromHexString("0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20"));
            BigInteger initialVoterVotes = 10;

            // 1. Initialize the target candidate state in live storage
            SeedCandidate(snapshot, validatorKey.PublicKey, initialVoterVotes);

            // 2. Seed the stale committee cache snapshot to replicate block-start data (10 votes)
            SeedCommitteeCache(snapshot, validatorKey.PublicKey, initialVoterVotes);

            // Mock a simulated block context at an epoch boundary
            var block = new Block
            {
                Header = new Header
                {
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = validatorKey.PublicKeyHash,
                    PrevHash = UInt256.Zero,
                    Witness = Witness.Empty,
                    Index = (uint)_system.Settings.CommitteeMembersCount // Force epoch boundary trigger condition
                },
                Transactions = []
            };

            // 3. Simulate mid-block transaction side-effects (Attacker adds 10,000,000 live votes)
            BigInteger attackerVotes = 10_000_000;
            var storageItem = snapshot.GetAndChange(StorageKey.Create(NativeContract.NEO.Id, PrefixCandidate, validatorKey.PublicKey));
            storageItem.Value = BinarySerializer.Serialize(new Struct { true, attackerVotes }, ExecutionEngineLimits.Default);

            // 4. TRIGGER REAL NATIVE POST-PERSIST LOGIC
            using (var engine = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, block, _system.Settings, 0))
            {
                using var script = new ScriptBuilder();
                script.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
                engine.LoadScript(script.ToArray());

                var state = engine.Execute();
                Assert.AreEqual(VMState.HALT, state, "NativePostPersist lifecycle execution faulted.");
            }

            // 5. STORAGE ENTRY VALIDATION
            // This will now pass because the committee structure features votes > 0, entering the operational loop
            Assert.IsTrue(snapshot.TryGet(StorageKey.Create(NativeContract.NEO.Id, PrefixVoterRewardPerCommittee, validatorKey.PublicKey), out var rewardItem), "Committee reward record entry missing.");

            var accumulatedRewardPerVote = (BigInteger)rewardItem;

            // 6. ECONOMIC ACCOUNTING MATH VALIDATION (Where the vulnerable system breaks)
            var gasPerBlock = NativeContract.NEO.GetGasPerBlock(snapshot);
            var expectedMaxReward = 2 * (gasPerBlock * 80 * VoteFactor / 100) / attackerVotes;

            // WITHOUT FIX: The math will divide by the stale cache value '10', inflating the reward distribution 
            // exponentially and throwing an assertion failure detailing the inflation gap.
            Assert.IsTrue(accumulatedRewardPerVote <= expectedMaxReward,
                $"Inflation Vulnerability Triggered! Calculated reward: {accumulatedRewardPerVote}. Expected bound <= {expectedMaxReward} (Stale denominator error).");
        }

        private static void SeedCandidate(DataCache snapshot, ECPoint candidate, BigInteger votes)
        {
            snapshot.Add(
                StorageKey.Create(NativeContract.NEO.Id, PrefixCandidate, candidate),
                new StorageItem(BinarySerializer.Serialize(new Struct { true, votes }, ExecutionEngineLimits.Default))
            );
        }

        private static void SeedCommitteeCache(DataCache snapshot, ECPoint candidate, BigInteger votes)
        {
            // Perfectly mirrors the binary serialization structure NeoToken uses for PrefixCommittee
            var committeeStruct = new Struct { candidate.ToArray(), votes };
            var committeeArray = new Neo.VM.Types.Array { committeeStruct };

            snapshot.Add(
                StorageKey.Create(NativeContract.NEO.Id, PrefixCommittee),
                new StorageItem(BinarySerializer.Serialize(committeeArray, ExecutionEngineLimits.Default))
            );
        }

        [TestCleanup]
        public void Teardown()
        {
            _system?.Dispose();
        }
    }
}
