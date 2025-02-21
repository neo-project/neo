// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Murmur32.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System;
using System.Buffers.Binary;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Murmur32
    {
        [TestMethod]
        public void TestGetHashSize()
        {
            Assert.AreEqual(32, Murmur32.HashSizeInBits);
        }

        [TestMethod]
        public void TestHashToUInt32()
        {
            byte[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
            Assert.AreEqual(378574820u, array.Murmur32(10u));
        }

        [TestMethod]
        public void TestComputeHash()
        {
            var murmur3 = new Murmur32(10u);
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
            var buffer = murmur3.ComputeHash(data);
            var hash = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            Assert.AreEqual(378574820u, hash);
        }

        [TestMethod]
        public void TestComputeHashUInt32()
        {
            var murmur3 = new Murmur32(10u);
            var hash = murmur3.ComputeHashUInt32("hello worldhello world"u8.ToArray());
            Assert.AreEqual(60539726u, hash);

            hash = murmur3.ComputeHashUInt32("he"u8.ToArray());
            Assert.AreEqual(972873329u, hash);
        }

        [TestMethod]
        public void TestAppend()
        {
            var murmur3 = new Murmur32(10u);
            murmur3.Append("h"u8.ToArray());
            murmur3.Append("e"u8.ToArray());
            Assert.AreEqual(972873329u, murmur3.GetCurrentHashUInt32());

            murmur3.Reset();
            murmur3.Append("hello world"u8.ToArray());
            murmur3.Append("hello world"u8.ToArray());
            Assert.AreEqual(60539726u, murmur3.GetCurrentHashUInt32());

            murmur3.Reset();
            murmur3.Append("hello worldh"u8.ToArray());
            murmur3.Append("ello world"u8.ToArray());
            Assert.AreEqual(60539726u, murmur3.GetCurrentHashUInt32());

            murmur3.Reset();
            murmur3.Append("hello worldhello world"u8.ToArray());
            murmur3.Append(""u8.ToArray());
            Assert.AreEqual(60539726u, murmur3.GetCurrentHashUInt32());

            murmur3.Reset();
            murmur3.Append(""u8.ToArray());
            murmur3.Append("hello worldhello world"u8.ToArray());
            Assert.AreEqual(60539726u, murmur3.GetCurrentHashUInt32());

            // random data, random split
            var random = new Random();
            var data = new byte[random.Next(1, 2048)];
            random.NextBytes(data);
            for (int i = 0; i < 32; i++)
            {
                var split = random.Next(1, data.Length - 1);
                murmur3.Reset();
                murmur3.Append(data.AsSpan(0, split));
                murmur3.Append(data.AsSpan(split));
                Assert.AreEqual(data.Murmur32(10u), murmur3.GetCurrentHashUInt32());
            }
        }
    }
}
