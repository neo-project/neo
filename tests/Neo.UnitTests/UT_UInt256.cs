// Copyright (C) 2015-2025 The Neo Project.
//
// UT_UInt256.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable CS1718

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using System;
using System.IO;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_UInt256
    {
        [TestMethod]
        public void TestFail()
        {
            Assert.ThrowsExactly<FormatException>(() => _ = new UInt256(new byte[UInt256.Length + 1]));
        }

        [TestMethod]
        public void TestGernerator1()
        {
            UInt256 uInt256 = new();
            Assert.IsNotNull(uInt256);
        }

        [TestMethod]
        public void TestGernerator2()
        {
            UInt256 uInt256 = new(new byte[32]);
            Assert.IsNotNull(uInt256);
        }

        [TestMethod]
        public void TestCompareTo()
        {
            byte[] temp = new byte[32];
            temp[31] = 0x01;
            UInt256 result = new(temp);
            Assert.AreEqual(0, UInt256.Zero.CompareTo(UInt256.Zero));
            Assert.AreEqual(-1, UInt256.Zero.CompareTo(result));
            Assert.AreEqual(1, result.CompareTo(UInt256.Zero));
        }

        [TestMethod]
        public void TestDeserialize()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.Write(new byte[20]);
            UInt256 uInt256 = new();
            Assert.ThrowsExactly<FormatException>(() =>
            {
                MemoryReader reader = new(stream.ToArray());
                ((ISerializable)uInt256).Deserialize(ref reader);
            });
        }

        [TestMethod]
        public void TestEquals()
        {
            byte[] temp = new byte[32];
            temp[31] = 0x01;
            UInt256 result = new(temp);
            Assert.IsTrue(UInt256.Zero.Equals(UInt256.Zero));
            Assert.IsFalse(UInt256.Zero.Equals(result));
            Assert.IsFalse(result.Equals(null));
        }

        [TestMethod]
        public void TestEquals1()
        {
            UInt256 temp1 = new();
            UInt256 temp2 = new();
            UInt160 temp3 = new();
            Assert.IsFalse(temp1.Equals(null));
            Assert.IsTrue(temp1.Equals(temp1));
            Assert.IsTrue(temp1.Equals(temp2));
            Assert.IsFalse(temp1.Equals(temp3));
        }

        [TestMethod]
        public void TestEquals2()
        {
            UInt256 temp1 = new();
            object temp2 = null;
            object temp3 = new();
            Assert.IsFalse(temp1.Equals(temp2));
            Assert.IsFalse(temp1.Equals(temp3));
        }

        [TestMethod]
        public void TestParse()
        {
            Action action = () => UInt256.Parse(null);
            Assert.ThrowsExactly<FormatException>(() => action());
            UInt256 result = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000");
            Assert.AreEqual(UInt256.Zero, result);
            Action action1 = () => UInt256.Parse("000000000000000000000000000000000000000000000000000000000000000");
            Assert.ThrowsExactly<FormatException>(() => action1());
            UInt256 result1 = UInt256.Parse("0000000000000000000000000000000000000000000000000000000000000000");
            Assert.AreEqual(UInt256.Zero, result1);
        }

        [TestMethod]
        public void TestTryParse()
        {
            Assert.IsFalse(UInt256.TryParse(null, out _));
            Assert.IsTrue(UInt256.TryParse("0x0000000000000000000000000000000000000000000000000000000000000000", out var temp));
            Assert.AreEqual(UInt256.Zero, temp);
            Assert.IsTrue(UInt256.TryParse("0x1230000000000000000000000000000000000000000000000000000000000000", out temp));
            Assert.AreEqual("0x1230000000000000000000000000000000000000000000000000000000000000", temp.ToString());
            Assert.IsFalse(UInt256.TryParse("000000000000000000000000000000000000000000000000000000000000000", out _));
            Assert.IsFalse(UInt256.TryParse("0xKK00000000000000000000000000000000000000000000000000000000000000", out _));
        }

        [TestMethod]
        public void TestOperatorEqual()
        {
            Assert.IsFalse(new UInt256() == null);
            Assert.IsFalse(null == new UInt256());
        }

        [TestMethod]
        public void TestOperatorLarger()
        {
            Assert.IsFalse(UInt256.Zero > UInt256.Zero);
        }

        [TestMethod]
        public void TestOperatorLargerAndEqual()
        {
            Assert.IsTrue(UInt256.Zero >= UInt256.Zero);
        }

        [TestMethod]
        public void TestOperatorSmaller()
        {
            Assert.IsFalse(UInt256.Zero < UInt256.Zero);
        }

        [TestMethod]
        public void TestOperatorSmallerAndEqual()
        {
            Assert.IsTrue(UInt256.Zero <= UInt256.Zero);
        }

        [TestMethod]
        public void TestSpanAndSerialize()
        {
            var random = new Random();
            var data = new byte[UInt256.Length];
            random.NextBytes(data);

            var value = new UInt256(data);
            var span = value.GetSpan();
            Assert.IsTrue(span.SequenceEqual(value.ToArray()));

            data = new byte[UInt256.Length];
            value.Serialize(data.AsSpan());
            CollectionAssert.AreEqual(data, value.ToArray());

            data = new byte[UInt256.Length];
            ((ISerializableSpan)value).Serialize(data.AsSpan());
            CollectionAssert.AreEqual(data, value.ToArray());
        }

        [TestMethod]
        public void TestSpanAndSerializeLittleEndian()
        {
            var random = new Random();
            var data = new byte[UInt256.Length];
            random.NextBytes(data);

            var value = new UInt256(data);
            var spanLittleEndian = value.GetSpanLittleEndian();
            CollectionAssert.AreEqual(data, spanLittleEndian.ToArray());

            // Check that Serialize LittleEndian and Serialize BigEndian are equals
            var dataLittleEndian = new byte[UInt256.Length];
            value.SerializeLittleEndian(dataLittleEndian.AsSpan());
            CollectionAssert.AreEqual(value.ToArray(), dataLittleEndian);

            // Check that Serialize LittleEndian and Serialize BigEndian are equals
            var dataSerialized = new byte[UInt256.Length];
            value.Serialize(dataSerialized.AsSpan());
            CollectionAssert.AreEqual(value.ToArray(), dataSerialized);

            var shortBuffer = new byte[UInt256.Length - 1];
            Assert.ThrowsExactly<ArgumentException>(() => value.Serialize(shortBuffer.AsSpan()));
            Assert.ThrowsExactly<ArgumentException>(() => value.SerializeLittleEndian(shortBuffer.AsSpan()));
        }
    }
}
