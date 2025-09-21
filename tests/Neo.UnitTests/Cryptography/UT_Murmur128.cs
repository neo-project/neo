// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Murmur128.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Extensions.Factories;
using System;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Murmur128
    {
        [TestMethod]
        public void TestGetHashSize()
        {
            Assert.AreEqual(128, Murmur128.HashSizeInBits);
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
        public void TestComputeHash128()
        {
            var murmur128 = new Murmur128(123u);
            var buffer = murmur128.ComputeHash("hello world"u8.ToArray());
            Assert.AreEqual("e0a0632d4f51302c55e3b3e48d28795d", buffer.ToHexString());

            murmur128.Reset();
            murmur128.Append("hello "u8.ToArray());
            murmur128.Append("world"u8.ToArray());
            buffer = murmur128.GetCurrentHash();
            Assert.AreEqual("e0a0632d4f51302c55e3b3e48d28795d", buffer.ToHexString());

            murmur128.Reset();
            murmur128.Append("hello worldhello world"u8.ToArray());
            buffer = murmur128.GetCurrentHash();
            Assert.AreEqual("76f870485d4e69f8302d4b3fad28fd39", buffer.ToHexString());

            murmur128.Reset();
            murmur128.Append("hello world"u8.ToArray());
            murmur128.Append("hello world"u8.ToArray());
            buffer = murmur128.GetCurrentHash();
            Assert.AreEqual("76f870485d4e69f8302d4b3fad28fd39", buffer.ToHexString());

            murmur128.Reset();
            murmur128.Append("hello worldhello "u8.ToArray());
            murmur128.Append("world"u8.ToArray());
            buffer = murmur128.GetCurrentHash();
            Assert.AreEqual("76f870485d4e69f8302d4b3fad28fd39", buffer.ToHexString());
        }

        [TestMethod]
        public void TestAppend()
        {
            var buffer = new byte[RandomNumberFactory.NextInt32(2, 2048)];
            Random.Shared.NextBytes(buffer);
            for (int i = 0; i < 100; i++)
            {
                int split = RandomNumberFactory.NextInt32(1, buffer.Length - 1);
                var murmur128 = new Murmur128(123u);
                murmur128.Append(buffer.AsSpan(0, split));
                murmur128.Append(buffer.AsSpan(split));
                Assert.AreEqual(murmur128.GetCurrentHash().ToHexString(), buffer.Murmur128(123u).ToHexString());
            }
        }
    }
}
