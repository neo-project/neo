// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Murmur128.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Murmur128
    {
        [TestMethod]
        public void TestGetHashSize()
        {
            Murmur128 murmur128 = new Murmur128(1);
            Assert.AreEqual(128, murmur128.HashSize);
        }

        [TestMethod]
        public void TestHashCore()
        {
            byte[] array = Encoding.ASCII.GetBytes("hello");
            Assert.AreEqual("0bc59d0ad25fde2982ed65af61227a0e", array.Murmur128(123u).ToHexString());

            array = Encoding.ASCII.GetBytes("world");
            Assert.AreEqual("3d3810fed480472bd214a14023bb407f", array.Murmur128(123u).ToHexString());

            array = Encoding.ASCII.GetBytes("hello world");
            Assert.AreEqual("e0a0632d4f51302c55e3b3e48d28795d", array.Murmur128(123u).ToHexString());

            array = "718f952132679baa9c5c2aa0d329fd2a".HexToBytes();
            Assert.AreEqual("9b4aa747ff0cf4e41b3d96251551c8ae", array.Murmur128(123u).ToHexString());
        }

        [TestMethod]
        public void TestTryComputeHash()
        {
            var murmur128 = new Murmur128(123u);
            var buffer = new byte[murmur128.HashSize / 8];
            var ok = murmur128.TryComputeHash("hello world"u8.ToArray(), buffer, out _);
            Assert.IsTrue(ok);
            Assert.AreEqual("e0a0632d4f51302c55e3b3e48d28795d", buffer.ToHexString());
        }
    }
}
