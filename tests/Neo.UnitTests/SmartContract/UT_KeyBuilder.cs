// Copyright (C) 2015-2025 The Neo Project.
//
// UT_KeyBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_KeyBuilder
    {
        [TestMethod]
        public void Test()
        {
            var key = new KeyBuilder(1, 2);

            Assert.AreEqual("0100000002", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add([3, 4]);
            Assert.AreEqual("01000000020304", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add([3, 4]);
            key = key.Add(UInt160.Zero);
            Assert.AreEqual("010000000203040000000000000000000000000000000000000000", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.AddBigEndian(123);
            Assert.AreEqual("01000000020000007b", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 0);
            key = key.AddBigEndian(1);
            Assert.AreEqual("010000000000000001", key.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestAddInt()
        {
            var key = new KeyBuilder(1, 2);
            Assert.AreEqual("0100000002", key.ToArray().ToHexString());

            // add int
            key = new KeyBuilder(1, 2);
            key = key.AddBigEndian(-1);
            key = key.AddBigEndian(2);
            key = key.AddBigEndian(3);
            Assert.AreEqual("0100000002ffffffff0000000200000003", key.ToArray().ToHexString());

            // add ulong
            key = new KeyBuilder(1, 2);
            key = key.AddBigEndian(1ul);
            key = key.AddBigEndian(2ul);
            key = key.AddBigEndian(ulong.MaxValue);
            Assert.AreEqual("010000000200000000000000010000000000000002ffffffffffffffff", key.ToArray().ToHexString());

            // add uint
            key = new KeyBuilder(1, 2);
            key = key.AddBigEndian(1u);
            key = key.AddBigEndian(2u);
            key = key.AddBigEndian(uint.MaxValue);
            Assert.AreEqual("01000000020000000100000002ffffffff", key.ToArray().ToHexString());

            // add byte
            key = new KeyBuilder(1, 2);
            key = key.Add((byte)1);
            key = key.Add((byte)2);
            key = key.Add((byte)3);
            Assert.AreEqual("0100000002010203", key.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestAddUInt()
        {
            var key = new KeyBuilder(1, 2);
            var value = new byte[UInt160.Length];
            for (int i = 0; i < value.Length; i++)
                value[i] = (byte)i;

            key = key.Add(new UInt160(value));
            Assert.AreEqual("0100000002000102030405060708090a0b0c0d0e0f10111213", key.ToArray().ToHexString());

            var key2 = new KeyBuilder(1, 2);
            key2 = key2.Add((ISerializableSpan)new UInt160(value));

            // It must be same before and after optimization.
            Assert.AreEqual(key.ToArray().ToHexString(), key2.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            value = new byte[UInt256.Length];
            for (int i = 0; i < value.Length; i++)
                value[i] = (byte)i;
            key = key.Add(new UInt256(value));
            Assert.AreEqual("0100000002000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f", key.ToArray().ToHexString());

            key2 = new KeyBuilder(1, 2);
            key2 = key2.Add((ISerializableSpan)new UInt256(value));

            // It must be same before and after optimization.
            Assert.AreEqual(key.ToArray().ToHexString(), key2.ToArray().ToHexString());
        }
    }
}
