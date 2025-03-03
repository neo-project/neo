// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StorageItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract;
using System.IO;
using System.Text;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_StorageItem
    {
        StorageItem uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new StorageItem();
        }

        [TestMethod]
        public void Value_Get()
        {
            Assert.IsTrue(uut.Value.IsEmpty);
        }

        [TestMethod]
        public void Value_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32 };
            uut.Value = val;
            Assert.AreEqual(2, uut.Value.Length);
            Assert.AreEqual(val[0], uut.Value.Span[0]);
            Assert.AreEqual(val[1], uut.Value.Span[1]);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);
            Assert.AreEqual(11, uut.Size); // 1 + 10
        }

        [TestMethod]
        public void Size_Get_Larger()
        {
            uut.Value = TestUtils.GetByteArray(88, 0x42);
            Assert.AreEqual(89, uut.Size); // 1 + 88
        }

        [TestMethod]
        public void Clone()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);

            StorageItem newSi = uut.Clone();
            var span = newSi.Value.Span;
            Assert.AreEqual(10, span.Length);
            Assert.AreEqual(0x42, span[0]);
            for (int i = 1; i < 10; i++)
            {
                Assert.AreEqual(0x20, span[i]);
            }
        }

        [TestMethod]
        public void Deserialize()
        {
            byte[] data = new byte[] { 66, 32, 32, 32, 32, 32, 32, 32, 32, 32 };
            MemoryReader reader = new(data);
            uut.Deserialize(ref reader);
            var span = uut.Value.Span;
            Assert.AreEqual(10, span.Length);
            Assert.AreEqual(0x42, span[0]);
            for (int i = 1; i < 10; i++)
            {
                Assert.AreEqual(0x20, span[i]);
            }
        }

        [TestMethod]
        public void Serialize()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 66, 32, 32, 32, 32, 32, 32, 32, 32, 32 };

            Assert.AreEqual(requiredData.Length, data.Length);
            for (int i = 0; i < requiredData.Length; i++)
            {
                Assert.AreEqual(requiredData[i], data[i]);
            }
        }

        [TestMethod]
        public void TestFromReplica()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);
            StorageItem dest = new StorageItem();
            dest.FromReplica(uut);
            CollectionAssert.AreEqual(uut.Value.ToArray(), dest.Value.ToArray());
        }
    }
}
