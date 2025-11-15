// Copyright (C) 2015-2025 The Neo Project.
//
// UT_UInt160.cs file belongs to the neo project and is free
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
public class UT_UInt160
{
    [TestMethod]
    public void TestFail()
    {
        Assert.ThrowsExactly<FormatException>(() => _ = new UInt160(new byte[UInt160.Length + 1]));
    }

    [TestMethod]
    public void TestGernerator1()
    {
        var uInt160 = new UInt160();
        Assert.IsNotNull(uInt160);
    }

    [TestMethod]
    public void TestGernerator2()
    {
        UInt160 uInt160 = new byte[20];
        Assert.IsNotNull(uInt160);
    }

    [TestMethod]
    public void TestGernerator3()
    {
        UInt160 uInt160 = "0xff00000000000000000000000000000000000001";
        Assert.IsNotNull(uInt160);
        Assert.AreEqual("0xff00000000000000000000000000000000000001", uInt160.ToString());

        UInt160 value = "0x0102030405060708090a0b0c0d0e0f1011121314";
        Assert.IsNotNull(value);
        Assert.AreEqual("0x0102030405060708090a0b0c0d0e0f1011121314", value.ToString());
    }

    [TestMethod]
    public void TestCompareTo()
    {
        var temp = new byte[20];
        temp[19] = 0x01;
        var result = new UInt160(temp);
        Assert.AreEqual(0, UInt160.Zero.CompareTo(UInt160.Zero));
        Assert.AreEqual(-1, UInt160.Zero.CompareTo(result));
        Assert.AreEqual(1, result.CompareTo(UInt160.Zero));
        Assert.AreEqual(0, result.CompareTo(temp));
    }

    [TestMethod]
    public void TestEquals()
    {
        byte[] temp = new byte[20];
        temp[19] = 0x01;
        var result = new UInt160(temp);
        Assert.IsTrue(UInt160.Zero.Equals(UInt160.Zero));
        Assert.IsFalse(UInt160.Zero.Equals(result));
        Assert.IsFalse(result.Equals(null));
        Assert.IsTrue(UInt160.Zero == UInt160.Zero);
        Assert.IsFalse(UInt160.Zero != UInt160.Zero);
        Assert.IsTrue(UInt160.Zero == "0x0000000000000000000000000000000000000000");
        Assert.IsFalse(UInt160.Zero == "0x0000000000000000000000000000000000000001");
    }

    [TestMethod]
    public void TestParse()
    {
        UInt160 result = UInt160.Parse("0x0000000000000000000000000000000000000000");
        Assert.AreEqual(UInt160.Zero, result);
        Assert.ThrowsExactly<FormatException>(() => UInt160.Parse("000000000000000000000000000000000000000"));
        UInt160 result1 = UInt160.Parse("0000000000000000000000000000000000000000");
        Assert.AreEqual(UInt160.Zero, result1);
    }

    [TestMethod]
    public void TestTryParse()
    {
        Assert.IsTrue(UInt160.TryParse("0x0000000000000000000000000000000000000000", out var temp));
        Assert.AreEqual("0x0000000000000000000000000000000000000000", temp.ToString());
        Assert.AreEqual(UInt160.Zero, temp);
        Assert.IsTrue(UInt160.TryParse("0x1230000000000000000000000000000000000000", out temp));
        Assert.AreEqual("0x1230000000000000000000000000000000000000", temp.ToString());
        Assert.IsFalse(UInt160.TryParse("000000000000000000000000000000000000000", out _));
        Assert.IsFalse(UInt160.TryParse("0xKK00000000000000000000000000000000000000", out _));
        Assert.IsFalse(UInt160.TryParse(" 1 2 3 45 000000000000000000000000000000", out _));
    }

    [TestMethod]
    public void TestOperatorLarger()
    {
        Assert.IsFalse(UInt160.Zero > UInt160.Zero);
        Assert.IsFalse(UInt160.Zero > "0x0000000000000000000000000000000000000000");
    }

    [TestMethod]
    public void TestOperatorLargerAndEqual()
    {
        Assert.IsTrue(UInt160.Zero >= UInt160.Zero);
        Assert.IsTrue(UInt160.Zero >= "0x0000000000000000000000000000000000000000");
    }

    [TestMethod]
    public void TestOperatorSmaller()
    {
        Assert.IsFalse(UInt160.Zero < UInt160.Zero);
        Assert.IsFalse(UInt160.Zero < "0x0000000000000000000000000000000000000000");
    }

    [TestMethod]
    public void TestOperatorSmallerAndEqual()
    {
        Assert.IsTrue(UInt160.Zero <= UInt160.Zero);
        Assert.IsTrue(UInt160.Zero >= "0x0000000000000000000000000000000000000000");
    }

    [TestMethod]
    public void TestSpanAndSerialize()
    {
        // random data
        var data = RandomNumberFactory.NextBytes(UInt160.Length);

        var value = new UInt160(data);
        var span = value.GetSpan();
        Assert.IsTrue(span.SequenceEqual(value.ToArray()));

        data = new byte[UInt160.Length];
        value.Serialize(data.AsSpan());
        CollectionAssert.AreEqual(data, value.ToArray());

        data = new byte[UInt160.Length];
        ((ISerializableSpan)value).Serialize(data.AsSpan());
        CollectionAssert.AreEqual(data, value.ToArray());
    }

    [TestMethod]
    public void TestSpanAndSerializeLittleEndian()
    {
        // random data
        var data = RandomNumberFactory.NextBytes(UInt160.Length);

        var value = new UInt160(data);

        var spanLittleEndian = value.GetSpanLittleEndian();
        CollectionAssert.AreEqual(data, spanLittleEndian.ToArray());

        var dataLittleEndian = new byte[UInt160.Length];
        value.SafeSerialize(dataLittleEndian.AsSpan());
        CollectionAssert.AreEqual(data, dataLittleEndian);

        // Check that Serialize LittleEndian and Serialize BigEndian are equals
        var dataSerialized = new byte[UInt160.Length];
        value.Serialize(dataSerialized.AsSpan());
        CollectionAssert.AreEqual(value.ToArray(), dataSerialized);

        var shortBuffer = new byte[UInt160.Length - 1];
        Assert.ThrowsExactly<ArgumentException>(() => value.Serialize(shortBuffer.AsSpan()));
        Assert.ThrowsExactly<ArgumentException>(() => value.SafeSerialize(shortBuffer.AsSpan()));
    }
}

#pragma warning restore CS1718 // Comparison made to same variable
