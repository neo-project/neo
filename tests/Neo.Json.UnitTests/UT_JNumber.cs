// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JNumber.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;

namespace Neo.Json.UnitTests
{
    enum Woo
    {
        Tom,
        Jerry,
        James
    }

    [TestClass]
    public class UT_JNumber
    {
        private JNumber maxInt;
        private JNumber minInt;
        private JNumber zero;

        [TestInitialize]
        public void SetUp()
        {
            maxInt = new JNumber(JNumber.MAX_SAFE_INTEGER);
            minInt = new JNumber(JNumber.MIN_SAFE_INTEGER);
            zero = new JNumber();
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            Assert.IsTrue(maxInt.AsBoolean());
            Assert.IsFalse(zero.AsBoolean());
        }

        [TestMethod]
        public void TestAsString()
        {
            Action action1 = () => new JNumber(double.PositiveInfinity).AsString();
            Assert.ThrowsException<FormatException>(action1);

            Action action2 = () => new JNumber(double.NegativeInfinity).AsString();
            Assert.ThrowsException<FormatException>(action2);

            Action action3 = () => new JNumber(double.NaN).AsString();
            Assert.ThrowsException<FormatException>(action3);
        }

        [TestMethod]
        public void TestGetEnum()
        {
            Assert.AreEqual(Woo.Tom, zero.GetEnum<Woo>());
            Assert.AreEqual(Woo.Jerry, new JNumber(1).GetEnum<Woo>());
            Assert.AreEqual(Woo.James, new JNumber(2).GetEnum<Woo>());
            Assert.AreEqual(Woo.Tom, new JNumber(3).AsEnum<Woo>());
            Action action = () => new JNumber(3).GetEnum<Woo>();
            Assert.ThrowsException<InvalidCastException>(action);
        }

        [TestMethod]
        public void TestEqual()
        {
            Assert.IsTrue(maxInt.Equals(JNumber.MAX_SAFE_INTEGER));
            Assert.IsTrue(maxInt == JNumber.MAX_SAFE_INTEGER);
            Assert.IsTrue(minInt.Equals(JNumber.MIN_SAFE_INTEGER));
            Assert.IsTrue(minInt == JNumber.MIN_SAFE_INTEGER);
            Assert.IsTrue(zero == new JNumber());
            Assert.IsFalse(zero != new JNumber());
            Assert.IsTrue(zero.AsNumber() == zero.GetNumber());
            Assert.IsFalse(zero == null);

            var jnum = new JNumber(1);
            Assert.IsTrue(jnum.Equals(new JNumber(1)));
            Assert.IsTrue(jnum.Equals((uint)1));
            Assert.IsTrue(jnum.Equals((int)1));
            Assert.IsTrue(jnum.Equals((ulong)1));
            Assert.IsTrue(jnum.Equals((long)1));
            Assert.IsTrue(jnum.Equals((byte)1));
            Assert.IsTrue(jnum.Equals((sbyte)1));
            Assert.IsTrue(jnum.Equals((short)1));
            Assert.IsTrue(jnum.Equals((ushort)1));
            Assert.IsTrue(jnum.Equals((decimal)1));
            Assert.IsTrue(jnum.Equals((float)1));
            Assert.IsTrue(jnum.Equals((double)1));
            Assert.IsFalse(jnum.Equals(null));
            var x = jnum;
            Assert.IsTrue(jnum.Equals(x));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => jnum.Equals(new BigInteger(1)));
        }
    }
}
