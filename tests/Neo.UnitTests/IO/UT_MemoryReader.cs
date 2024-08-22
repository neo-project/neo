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
using Newtonsoft.Json.Linq;
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
            var values = new sbyte[] { 0, 1, -1, 5, -5, sbyte.MaxValue, sbyte.MinValue };
            foreach (var v in values)
            {
                byte[] byteArray = new byte[1];
                byteArray[0] = (byte)v;
                MemoryReader reader = new(byteArray);
                var n = reader.ReadSByte();
                n.Should().Be(v);
            }

            var values2 = new long[] { (long)int.MaxValue + 1, (long)int.MinValue - 1 };
            foreach (var v in values2)
            {
                byte[] byteArray = new byte[1];
                byteArray[0] = (byte)v;
                MemoryReader reader = new(byteArray);
                var n = reader.ReadSByte();
                n.Should().Be((sbyte)v);
            }
        }

        [TestMethod]
        public void TestReadInt32()
        {
            var values = new int[] { 0, 1, -1, 5, -5, int.MaxValue, int.MinValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                MemoryReader reader = new(bytes);
                var n = reader.ReadInt32();
                n.Should().Be(v);
            }

            var values2 = new long[] { (long)int.MaxValue + 1, (long)int.MinValue - 1 };
            foreach (var v in values2)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                MemoryReader reader = new(bytes);
                var n = reader.ReadInt32();
                n.Should().Be((int)v);
            }
        }

        [TestMethod]
        public void TestReadUInt64()
        {
            var values = new ulong[] { 0, 1, 5, ulong.MaxValue, ulong.MinValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                MemoryReader reader = new(bytes);
                var n = reader.ReadUInt64();
                n.Should().Be(v);
            }

            var values2 = new long[] { long.MinValue, -1, long.MaxValue };
            foreach (var v in values2)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                MemoryReader reader = new(bytes);
                var n = reader.ReadUInt64();
                n.Should().Be((ulong)v);
            }
        }

        [TestMethod]
        public void TestReadInt16BigEndian()
        {
            var values = new short[] { short.MinValue, -1, 0, 1, 12345, short.MaxValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                MemoryReader reader = new(bytes);
                var n = reader.ReadInt16BigEndian();
                n.Should().Be(v);
            }
        }

        [TestMethod]
        public void TestReadUInt16BigEndian()
        {
            var values = new ushort[] { ushort.MinValue, 0, 1, 12345, ushort.MaxValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                MemoryReader reader = new(bytes);
                var n = reader.ReadUInt16BigEndian();
                n.Should().Be(v);
            }
        }

        [TestMethod]
        public void TestReadInt32BigEndian()
        {
            var values = new int[] { int.MinValue, -1, 0, 1, 12345, int.MaxValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                MemoryReader reader = new(bytes);
                var n = reader.ReadInt32BigEndian();
                n.Should().Be(v);
            }
        }

        [TestMethod]
        public void TestReadUInt32BigEndian()
        {
            var values = new uint[] { uint.MinValue, 0, 1, 12345, uint.MaxValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                MemoryReader reader = new(bytes);
                var n = reader.ReadUInt32BigEndian();
                n.Should().Be(v);
            }
        }

        [TestMethod]
        public void TestReadInt64BigEndian()
        {
            var values = new long[] { long.MinValue, int.MinValue, -1, 0, 1, 12345, int.MaxValue, long.MaxValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                MemoryReader reader = new(bytes);
                var n = reader.ReadInt64BigEndian();
                n.Should().Be(v);
            }
        }

        [TestMethod]
        public void TestReadUInt64BigEndian()
        {
            var values = new ulong[] { ulong.MinValue, 0, 1, 12345, ulong.MaxValue };
            foreach (var v in values)
            {
                byte[] bytes = BitConverter.GetBytes(v);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                MemoryReader reader = new(bytes);
                var n = reader.ReadUInt64BigEndian();
                n.Should().Be(v);
            }
        }
    }
}
