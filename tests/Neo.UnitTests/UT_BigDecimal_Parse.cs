// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BigDecimal_Parse.cs file belongs to the neo project and is free
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
    /// Parse/TryParse coverage not covered by UT_BigDecimal or UT_BigDecimal_Edges.
    /// </summary>
    [TestClass]
    public class UT_BigDecimal_Parse
    {
        [TestMethod]
        public void Parse_SimpleAndDecimal()
        {
            var a = BigDecimal.Parse("123", 0);
            Assert.AreEqual(new BigInteger(123), a.Value);
            Assert.AreEqual(0, a.Decimals);

            var b = BigDecimal.Parse("12.34", 2);
            Assert.AreEqual(new BigInteger(1234), b.Value);
            Assert.AreEqual(2, b.Decimals);
        }

        [TestMethod]
        public void Parse_ScientificNotation()
        {
            var a = BigDecimal.Parse("1.5e2", 0);
            Assert.AreEqual(new BigInteger(150), a.Value);

            var b = BigDecimal.Parse("1e-1", 2);
            Assert.AreEqual(new BigInteger(10), b.Value); // 0.1 with 2 decimals => 10
        }

        [TestMethod]
        public void TryParse_Invalid_ReturnsFalse()
        {
            Assert.IsFalse(BigDecimal.TryParse("not-a-number", 2, out _));
            Assert.IsFalse(BigDecimal.TryParse("1e999", 0, out _));
        }

        [TestMethod]
        public void Parse_Invalid_Throws()
        {
            Assert.ThrowsExactly<FormatException>(() => BigDecimal.Parse("abc", 0));
        }
    }
}
