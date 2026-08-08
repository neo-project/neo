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

using System.Globalization;
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
            Assert.IsTrue(jnum.Equals(new BigInteger(1)));
            Assert.IsFalse(jnum.Equals(new BigInteger(2)));
        }

        [TestMethod]
        public void TestBigInteger_ImplicitConversion_AndExactWrite()
        {
            BigInteger huge = BigInteger.Parse("100000000000000000000000");
            JNumber number = huge;
            JToken token = huge;

            Assert.IsInstanceOfType<JNumber>(token);
            Assert.AreEqual(huge, number.GetBigInteger());
            Assert.IsTrue(number.TryGetBigInteger(out var bi));
            Assert.AreEqual(huge, bi);

            // Exact JSON number literal (not a string, not scientific notation with loss).
            Assert.AreEqual("100000000000000000000000", number.ToString());
            Assert.AreEqual("100000000000000000000000", token.ToString());

            var obj = new JObject { ["Value"] = huge };
            Assert.AreEqual("""{"Value":100000000000000000000000}""", obj.ToString());
        }

        [TestMethod]
        public void TestBigInteger_SafeRange_StoredAsDouble()
        {
            BigInteger safe = 42;
            var number = JNumber.FromBigInteger(safe);
            Assert.AreEqual(42d, number.Value);
            Assert.AreEqual("42", number.ToString());
            Assert.IsTrue(number.Equals(42));
            Assert.IsTrue(number.Equals(new BigInteger(42)));
        }

        [TestMethod]
        public void TestBigInteger_OutsideSafeRange_KeepsExactInteger()
        {
            // Beyond MAX_SAFE_INTEGER (2^53-1): exact path only via BigInteger, not long→double.
            var outsideSafe = new BigInteger(JNumber.MAX_SAFE_INTEGER) + 2;
            JNumber fromBig = outsideSafe;
            Assert.IsTrue(fromBig.HasExactBigInteger);
            Assert.IsTrue(fromBig.TryGetExactBigInteger(out var exact));
            Assert.AreEqual(outsideSafe, exact);
            Assert.AreEqual(outsideSafe.ToString(CultureInfo.InvariantCulture), fromBig.ToString());
        }

        [TestMethod]
        public void TestBigInteger_ParseRoundTrip_ExactIntegers()
        {
            const string json = """{"Value":100000000000000000000000}""";
            var parsed = (JObject)JToken.Parse(json, exactIntegers: true)!;
            var number = (JNumber)parsed["Value"]!;
            Assert.AreEqual(BigInteger.Parse("100000000000000000000000"), number.GetBigInteger());
            Assert.AreEqual(json, parsed.ToString());
        }

        [TestMethod]
        public void TestBigInteger_Parse_DefaultIsDouble_NotExact()
        {
            // Default parse keeps historical double semantics (consensus-safe pre-HF_Huyao).
            const string json = """{"Value":100000000000000000000000}""";
            var parsed = (JObject)JToken.Parse(json)!;
            var number = (JNumber)parsed["Value"]!;
            // Double cannot represent this integer exactly; GetBigInteger reflects the rounded value.
            Assert.AreNotEqual(BigInteger.Parse("100000000000000000000000"), number.GetBigInteger());
        }

        [TestMethod]
        public void TestBigInteger_FractionalDouble_NotInteger()
        {
            var number = new JNumber(1.5);
            Assert.IsFalse(number.TryGetBigInteger(out _));
            Assert.ThrowsExactly<InvalidCastException>(() => _ = number.GetBigInteger());
        }
    }
}
