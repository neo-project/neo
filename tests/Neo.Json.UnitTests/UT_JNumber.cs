// Copyright (C) 2015-2026 The Neo Project.
//
// UT_JNumber.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            Assert.ThrowsExactly<FormatException>(action1);

            Action action2 = () => new JNumber(double.NegativeInfinity).AsString();
            Assert.ThrowsExactly<FormatException>(action2);

            Action action3 = () => new JNumber(double.NaN).AsString();
            Assert.ThrowsExactly<FormatException>(action3);
        }

        [TestMethod]
        public void TestGetEnum()
        {
            Assert.AreEqual(Woo.Tom, zero.GetEnum<Woo>());
            Assert.AreEqual(Woo.Jerry, new JNumber(1).GetEnum<Woo>());
            Assert.AreEqual(Woo.James, new JNumber(2).GetEnum<Woo>());
            Assert.AreEqual(Woo.Tom, new JNumber(3).AsEnum<Woo>());
            Action action = () => new JNumber(3).GetEnum<Woo>();
            Assert.ThrowsExactly<InvalidCastException>(action);
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
            Assert.AreEqual(zero.GetNumber(), zero.AsNumber());
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
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = jnum.Equals(new BigInteger(1)));
        }

        [TestMethod]
        public void TestBigIntegerConversion()
        {
            Assert.AreEqual(JNumber.MAX_SAFE_INTEGER, ((JNumber)new BigInteger(JNumber.MAX_SAFE_INTEGER)).GetNumber());
            Assert.AreEqual(JNumber.MIN_SAFE_INTEGER, ((JNumber)new BigInteger(JNumber.MIN_SAFE_INTEGER)).GetNumber());
            JToken token = new BigInteger(42);
            Assert.AreEqual(42, token.GetNumber());
            Assert.IsTrue(((JNumber)new BigInteger(42)).Equals(new JNumber(42)));
            Assert.ThrowsExactly<FormatException>(() => _ = new JNumber(new BigInteger(JNumber.MAX_SAFE_INTEGER) + 1));
            Assert.ThrowsExactly<FormatException>(() => _ = (JToken)(new BigInteger(JNumber.MIN_SAFE_INTEGER) - 1));
        }

        [TestMethod]
        [DataRow("9007199254740992")]
        [DataRow("-9007199254740992")]
        [DataRow("[0,{\"value\":9007199254740992}]")]
        public void TestParseSafeIntegerRange(string json)
        {
            Assert.ThrowsExactly<FormatException>(() => JToken.Parse(json));
            Assert.IsNotNull(JToken.Parse(json, checkNumbers: false));
        }

        [TestMethod]
        [DataRow("9007199254740991")]
        [DataRow("-9007199254740991")]
        public void TestParseSafeIntegerBoundaries(string json)
        {
            Assert.AreEqual(json, JToken.Parse(json)!.ToString());
        }

        [TestMethod]
        public void TestParseLegacyUnderflow()
        {
            Assert.AreEqual(0, JToken.Parse("1e-400", checkNumbers: false)!.GetNumber());
        }

        [TestMethod]
        [DataRow("1e-400")]
        [DataRow("-1e-400")]
        [DataRow("1e-100000")]
        [DataRow("[1e-400]")]
        [DataRow("{\"value\":1e-400}")]
        public void TestParseRejectsNonzeroUnderflow(string json)
        {
            Assert.ThrowsExactly<FormatException>(() => JToken.Parse(json));
        }

        [TestMethod]
        [DataRow("0e-400")]
        [DataRow("-0e-400")]
        [DataRow("0.000e-400")]
        public void TestParseAcceptsZeroLiterals(string json)
        {
            Assert.AreEqual(0, JToken.Parse(json)!.GetNumber());
        }
    }
}
