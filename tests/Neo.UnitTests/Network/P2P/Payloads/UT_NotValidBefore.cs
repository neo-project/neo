// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NotValidBefore.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_NotValidBefore
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new NotValidBefore();
            Assert.AreEqual(5, test.Size);
        }

        [TestMethod]
        public void ToJson()
        {
            var test = new NotValidBefore
            {
                Height = 42
            };
            var json = test.ToJson().ToString();
            Assert.AreEqual(@"{""type"":""NotValidBefore"",""height"":42}", json);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new NotValidBefore();

            var clone = test.ToArray().AsSerializable<NotValidBefore>();
            Assert.AreEqual(clone.Type, test.Type);

            // As transactionAttribute

            byte[] buffer = test.ToArray();
            var reader = new MemoryReader(buffer);
            clone = TransactionAttribute.DeserializeFrom(ref reader) as NotValidBefore;
            Assert.AreEqual(clone.Type, test.Type);

            // Wrong type

            buffer[0] = 0xff;
            reader = new MemoryReader(buffer);
            try
            {
                TransactionAttribute.DeserializeFrom(ref reader);
                Assert.Fail();
            }
            catch (FormatException) { }
            reader = new MemoryReader(buffer);
            try
            {
                new NotValidBefore().Deserialize(ref reader);
                Assert.Fail();
            }
            catch (FormatException) { }
        }

        [TestMethod]
        public void Verify()
        {
            var test = new NotValidBefore();
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            test.Height = NativeContract.Ledger.CurrentIndex(snapshotCache) + 1;

            Assert.IsFalse(test.Verify(snapshotCache, new Transaction()));
            test.Height--;
            Assert.IsTrue(test.Verify(snapshotCache, new Transaction()));
        }
    }
}
