// Copyright (C) 2015-2025 The Neo Project.
//
// UT_GasToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using System;
using System;
using System;
using System.Linq;
using System.Linq;
using System.Numerics;
using System.Numerics;
using System.Numerics;
using System.Threading.Tasks;
using VMTypes = Neo.VM.Types;
// using VMArray = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_GasToken
    {
        private DataCache _snapshotCache;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            _persistingBlock = new Block { Header = new Header() };
        }

        [TestMethod]
        public void Check_Name() => NativeContract.GAS.Name.Should().Be(nameof(GasToken));

        [TestMethod]
        public void Check_Symbol() => NativeContract.GAS.Symbol(_snapshotCache).Should().Be("GAS");

        [TestMethod]
        public void Check_Decimals() => NativeContract.GAS.Decimals(_snapshotCache).Should().Be(8);

        [TestMethod]
        public async Task Check_BalanceOfTransferAndBurn()
        {
            var snapshot = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
            byte[] to = new byte[20];
            var supply = NativeContract.GAS.TotalSupply(snapshot);
            supply.Should().Be(5200000050000000); // 3000000000000000 + 50000000 (neo holder reward)

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
            var keyCount = snapshot.GetChangeSet().Count();
            // Check unclaim

            var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            unclaim.Value.Should().Be(new BigInteger(0.5 * 1000 * 100000000L));
            unclaim.State.Should().BeTrue();

            // Transfer

            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true, persistingBlock).Should().BeTrue();
            Assert.ThrowsException<ArgumentNullException>(() => NativeContract.NEO.Transfer(snapshot, from, null, BigInteger.Zero, true, persistingBlock));
            Assert.ThrowsException<ArgumentNullException>(() => NativeContract.NEO.Transfer(snapshot, null, to, BigInteger.Zero, false, persistingBlock));
            NativeContract.NEO.BalanceOf(snapshot, from).Should().Be(100000000);
            NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(0);

            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(52000500_00000000);
            NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(0);

            // Check unclaim

            unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            supply = NativeContract.GAS.TotalSupply(snapshot);
            supply.Should().Be(5200050050000000);

            snapshot.GetChangeSet().Count().Should().Be(keyCount + 3); // Gas

            // Transfer

            keyCount = snapshot.GetChangeSet().Count();

            NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, false, persistingBlock).Should().BeFalse(); // Not signed
            NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000001, true, persistingBlock).Should().BeFalse(); // More than balance
            NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, true, persistingBlock).Should().BeTrue(); // All balance

            // Balance of

            NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(52000500_00000000);
            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(0);

            snapshot.GetChangeSet().Count().Should().Be(keyCount + 1); // All

            // Burn

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 0);
            engine.LoadScript(Array.Empty<byte>());

            await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(async () =>
                await NativeContract.GAS.Burn(engine, new UInt160(to), BigInteger.MinusOne));

            // Burn more than expected

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(52000500_00000001)));

            // Real burn

            await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(1));

            NativeContract.GAS.BalanceOf(engine.SnapshotCache, to).Should().Be(5200049999999999);

            engine.SnapshotCache.GetChangeSet().Count().Should().Be(2);

            // Burn all
            await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(5200049999999999));

            (keyCount - 2).Should().Be(engine.SnapshotCache.GetChangeSet().Count());

            // Bad inputs

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => NativeContract.GAS.Transfer(engine.SnapshotCache, from, to, BigInteger.MinusOne, true, persistingBlock));
            Assert.ThrowsException<FormatException>(() => NativeContract.GAS.Transfer(engine.SnapshotCache, new byte[19], to, BigInteger.One, false, persistingBlock));
            Assert.ThrowsException<FormatException>(() => NativeContract.GAS.Transfer(engine.SnapshotCache, from, new byte[19], BigInteger.One, false, persistingBlock));
        }

        internal static StorageKey CreateStorageKey(byte prefix, uint key)
        {
            return CreateStorageKey(prefix, BitConverter.GetBytes(key));
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
            buffer[0] = prefix;
            key?.CopyTo(buffer.AsSpan(1));
            return new()
            {
                Id = NativeContract.GAS.Id,
                Key = buffer
            };
        }

        [TestMethod]
        public void Check_OnPersist_NotaryAssisted()
        {
            // Hardcode test values.
            const uint defaultNotaryssestedFeePerKey = 1000_0000;
            const byte NKeys1 = 4;
            const byte NKeys2 = 6;

            // Generate two transactions with NotaryAssisted attributes with hardcoded NKeys values.
            var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
            var tx1 = TestUtils.GetTransaction(from);
            tx1.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = NKeys1 } };
            var netFee1 = 1_0000_0000;
            tx1.NetworkFee = netFee1;
            var tx2 = TestUtils.GetTransaction(from);
            tx2.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = NKeys2 } };
            var netFee2 = 2_0000_0000;
            tx2.NetworkFee = netFee2;

            // Calculate expected Notary nodes reward.
            var expectedNotaryReward = (NKeys1 + 1) * defaultNotaryssestedFeePerKey + (NKeys2 + 1) * defaultNotaryssestedFeePerKey;

            // Build block to check transaction fee distribution during Gas OnPersist.
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Transactions = new Transaction[] { tx1, tx2 }
            };
            var snapshot = _snapshotCache.CloneCache();
            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            // Check that block's Primary balance is 0.
            ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
            var primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
            NativeContract.GAS.BalanceOf(engine.Snapshot, primary).Should().Be(0);

            // Execute OnPersist script.
            engine.LoadScript(script.ToArray());
            Assert.IsTrue(engine.Execute() == VMState.HALT);

            // Check that proper amount of GAS was minted to block's Primary and the rest
            // will be minted to Notary nodes as a reward once Notary contract is implemented.
            Assert.AreEqual(2 + 1, engine.Notifications.Count()); // burn tx1 and tx2 network fee + mint primary reward
            Assert.AreEqual(netFee1 + netFee2 - expectedNotaryReward, engine.Notifications[2].State[2]);
            NativeContract.GAS.BalanceOf(engine.Snapshot, primary).Should().Be(netFee1 + netFee2 - expectedNotaryReward);
        }
    }
}
