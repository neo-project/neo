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

using System.Numerics;
using System.Text.Json.Nodes;

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
        private JsonValue maxInt;
        private JsonValue minInt;
        private JsonValue zero;

        [TestInitialize]
        public void SetUp()
        {
            maxInt = JsonValue.Create(JsonConstants.MAX_SAFE_INTEGER);
            minInt = JsonValue.Create(JsonConstants.MIN_SAFE_INTEGER);
            zero = JsonValue.Create(0);
        }

        [TestMethod]
        public void TestAsString()
        {
            Action action1 = () => JsonValue.Create(double.PositiveInfinity).GetValue<string>();
            Assert.ThrowsExactly<FormatException>(action1);

            Action action2 = () => JsonValue.Create(double.NegativeInfinity).GetValue<string>();
            Assert.ThrowsExactly<FormatException>(action2);

            Action action3 = () => JsonValue.Create(double.NaN).GetValue<string>();
            Assert.ThrowsExactly<FormatException>(action3);
        }

        [TestMethod]
        public void TestGetEnum()
        {
            Assert.AreEqual(Woo.Tom, zero.GetEnum<Woo>());
            Assert.AreEqual(Woo.Jerry, JsonValue.Create(1).GetEnum<Woo>());
            Assert.AreEqual(Woo.James, JsonValue.Create(2).GetEnum<Woo>());
            Action action = () => JsonValue.Create(3).GetEnum<Woo>();
            Assert.ThrowsExactly<InvalidCastException>(action);
        }

        [TestMethod]
        public void TestEqual()
        {
            Assert.IsTrue(maxInt.Equals(JsonConstants.MAX_SAFE_INTEGER));
            Assert.IsTrue(minInt.Equals(JsonConstants.MIN_SAFE_INTEGER));
            Assert.IsFalse(zero == null);

            var jnum = JsonValue.Create(1);
            Assert.IsTrue(jnum.Equals(JsonValue.Create(1)));
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
    }
}
