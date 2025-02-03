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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_GasToken
    {
        private StorageCache _snapshotCache;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            _persistingBlock = new Block { Header = new Header() };
        }

        [TestMethod]
        public void Check_Name() => Assert.AreEqual(nameof(GasToken), NativeContract.GAS.Name);

        [TestMethod]
        public void Check_Symbol() => Assert.AreEqual("GAS", NativeContract.GAS.Symbol(_snapshotCache));

        [TestMethod]
        public void Check_Decimals() => Assert.AreEqual(8, NativeContract.GAS.Decimals(_snapshotCache));

        [TestMethod]
        public async Task Check_BalanceOfTransferAndBurn()
        {
            var snapshot = _snapshotCache.CloneCache();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
            byte[] to = new byte[20];
            var supply = NativeContract.GAS.TotalSupply(snapshot);
            Assert.AreEqual(5200000050000000, supply); // 3000000000000000 + 50000000 (neo holder reward)

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
            var keyCount = snapshot.GetChangeSet().Count();
            // Check unclaim

            var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            Assert.AreEqual(new BigInteger(0.5 * 1000 * 100000000L), unclaim.Value);
            Assert.IsTrue(unclaim.State);

            // Transfer

            Assert.IsTrue(NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true, persistingBlock));
            Assert.ThrowsException<ArgumentNullException>(() => NativeContract.NEO.Transfer(snapshot, from, null, BigInteger.Zero, true, persistingBlock));
            Assert.ThrowsException<ArgumentNullException>(() => NativeContract.NEO.Transfer(snapshot, null, to, BigInteger.Zero, false, persistingBlock));
            Assert.AreEqual(100000000, NativeContract.NEO.BalanceOf(snapshot, from));
            Assert.AreEqual(0, NativeContract.NEO.BalanceOf(snapshot, to));

            Assert.AreEqual(52000500_00000000, NativeContract.GAS.BalanceOf(snapshot, from));
            Assert.AreEqual(0, NativeContract.GAS.BalanceOf(snapshot, to));

            // Check unclaim

            unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            Assert.AreEqual(new BigInteger(0), unclaim.Value);
            Assert.IsTrue(unclaim.State);

            supply = NativeContract.GAS.TotalSupply(snapshot);
            Assert.AreEqual(5200050050000000, supply);

            Assert.AreEqual(keyCount + 3, snapshot.GetChangeSet().Count()); // Gas

            // Transfer

            keyCount = snapshot.GetChangeSet().Count();

            Assert.IsFalse(NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, false, persistingBlock)); // Not signed
            Assert.IsFalse(NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000001, true, persistingBlock)); // More than balance
            Assert.IsTrue(NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, true, persistingBlock)); // All balance

            // Balance of

            Assert.AreEqual(52000500_00000000, NativeContract.GAS.BalanceOf(snapshot, to));
            Assert.AreEqual(0, NativeContract.GAS.BalanceOf(snapshot, from));

            Assert.AreEqual(keyCount + 1, snapshot.GetChangeSet().Count()); // All

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

            Assert.AreEqual(5200049999999999, NativeContract.GAS.BalanceOf(engine.SnapshotCache, to));

            Assert.AreEqual(2, engine.SnapshotCache.GetChangeSet().Count());

            // Burn all
            await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(5200049999999999));

            Assert.AreEqual(keyCount - 2, engine.SnapshotCache.GetChangeSet().Count());

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
    }
}
