// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BigDecimal.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;

namespace Neo.UnitTests;

[TestClass]
public class UT_BigDecimal
{
    [TestMethod]
    public void TestChangeDecimals()
    {
        BigDecimal originalValue = new(new BigInteger(12300), 5);
        BigDecimal result1 = originalValue.ChangeDecimals(7);
        Assert.AreEqual(new BigInteger(1230000), result1.Value);
        Assert.AreEqual(7, result1.Decimals);
        BigDecimal result2 = originalValue.ChangeDecimals(3);
        Assert.AreEqual(new BigInteger(123), result2.Value);
        Assert.AreEqual(3, result2.Decimals);
        BigDecimal result3 = originalValue.ChangeDecimals(5);
        Assert.AreEqual(originalValue.Value, result3.Value);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => originalValue.ChangeDecimals(2));
    }

    [TestMethod]
    public void TestBigDecimalConstructor()
    {
        BigDecimal value = new(new BigInteger(45600), 7);
        Assert.AreEqual(new BigInteger(45600), value.Value);
        Assert.AreEqual(7, value.Decimals);

        value = new BigDecimal(new BigInteger(0), 5);
        Assert.AreEqual(new BigInteger(0), value.Value);
        Assert.AreEqual(5, value.Decimals);

        value = new BigDecimal(new BigInteger(-10), 0);
        Assert.AreEqual(new BigInteger(-10), value.Value);
        Assert.AreEqual(0, value.Decimals);

        value = new BigDecimal(123.456789M, 6);
        Assert.AreEqual(new BigInteger(123456789), value.Value);
        Assert.AreEqual(6, value.Decimals);

        value = new BigDecimal(-123.45M, 3);
        Assert.AreEqual(new BigInteger(-123450), value.Value);
        Assert.AreEqual(3, value.Decimals);

        value = new BigDecimal(123.45M, 2);
        Assert.AreEqual(new BigInteger(12345), value.Value);
        Assert.AreEqual(2, value.Decimals);

        value = new BigDecimal(123M, 0);
        Assert.AreEqual(new BigInteger(123), value.Value);
        Assert.AreEqual(0, value.Decimals);

        value = new BigDecimal(0M, 0);
        Assert.AreEqual(new BigInteger(0), value.Value);
        Assert.AreEqual(0, value.Decimals);

        value = new BigDecimal(5.5M, 1);
        var b = new BigDecimal(55M);
        Assert.AreEqual(b.Value, value.Value);
    }

    [TestMethod]
    public void TestGetDecimals()
    {
        BigDecimal value = new(new BigInteger(45600), 7);
        Assert.AreEqual(1, value.Sign);
        value = new BigDecimal(new BigInteger(0), 5);
        Assert.AreEqual(0, value.Sign);
        value = new BigDecimal(new BigInteger(-10), 0);
        Assert.AreEqual(-1, value.Sign);
    }

    [TestMethod]
    public void TestCompareDecimals()
    {
        BigDecimal a = new(5.5M, 1);
        BigDecimal b = new(55M);
        BigDecimal c = new(55M, 1);
        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals(c));
        Assert.IsTrue(b.Equals(c));
        Assert.AreEqual(-1, a.CompareTo(b));
        Assert.AreEqual(-1, a.CompareTo(c));
        Assert.AreEqual(0, b.CompareTo(c));
    }

    [TestMethod]
    public void TestCompareDecimalsObject()
    {
        var a = new BigDecimal(new BigInteger(12345), 2);
        var b = new BigDecimal(new BigInteger(12345), 2);
        var c = new BigDecimal(new BigInteger(54321), 2);
        var d = new BigDecimal(new BigInteger(12345), 3);
        var e = new BigInteger(12345);

        // Check same value and decimal
        Assert.IsTrue(a.Equals((object)b));

        // Check different value and decimal
        Assert.IsFalse(a.Equals((object)c));

        // Check same value and different decimal
        Assert.IsFalse(a.Equals((object)d));

        // Check different data type
        Assert.IsFalse(a.Equals(e));
    }

    [TestMethod]
    public void TestGetSign()
    {
        BigDecimal value = new(new BigInteger(45600), 7);
        Assert.AreEqual(1, value.Sign);
        value = new BigDecimal(new BigInteger(0), 5);
        Assert.AreEqual(0, value.Sign);
        value = new BigDecimal(new BigInteger(-10), 0);
        Assert.AreEqual(-1, value.Sign);
    }

    [TestMethod]
    public void TestParse()
    {
        string s = "12345";
        byte decimals = 0;
        Assert.AreEqual(new BigDecimal(new BigInteger(12345), 0), BigDecimal.Parse(s, decimals));

        s = "abcdEfg";
        Assert.ThrowsExactly<FormatException>(() => BigDecimal.Parse(s, decimals));
    }

    [TestMethod]
    public void TestToString()
    {
        BigDecimal value = new(new BigInteger(100000), 5);
        Assert.AreEqual("1", value.ToString());
        value = new BigDecimal(new BigInteger(123456), 5);
        Assert.AreEqual("1.23456", value.ToString());
    }

    [TestMethod]
    public void TestTryParse()
    {
        string s = "12345";
        byte decimals = 0;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out BigDecimal result));
        Assert.AreEqual(new BigDecimal(new BigInteger(12345), 0), result);

        s = "12345E-5";
        decimals = 5;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(new BigDecimal(new BigInteger(12345), 5), result);

        s = "abcdEfg";
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "123.45";
        decimals = 2;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(new BigDecimal(new BigInteger(12345), 2), result);

        s = "123.45E-5";
        decimals = 7;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(new BigDecimal(new BigInteger(12345), 7), result);

        s = "12345E-5";
        decimals = 3;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "1.2345";
        decimals = 3;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "1.2345E-5";
        decimals = 3;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "12345";
        decimals = 3;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(new BigDecimal(new BigInteger(12345000), 3), result);

        s = "12345E-2";
        decimals = 3;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(new BigDecimal(new BigInteger(123450), 3), result);

        s = "123.45";
        decimals = 3;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(new BigDecimal(new BigInteger(123450), 3), result);

        s = "123.45E3";
        decimals = 3;
        Assert.IsTrue(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(new BigDecimal(new BigInteger(123450000), 3), result);

        s = "a456bcdfg";
        decimals = 0;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "a456bce-5";
        decimals = 5;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "a4.56bcd";
        decimals = 5;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "a4.56bce3";
        decimals = 2;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "a456bcd";
        decimals = 2;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "a456bcdE3";
        decimals = 2;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "a456b.cd";
        decimals = 5;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);

        s = "a456b.cdE3";
        decimals = 5;
        Assert.IsFalse(BigDecimal.TryParse(s, decimals, out result));
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void TestOperators()
    {
        var a = new BigDecimal(new BigInteger(1000), 2);
        var b = new BigDecimal(new BigInteger(10000), 3);
        var c = new BigDecimal(new BigInteger(10001), 2);

        // Check equal operator
        Assert.IsTrue(a == b);
        Assert.IsFalse(a == c);

        // Check different operator
        Assert.IsFalse(a != b);
        Assert.IsTrue(a != c);

        // Check less operator
        Assert.IsTrue(a < c);
        Assert.IsFalse(a < b);

        // Check less or equal operator
        Assert.IsTrue(a <= c);
        Assert.IsTrue(a <= b);
        Assert.IsFalse(c <= a);

        // Check greater operator
        Assert.IsFalse(a > c);
        Assert.IsFalse(a > b);
        Assert.IsTrue(c > a);

        // Check greater or equal operator
        Assert.IsFalse(a >= c);
        Assert.IsTrue(a >= b);
        Assert.IsTrue(c >= a);
    }

    [TestMethod]
    public void TestGetHashCode()
    {
        var a = new BigDecimal(new BigInteger(123450), 3);
        var b = new BigDecimal(new BigInteger(123450), 3);
        var c = new BigDecimal(new BigInteger(12345), 2);
        var d = new BigDecimal(new BigInteger(123451), 3);
        // Check hash codes are equal for equivalent decimals
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        // Check hash codes may differ for semantically equivalent values
        Assert.AreNotEqual(a.GetHashCode(), c.GetHashCode());
        // Check hash codes are not equal for different values
        Assert.AreNotEqual(a.GetHashCode(), d.GetHashCode());
    }
}
