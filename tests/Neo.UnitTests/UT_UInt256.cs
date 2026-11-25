// Copyright (C) 2015-2026 The Neo Project.
//
// UT_UInt256.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable CS1718 // Comparison made to same variable

using Neo.Extensions.IO;
using Neo.Factories;
using Neo.IO;

namespace Neo.UnitTests;

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
        UInt256 uInt256 = new byte[32];
        Assert.IsNotNull(uInt256);
        Assert.AreEqual(UInt256.Zero, uInt256);
    }

    [TestMethod]
    public void TestGernerator3()
    {
        UInt256 uInt256 = "0xff00000000000000000000000000000000000000000000000000000000000001";
        Assert.IsNotNull(uInt256);
        Assert.AreEqual("0xff00000000000000000000000000000000000000000000000000000000000001", uInt256.ToString());

        UInt256 value = "0x0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20";
        Assert.IsNotNull(value);
        Assert.AreEqual("0x0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20", value.ToString());
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
        Assert.AreEqual(0, result.CompareTo(temp));
    }

    [TestMethod]
    public void TestDeserialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(new byte[20]);
        UInt256 uInt256 = new();
        Assert.ThrowsExactly<FormatException>(() => MemoryReaderDeserialize(stream.ToArray(), uInt256));

        static void MemoryReaderDeserialize(byte[] buffer, ISerializable obj)
        {
            MemoryReader reader = new(buffer);
            obj.Deserialize(ref reader);
        }
    }

    [TestMethod]
    public void TestEquals()
    {
        var temp = new byte[32];
        temp[31] = 0x01;

        var result = new UInt256(temp);
        Assert.IsTrue(UInt256.Zero.Equals(UInt256.Zero));
        Assert.IsFalse(UInt256.Zero.Equals(result));
        Assert.IsFalse(result.Equals(null));
    }

    [TestMethod]
    public void TestEquals1()
    {
        var temp1 = new UInt256();
        var temp2 = new UInt256();
        var temp3 = new UInt160();
        Assert.IsFalse(temp1.Equals(null));
        Assert.IsTrue(temp1.Equals(temp1));
        Assert.IsTrue(temp1.Equals(temp2));
        Assert.IsFalse(temp1.Equals(temp3));
    }

    [TestMethod]
    public void TestEquals2()
    {
        UInt256 temp1 = new();
        object? temp2 = null;
        object temp3 = new();
        Assert.IsFalse(temp1.Equals(temp2));
        Assert.IsFalse(temp1.Equals(temp3));
    }

    [TestMethod]
    public void TestParse()
    {
        UInt256 result = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000");
        Assert.AreEqual(UInt256.Zero, result);
        Assert.ThrowsExactly<FormatException>(() => UInt256.Parse("000000000000000000000000000000000000000000000000000000000000000"));
        UInt256 result1 = UInt256.Parse("0000000000000000000000000000000000000000000000000000000000000000");
        Assert.AreEqual(UInt256.Zero, result1);
    }

    [TestMethod]
    public void TestTryParse()
    {
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
        var data = RandomNumberFactory.NextBytes(UInt256.Length);

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
        var data = RandomNumberFactory.NextBytes(UInt256.Length);

        var value = new UInt256(data);
        var spanLittleEndian = value.GetSpanLittleEndian();
        CollectionAssert.AreEqual(data, spanLittleEndian.ToArray());

        // Check that Serialize LittleEndian and Serialize BigEndian are equals
        var dataLittleEndian = new byte[UInt256.Length];
        value.SafeSerialize(dataLittleEndian.AsSpan());
        CollectionAssert.AreEqual(value.ToArray(), dataLittleEndian);

        // Check that Serialize LittleEndian and Serialize BigEndian are equals
        var dataSerialized = new byte[UInt256.Length];
        value.Serialize(dataSerialized.AsSpan());
        CollectionAssert.AreEqual(value.ToArray(), dataSerialized);

        var shortBuffer = new byte[UInt256.Length - 1];
        Assert.ThrowsExactly<ArgumentException>(() => value.Serialize(shortBuffer.AsSpan()));
        Assert.ThrowsExactly<ArgumentException>(() => value.SafeSerialize(shortBuffer.AsSpan()));
    }

    [TestMethod]
    public void TestXorWithZeroIsIdentity()
    {
        var a = CreateSequential(0x10);
        Assert.AreEqual(a, a ^ UInt256.Zero);
        Assert.AreEqual(a, UInt256.Zero ^ a);
    }

    [TestMethod]
    public void TestXorWithSelfIsZero()
    {
        var a = CreateSequential(0x42);
        Assert.AreEqual(UInt256.Zero, a ^ a);
    }

    [TestMethod]
    public void TestXorAssociative()
    {
        var a = CreateSequential(0x10);
        var b = CreateSequential(0x20);
        var c = CreateSequential(0x30);
        var left = (a ^ b) ^ c;
        var right = a ^ (b ^ c);

        Assert.AreEqual(left, right);
    }

    [TestMethod]
    public void TestXorCommutativeAndMatchesManual()
    {
        var a = CreateSequential(0x00);
        var b = CreateSequential(0xF0);

        var ab = a.ToArray();
        var bb = b.ToArray();

        var rb = new byte[UInt256.Length];
        for (int i = 0; i < rb.Length; i++)
            rb[i] = (byte)(ab[i] ^ bb[i]);

        var expectedValue = new UInt256(rb);
        Assert.AreEqual(expectedValue, a ^ b);
        Assert.AreEqual(expectedValue, b ^ a);
    }

    private static UInt256 CreateSequential(byte start)
    {
        var bytes = new byte[UInt256.Length];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = unchecked((byte)(start + i));
        return new UInt256(bytes);
    }
}
