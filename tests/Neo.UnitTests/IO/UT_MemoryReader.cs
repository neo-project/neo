// Copyright (C) 2015-2024 The Neo Project.
//
// UT_MemoryReader.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using System;
using System.IO;
using System.Text;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_MemoryReader
    {
        [TestMethod]
        public void TestReadFixedString()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.WriteFixedString("AA", Encoding.UTF8.GetBytes("AA").Length + 1);
            MemoryReader reader = new(stream.ToArray());
            string result = reader.ReadFixedString(Encoding.UTF8.GetBytes("AA").Length + 1);
            Assert.AreEqual("AA", result);
        }

        [TestMethod]
        public void TestReadVarString()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.WriteVarString("AAAAAAA");
            MemoryReader reader = new(stream.ToArray());
            string result = reader.ReadVarString(10);
            Assert.AreEqual("AAAAAAA", result);
        }

        [TestMethod]
        public void TestReadNullableArray()
        {
            byte[] bs = "0400000000".HexToBytes();
            MemoryReader reader = new(bs);
            var n = reader.ReadNullableArray<UInt256>();
            Assert.AreEqual(5, reader.Position);
        }

        [TestMethod]
        public void TestReadSByte()
        {
            sbyte value = -5;
            byte[] byteArray = new byte[1];
            byteArray[0] = (byte)value;
            MemoryReader reader = new(byteArray);
            var n = reader.ReadSByte();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadInt32()
        {
            int value = -5;
            byte[] bytes = BitConverter.GetBytes(value);
            MemoryReader reader = new(bytes);
            var n = reader.ReadInt32();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadUInt64()
        {
            ulong value = 12345;
            byte[] bytes = BitConverter.GetBytes(value);
            MemoryReader reader = new(bytes);
            var n = reader.ReadUInt64();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadInt16BigEndian()
        {
            short value = 12345;
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            MemoryReader reader = new(bytes);
            var n = reader.ReadInt16BigEndian();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadUInt16BigEndian()
        {
            ushort value = 12345;
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            MemoryReader reader = new(bytes);
            var n = reader.ReadUInt16BigEndian();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadInt32BigEndian()
        {
            int value = 12345;
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            MemoryReader reader = new(bytes);
            var n = reader.ReadInt32BigEndian();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadUInt32BigEndian()
        {
            uint value = 12345;
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            MemoryReader reader = new(bytes);
            var n = reader.ReadUInt32BigEndian();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadInt64BigEndian()
        {
            long value = 12345;
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            MemoryReader reader = new(bytes);
            var n = reader.ReadInt64BigEndian();
            n.Should().Be(value);
        }

        [TestMethod]
        public void TestReadUInt64BigEndian()
        {
            ulong value = 12345;
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            MemoryReader reader = new(bytes);
            var n = reader.ReadUInt64BigEndian();
            n.Should().Be(value);
        }
    }
}
