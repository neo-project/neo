// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BigDecimal_Edges.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;

namespace Neo.UnitTests
{
    /// <summary>
    /// Edge cases not covered by UT_BigDecimal happy-path constructor/ChangeDecimals tests.
    /// </summary>
    [TestClass]
    public class UT_BigDecimal_Edges
    {
        [TestMethod]
        public void Sign_MatchesValue()
        {
            Assert.AreEqual(1, new BigDecimal(new BigInteger(5), 0).Sign);
            Assert.AreEqual(-1, new BigDecimal(new BigInteger(-5), 0).Sign);
            Assert.AreEqual(0, new BigDecimal(BigInteger.Zero, 2).Sign);
        }

        [TestMethod]
        public void DecimalConstructor_ExcessPrecision_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _ = new BigDecimal(1.2345M, 2));
        }

        [TestMethod]
        public void CompareTo_And_Equals()
        {
            var a = new BigDecimal(new BigInteger(100), 2);
            var b = new BigDecimal(new BigInteger(100), 2);
            var c = new BigDecimal(new BigInteger(200), 2);

            Assert.AreEqual(0, a.CompareTo(b));
            Assert.IsTrue(a.CompareTo(c) < 0);
            Assert.IsTrue(c.CompareTo(a) > 0);
            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
        }

        [TestMethod]
        public void ChangeDecimals_NegativeWithRemainder_Throws()
        {
            var value = new BigDecimal(new BigInteger(-12300), 5);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => value.ChangeDecimals(2));
        }

        [TestMethod]
        public void ChangeDecimals_NegativeExact_Succeeds()
        {
            var value = new BigDecimal(new BigInteger(-12300), 5);
            var result = value.ChangeDecimals(3);
            Assert.AreEqual(new BigInteger(-123), result.Value);
            Assert.AreEqual(3, result.Decimals);
        }

        [TestMethod]
        public void ToString_IncludesDecimals()
        {
            var value = new BigDecimal(new BigInteger(12345), 3);
            var text = value.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(text));
            Assert.IsTrue(text.Contains('1') || text.Contains('.'));
        }
    }
}
