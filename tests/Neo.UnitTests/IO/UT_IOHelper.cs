// Copyright (C) 2015-2026 The Neo Project.
//
// UT_IOHelper.cs file belongs to the neo project and is free
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
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_IOHelper
    {
        [TestMethod]
        public void TestAsSerializableGeneric()
        {
            var caseArray = Enumerable.Repeat((byte)0x00, 20).ToArray();
            var result = caseArray.AsSerializable<UInt160>();
            Assert.AreEqual(UInt160.Zero, result);
        }

        [TestMethod]
        public void TestReadFixedBytes()
        {
            byte[] data = [0x01, 0x02, 0x03, 0x04];

            // Less data
            using (var reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8, false))
            {
                var result = reader.ReadFixedBytes(3);

                Assert.AreEqual("010203", result.ToHexString());
                Assert.AreEqual(3, reader.BaseStream.Position);
            }

            // Same data
            using (var reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8, false))
            {
                var result = reader.ReadFixedBytes(4);

                Assert.AreEqual("01020304", result.ToHexString());
                Assert.AreEqual(4, reader.BaseStream.Position);
            }

            // More data
            using (var reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8, false))
            {
                Assert.ThrowsExactly<FormatException>(() => _ = reader.ReadFixedBytes(5));
                Assert.AreEqual(4, reader.BaseStream.Position);
            }
        }

        [TestMethod]
        public void TestNullableArray()
        {
            var caseArray = new UInt160[]
            {
                null,
                UInt160.Zero,
                new UInt160(
                [
                    0xAA,0x00,0x00,0x00,0x00,
                    0xBB,0x00,0x00,0x00,0x00,
                    0xCC,0x00,0x00,0x00,0x00,
                    0xDD,0x00,0x00,0x00,0x00
                ])
            };

            byte[] data;
            using var stream = new MemoryStream();
            using var writter = new BinaryWriter(stream);
            {
                writter.WriteNullableArray(caseArray);
                data = stream.ToArray();
            }

            // Read Error
            Assert.ThrowsExactly<FormatException>(() =>
            {
                var reader = new MemoryReader(data);
                reader.ReadNullableArray<UInt160>(2);
                Assert.Fail();
            });

            // Read 100%
            var reader = new MemoryReader(data);
            var read = reader.ReadNullableArray<UInt160>();
            CollectionAssert.AreEqual(caseArray, read);
        }

        [TestMethod]
        public void TestAsSerializable()
        {
            var caseArray = Enumerable.Repeat((byte)0x00, 20).ToArray();
            var result = caseArray.AsSerializable<UInt160>();
            Assert.AreEqual(UInt160.Zero, result);
        }

        [TestMethod]
        public void TestCompression()
        {
            var data = new byte[] { 1, 2, 3, 4 };
            var byteArray = data.CompressLz4();
            var result = byteArray.Span.DecompressLz4(byte.MaxValue);

            CollectionAssert.AreEqual(result, data);

            // Compress

            data = new byte[255];
            for (int x = 0; x < data.Length; x++) data[x] = 1;

            byteArray = data.CompressLz4();
            result = byteArray.Span.DecompressLz4(byte.MaxValue);

            Assert.IsLessThan(result.Length, byteArray.Length);
            CollectionAssert.AreEqual(result, data);

            // Error max length

            Assert.ThrowsExactly<FormatException>(() => _ = byteArray.Span.DecompressLz4(byte.MaxValue - 1));
            Assert.ThrowsExactly<FormatException>(() => _ = byteArray.Span.DecompressLz4(-1));

            // Error length

            byte[] data_wrong = byteArray.ToArray();
            data_wrong[0]++;
            Assert.ThrowsExactly<FormatException>(() => _ = data_wrong.DecompressLz4(byte.MaxValue));
        }

        [TestMethod]
        public void TestAsSerializableArray()
        {
            byte[] byteArray = new UInt160[] { UInt160.Zero }.ToByteArray();
            UInt160[] result = byteArray.AsSerializableArray<UInt160>();
            Assert.HasCount(1, result);
            Assert.AreEqual(UInt160.Zero, result[0]);
        }

        [TestMethod]
        public void TestReadSerializable()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(UInt160.Zero);

            var reader = new MemoryReader(stream.ToArray());
            var result = reader.ReadSerializable<UInt160>();
            Assert.AreEqual(UInt160.Zero, result);
        }

        [TestMethod]
        public void TestReadSerializableArray()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write([UInt160.Zero]);

            var reader = new MemoryReader(stream.ToArray());
            var resultArray = reader.ReadSerializableArray<UInt160>();
            Assert.HasCount(1, resultArray);
            Assert.AreEqual(UInt160.Zero, resultArray[0]);
        }

        [TestMethod]
        public void TestReadVarBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.WriteVarBytes([0xAA, 0xAA]);
            stream.Seek(0, SeekOrigin.Begin);

            var reader = new BinaryReader(stream);
            var byteArray = reader.ReadVarBytes(10);
            Assert.AreEqual(Encoding.Default.GetString([0xAA, 0xAA]), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestReadVarInt()
        {
            for (int i = 0; i < 4; i++)
            {
                if (i == 0)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteVarInt(0xFFFF);
                    stream.Seek(0, SeekOrigin.Begin);

                    var reader = new BinaryReader(stream);
                    var result = reader.ReadVarInt(0xFFFF);
                    Assert.AreEqual((ulong)0xFFFF, result);
                }
                else if (i == 1)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteVarInt(0xFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    var reader = new BinaryReader(stream);
                    var result = reader.ReadVarInt(0xFFFFFFFF);
                    Assert.AreEqual(0xFFFFFFFF, result);
                }
                else
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteVarInt(0xFFFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);

                    var reader = new BinaryReader(stream);
                    Action action = () => reader.ReadVarInt(0xFFFFFFFF);
                    Assert.ThrowsExactly<FormatException>(action);
                }
            }
        }

        [TestMethod]
        public void TestToArray()
        {
            var byteArray = UInt160.Zero.ToArray();
            Assert.AreEqual(Encoding.Default.GetString(Enumerable.Repeat((byte)0x00, 20).ToArray()), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestToByteArrayGeneric()
        {
            var byteArray = new UInt160[] { UInt160.Zero }.ToByteArray();
            var expected = Enumerable.Repeat((byte)0x00, 21).ToArray();
            expected[0] = 0x01;
            Assert.AreEqual(Encoding.Default.GetString(expected), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWrite()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(UInt160.Zero);
            stream.Seek(0, SeekOrigin.Begin);

            var byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(Enumerable.Repeat((byte)0x00, 20).ToArray()), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteGeneric()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write([UInt160.Zero]);
            stream.Seek(0, SeekOrigin.Begin);

            var byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);

            var expected = Enumerable.Repeat((byte)0x00, 21).ToArray();
            expected[0] = 0x01;
            Assert.AreEqual(Encoding.Default.GetString(expected), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteFixedString()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    Action action = () => writer.WriteFixedString(null, 0);
                    Assert.ThrowsExactly<ArgumentNullException>(action);
                }
                else if (i == 1)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    Action action = () => writer.WriteFixedString("AA", Encoding.UTF8.GetBytes("AA").Length - 1);
                    Assert.ThrowsExactly<ArgumentException>(action);
                }
                else if (i == 2)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    Action action = () => writer.WriteFixedString("拉拉", Encoding.UTF8.GetBytes("拉拉").Length - 1);
                    Assert.ThrowsExactly<ArgumentException>(action);
                }
                else if (i == 3)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteFixedString("AA", Encoding.UTF8.GetBytes("AA").Length + 1);
                    stream.Seek(0, SeekOrigin.Begin);

                    var byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);

                    var expected = new byte[Encoding.UTF8.GetBytes("AA").Length + 1];
                    Encoding.UTF8.GetBytes("AA").CopyTo(expected, 0);
                    Assert.AreEqual(Encoding.Default.GetString(expected), Encoding.Default.GetString(byteArray));
                }
            }
        }

        [TestMethod]
        public void TestWriteVarBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.WriteVarBytes([0xAA]);
            stream.Seek(0, SeekOrigin.Begin);

            var byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString([0x01, 0xAA]), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteVarInt()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    Action action = () => writer.WriteVarInt(-1);
                    Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);
                }
                else if (i == 1)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteVarInt(0xFC);
                    stream.Seek(0, SeekOrigin.Begin);

                    var byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFC, byteArray[0]);
                }
                else if (i == 2)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteVarInt(0xFFFF);
                    stream.Seek(0, SeekOrigin.Begin);

                    var byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFD, byteArray[0]);
                    Assert.AreEqual(Encoding.Default.GetString([0xFF, 0xFF]),
                        Encoding.Default.GetString(byteArray.Skip(1).Take(byteArray.Length - 1).ToArray()));
                }
                else if (i == 3)
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteVarInt(0xFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);

                    var byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFE, byteArray[0]);
                    Assert.AreEqual(0xFFFFFFFF, BitConverter.ToUInt32(byteArray, 1));
                }
                else
                {
                    using var stream = new MemoryStream();
                    using var writer = new BinaryWriter(stream);
                    writer.WriteVarInt(0xAEFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);

                    var byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFF, byteArray[0]);
                    Assert.AreEqual(Encoding.Default.GetString([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00]),
                        Encoding.Default.GetString(byteArray.Skip(1).Take(byteArray.Length - 1).ToArray()));
                }
            }
        }

        [TestMethod]
        public void TestWriteVarString()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.WriteVarString("a");
            stream.Seek(0, SeekOrigin.Begin);

            var byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(0x01, byteArray[0]);
            Assert.AreEqual(0x61, byteArray[1]);
        }
    }
}
