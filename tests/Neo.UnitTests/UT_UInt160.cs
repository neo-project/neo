// Copyright (C) 2015-2024 The Neo Project.
//
// UT_UInt160.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable CS1718

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_UInt160
    {
        [TestMethod]
        public void TestFail()
        {
            Assert.ThrowsException<FormatException>(() => new UInt160(new byte[UInt160.Length + 1]));
        }

        [TestMethod]
        public void TestGernerator1()
        {
            UInt160 uInt160 = new UInt160();
            Assert.IsNotNull(uInt160);
        }

        [TestMethod]
        public void TestGernerator2()
        {
            UInt160 uInt160 = new UInt160(new byte[20]);
            Assert.IsNotNull(uInt160);
        }

        [TestMethod]
        public void TestCompareTo()
        {
            byte[] temp = new byte[20];
            temp[19] = 0x01;
            UInt160 result = new UInt160(temp);
            Assert.AreEqual(0, UInt160.Zero.CompareTo(UInt160.Zero));
            Assert.AreEqual(-1, UInt160.Zero.CompareTo(result));
            Assert.AreEqual(1, result.CompareTo(UInt160.Zero));
        }

        [TestMethod]
        public void TestEquals()
        {
            byte[] temp = new byte[20];
            temp[19] = 0x01;
            UInt160 result = new UInt160(temp);
            Assert.AreEqual(true, UInt160.Zero.Equals(UInt160.Zero));
            Assert.AreEqual(false, UInt160.Zero.Equals(result));
            Assert.AreEqual(false, result.Equals(null));
        }

        [TestMethod]
        public void TestParse()
        {
            Action action = () => UInt160.Parse(null);
            action.Should().Throw<FormatException>();
            UInt160 result = UInt160.Parse("0x0000000000000000000000000000000000000000");
            Assert.AreEqual(UInt160.Zero, result);
            Action action1 = () => UInt160.Parse("000000000000000000000000000000000000000");
            action1.Should().Throw<FormatException>();
            UInt160 result1 = UInt160.Parse("0000000000000000000000000000000000000000");
            Assert.AreEqual(UInt160.Zero, result1);
        }

        [TestMethod]
        public void TestTryParse()
        {
            Assert.AreEqual(false, UInt160.TryParse(null, out _));
            Assert.AreEqual(true, UInt160.TryParse("0x0000000000000000000000000000000000000000", out var temp));
            Assert.AreEqual("0x0000000000000000000000000000000000000000", temp.ToString());
            Assert.AreEqual(UInt160.Zero, temp);
            Assert.AreEqual(true, UInt160.TryParse("0x1230000000000000000000000000000000000000", out temp));
            Assert.AreEqual("0x1230000000000000000000000000000000000000", temp.ToString());
            Assert.AreEqual(false, UInt160.TryParse("000000000000000000000000000000000000000", out _));
            Assert.AreEqual(false, UInt160.TryParse("0xKK00000000000000000000000000000000000000", out _));
        }

        [TestMethod]
        public void TestOperatorLarger()
        {
            Assert.AreEqual(false, UInt160.Zero > UInt160.Zero);
        }

        [TestMethod]
        public void TestOperatorLargerAndEqual()
        {
            Assert.AreEqual(true, UInt160.Zero >= UInt160.Zero);
        }

        [TestMethod]
        public void TestOperatorSmaller()
        {
            Assert.AreEqual(false, UInt160.Zero < UInt160.Zero);
        }

        [TestMethod]
        public void TestOperatorSmallerAndEqual()
        {
            Assert.AreEqual(true, UInt160.Zero <= UInt160.Zero);
        }
    }
}
