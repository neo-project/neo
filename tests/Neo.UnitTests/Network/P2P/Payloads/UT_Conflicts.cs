// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Conflicts.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Conflicts
    {
        private const byte Prefix_Transaction = 11;
        private static readonly UInt256 _u = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            });

        private Conflicts CreateConflictsPayload()
        {
            return new Conflicts() { Hash = _u };
        }

        [TestMethod]
        public void Size_Get()
        {
            var test = CreateConflictsPayload();
            Assert.AreEqual(1 + 32, test.Size);
        }

        [TestMethod]
        public void ToJson()
        {
            var test = CreateConflictsPayload();
            var json = test.ToJson().ToString();
            Assert.AreEqual(@"{""type"":""Conflicts"",""hash"":""0x0101010101010101010101010101010101010101010101010101010101010101""}", json);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = CreateConflictsPayload();

            var clone = test.ToArray().AsSerializable<Conflicts>();
            Assert.AreEqual(clone.Type, test.Type);

            // As transactionAttribute
            byte[] buffer = test.ToArray();
            var reader = new MemoryReader(buffer);
            clone = TransactionAttribute.DeserializeFrom(ref reader) as Conflicts;
            Assert.AreEqual(clone.Type, test.Type);

            // Wrong type
            buffer[0] = 0xff;
            Assert.ThrowsException<FormatException>(() =>
            {
                var reader = new MemoryReader(buffer);
                TransactionAttribute.DeserializeFrom(ref reader);
            });
        }

        [TestMethod]
        public void Verify()
        {
            var test = CreateConflictsPayload();
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var key = Ledger.UT_MemoryPool.CreateStorageKey(NativeContract.Ledger.Id, Prefix_Transaction, _u.ToArray());

            // Conflicting transaction is in the Conflicts attribute of some other on-chain transaction.
            var conflict = new TransactionState();
            snapshotCache.Add(key, new StorageItem(conflict));
            Assert.IsTrue(test.Verify(snapshotCache, new Transaction()));

            // Conflicting transaction is on-chain.
            snapshotCache.Delete(key);
            conflict = new TransactionState
            {
                BlockIndex = 123,
                Transaction = new Transaction(),
                State = VMState.NONE
            };
            snapshotCache.Add(key, new StorageItem(conflict));
            Assert.IsFalse(test.Verify(snapshotCache, new Transaction()));

            // There's no conflicting transaction at all.
            snapshotCache.Delete(key);
            Assert.IsTrue(test.Verify(snapshotCache, new Transaction()));
        }
    }
}
