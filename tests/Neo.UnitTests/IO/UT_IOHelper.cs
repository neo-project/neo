// Copyright (C) 2015-2025 The Neo Project.
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
            byte[] caseArray = new byte[] { 0x00,0x00,0x00,0x00,0x00,
                                            0x00,0x00,0x00,0x00,0x00,
                                            0x00,0x00,0x00,0x00,0x00,
                                            0x00,0x00,0x00,0x00,0x00 };
            UInt160 result = caseArray.AsSerializable<UInt160>();
            Assert.AreEqual(UInt160.Zero, result);
        }

        [TestMethod]
        public void TestReadFixedBytes()
        {
            byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Less data

            using (BinaryReader reader = new(new MemoryStream(data), Encoding.UTF8, false))
            {
                byte[] result = reader.ReadFixedBytes(3);

                Assert.AreEqual("010203", result.ToHexString());
                Assert.AreEqual(3, reader.BaseStream.Position);
            }

            // Same data

            using (BinaryReader reader = new(new MemoryStream(data), Encoding.UTF8, false))
            {
                byte[] result = reader.ReadFixedBytes(4);

                Assert.AreEqual("01020304", result.ToHexString());
                Assert.AreEqual(4, reader.BaseStream.Position);
            }

            // More data

            using (BinaryReader reader = new(new MemoryStream(data), Encoding.UTF8, false))
            {
                Assert.ThrowsException<FormatException>(() => reader.ReadFixedBytes(5));
                Assert.AreEqual(4, reader.BaseStream.Position);
            }
        }

        [TestMethod]
        public void TestNullableArray()
        {
            var caseArray = new UInt160[]
            {
                null, UInt160.Zero, new UInt160(
                new byte[] {
                    0xAA,0x00,0x00,0x00,0x00,
                    0xBB,0x00,0x00,0x00,0x00,
                    0xCC,0x00,0x00,0x00,0x00,
                    0xDD,0x00,0x00,0x00,0x00
                })
            };

            byte[] data;
            using (MemoryStream stream = new())
            using (BinaryWriter writter = new(stream))
            {
                writter.WriteNullableArray(caseArray);
                data = stream.ToArray();
            }

            // Read Error

            Assert.ThrowsException<FormatException>(() =>
            {
                var reader = new MemoryReader(data);
                reader.ReadNullableArray<UInt160>(2);
                Assert.Fail();
            });

            // Read 100%

            MemoryReader reader = new(data);
            var read = reader.ReadNullableArray<UInt160>();
            CollectionAssert.AreEqual(caseArray, read);
        }

        [TestMethod]
        public void TestAsSerializable()
        {
            byte[] caseArray = [0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00,
                0x00];
            ISerializable result = caseArray.AsSerializable<UInt160>();
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

            Assert.IsTrue(byteArray.Length < result.Length);
            CollectionAssert.AreEqual(result, data);

            // Error max length

            Assert.ThrowsException<FormatException>(() => byteArray.Span.DecompressLz4(byte.MaxValue - 1));
            Assert.ThrowsException<FormatException>(() => byteArray.Span.DecompressLz4(-1));

            // Error length

            byte[] data_wrong = byteArray.ToArray();
            data_wrong[0]++;
            Assert.ThrowsException<FormatException>(() => data_wrong.DecompressLz4(byte.MaxValue));
        }

        [TestMethod]
        public void TestAsSerializableArray()
        {
            byte[] byteArray = new UInt160[] { UInt160.Zero }.ToByteArray();
            UInt160[] result = byteArray.AsSerializableArray<UInt160>();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(UInt160.Zero, result[0]);
        }

        [TestMethod]
        public void TestReadSerializable()
        {
            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            writer.Write(UInt160.Zero);
            MemoryReader reader = new(stream.ToArray());
            UInt160 result = reader.ReadSerializable<UInt160>();
            Assert.AreEqual(UInt160.Zero, result);
        }

        [TestMethod]
        public void TestReadSerializableArray()
        {
            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            writer.Write(new UInt160[] { UInt160.Zero });
            MemoryReader reader = new(stream.ToArray());
            UInt160[] resultArray = reader.ReadSerializableArray<UInt160>();
            Assert.AreEqual(1, resultArray.Length);
            Assert.AreEqual(UInt160.Zero, resultArray[0]);
        }

        [TestMethod]
        public void TestReadVarBytes()
        {
            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            writer.WriteVarBytes(new byte[] { 0xAA, 0xAA });
            stream.Seek(0, SeekOrigin.Begin);
            BinaryReader reader = new(stream);
            byte[] byteArray = reader.ReadVarBytes(10);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0xAA, 0xAA }), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestReadVarInt()
        {
            for (int i = 0; i < 4; i++)
            {
                if (i == 0)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteVarInt(0xFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    BinaryReader reader = new(stream);
                    ulong result = reader.ReadVarInt(0xFFFF);
                    Assert.AreEqual((ulong)0xFFFF, result);
                }
                else if (i == 1)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteVarInt(0xFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    BinaryReader reader = new(stream);
                    ulong result = reader.ReadVarInt(0xFFFFFFFF);
                    Assert.AreEqual(0xFFFFFFFF, result);
                }
                else
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteVarInt(0xFFFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    BinaryReader reader = new(stream);
                    Action action = () => reader.ReadVarInt(0xFFFFFFFF);
                    Assert.ThrowsException<FormatException>(action);
                }
            }
        }

        [TestMethod]
        public void TestToArray()
        {
            byte[] byteArray = UInt160.Zero.ToArray();
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x00,0x00,0x00,0x00,0x00,
                                                                    0x00,0x00,0x00,0x00,0x00,
                                                                    0x00,0x00,0x00,0x00,0x00,
                                                                    0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestToByteArrayGeneric()
        {
            byte[] byteArray = new UInt160[] { UInt160.Zero }.ToByteArray();
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01,0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWrite()
        {
            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            writer.Write(UInt160.Zero);
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x00,0x00,0x00,0x00,0x00,
                                                                    0x00,0x00,0x00,0x00,0x00,
                                                                    0x00,0x00,0x00,0x00,0x00,
                                                                    0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteGeneric()
        {
            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            writer.Write(new UInt160[] { UInt160.Zero });
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01,0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00,
                                                                         0x00,0x00,0x00,0x00,0x00}), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteFixedString()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    Action action = () => writer.WriteFixedString(null, 0);
                    Assert.ThrowsException<ArgumentNullException>(action);
                }
                else if (i == 1)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    Action action = () => writer.WriteFixedString("AA", Encoding.UTF8.GetBytes("AA").Length - 1);
                    Assert.ThrowsException<ArgumentException>(action);
                }
                else if (i == 2)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    Action action = () => writer.WriteFixedString("拉拉", Encoding.UTF8.GetBytes("拉拉").Length - 1);
                    Assert.ThrowsException<ArgumentException>(action);
                }
                else if (i == 3)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteFixedString("AA", Encoding.UTF8.GetBytes("AA").Length + 1);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    byte[] newArray = new byte[Encoding.UTF8.GetBytes("AA").Length + 1];
                    Encoding.UTF8.GetBytes("AA").CopyTo(newArray, 0);
                    Assert.AreEqual(Encoding.Default.GetString(newArray), Encoding.Default.GetString(byteArray));
                }
            }
        }

        [TestMethod]
        public void TestWriteVarBytes()
        {
            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            writer.WriteVarBytes(new byte[] { 0xAA });
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01, 0xAA }), Encoding.Default.GetString(byteArray));
        }

        [TestMethod]
        public void TestWriteVarInt()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    Action action = () => writer.WriteVarInt(-1);
                    Assert.ThrowsException<ArgumentOutOfRangeException>(action);
                }
                else if (i == 1)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteVarInt(0xFC);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFC, byteArray[0]);
                }
                else if (i == 2)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteVarInt(0xFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFD, byteArray[0]);
                    Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0xFF, 0xFF }), Encoding.Default.GetString(byteArray.Skip(1).Take(byteArray.Length - 1).ToArray()));
                }
                else if (i == 3)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteVarInt(0xFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFE, byteArray[0]);
                    Assert.AreEqual(0xFFFFFFFF, BitConverter.ToUInt32(byteArray, 1));
                }
                else
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    writer.WriteVarInt(0xAEFFFFFFFF);
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] byteArray = new byte[stream.Length];
                    stream.Read(byteArray, 0, (int)stream.Length);
                    Assert.AreEqual(0xFF, byteArray[0]);
                    Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00 }), Encoding.Default.GetString(byteArray.Skip(1).Take(byteArray.Length - 1).ToArray()));
                }
            }
        }

        [TestMethod]
        public void TestWriteVarString()
        {
            MemoryStream stream = new();
            BinaryWriter writer = new(stream);
            writer.WriteVarString("a");
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            Assert.AreEqual(0x01, byteArray[0]);
            Assert.AreEqual(0x61, byteArray[1]);
        }
    }
}
