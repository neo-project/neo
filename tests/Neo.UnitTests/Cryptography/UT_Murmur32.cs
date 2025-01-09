// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Murmur32.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System.Buffers.Binary;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Murmur32
    {
        [TestMethod]
        public void TestGetHashSize()
        {
            Murmur32 murmur3 = new Murmur32(1);
            murmur3.HashSize.Should().Be(32);
        }

        [TestMethod]
        public void TestHashToUInt32()
        {
            byte[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
            array.Murmur32(10u).Should().Be(378574820u);
        }

        [TestMethod]
        public void TestComputeHash()
        {
            var murmur3 = new Murmur32(10u);
            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
            var buffer = murmur3.ComputeHash(data);
            var hash = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            hash.Should().Be(378574820u);
        }

        [TestMethod]
        public void TestComputeHashUInt32()
        {
            var murmur3 = new Murmur32(10u);
            var hash = murmur3.ComputeHashUInt32("hello worldhello world"u8.ToArray());
            hash.Should().Be(60539726u);
        }
    }
}
