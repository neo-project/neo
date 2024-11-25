// Copyright (C) 2015-2024 The Neo Project.
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
            key = key.Add(new byte[] { 3, 4 });
            Assert.AreEqual("01000000020304", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add(new byte[] { 3, 4 });
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
        public void TestAdd()
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
    }
}
