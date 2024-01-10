// Copyright (C) 2015-2024 The Neo Project.
//
// UT_StorageItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
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
            uut.Value.IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void Value_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32 };
            uut.Value = val;
            uut.Value.Length.Should().Be(2);
            uut.Value.Span[0].Should().Be(val[0]);
            uut.Value.Span[1].Should().Be(val[1]);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);
            uut.Size.Should().Be(11); // 1 + 10
        }

        [TestMethod]
        public void Size_Get_Larger()
        {
            uut.Value = TestUtils.GetByteArray(88, 0x42);
            uut.Size.Should().Be(89); // 1 + 88
        }

        [TestMethod]
        public void Clone()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);

            StorageItem newSi = uut.Clone();
            var span = newSi.Value.Span;
            span.Length.Should().Be(10);
            span[0].Should().Be(0x42);
            for (int i = 1; i < 10; i++)
            {
                span[i].Should().Be(0x20);
            }
        }

        [TestMethod]
        public void Deserialize()
        {
            byte[] data = new byte[] { 66, 32, 32, 32, 32, 32, 32, 32, 32, 32 };
            MemoryReader reader = new(data);
            uut.Deserialize(ref reader);
            var span = uut.Value.Span;
            span.Length.Should().Be(10);
            span[0].Should().Be(0x42);
            for (int i = 1; i < 10; i++)
            {
                span[i].Should().Be(0x20);
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

            data.Length.Should().Be(requiredData.Length);
            for (int i = 0; i < requiredData.Length; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void TestFromReplica()
        {
            uut.Value = TestUtils.GetByteArray(10, 0x42);
            StorageItem dest = new StorageItem();
            dest.FromReplica(uut);
            dest.Value.Should().BeEquivalentTo(uut.Value);
        }
    }
}
