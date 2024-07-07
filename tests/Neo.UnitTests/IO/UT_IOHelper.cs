// Copyright (C) 2015-2024 The Neo Project.
//
// UT_IOHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using System;
using System.Collections.Generic;
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
        public void TestGetVarSizeInt()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    int result = UnsafeData.GetVarSize(1);
                    Assert.AreEqual(1, result);
                }
                else if (i == 1)
                {
                    int result = UnsafeData.GetVarSize(0xFFFF);
                    Assert.AreEqual(3, result);
                }
                else
                {
                    int result = UnsafeData.GetVarSize(0xFFFFFF);
                    Assert.AreEqual(5, result);
                }
            }
        }
        enum TestEnum0 : sbyte
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum1 : byte
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum2 : short
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum3 : ushort
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum4 : int
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum5 : uint
        {
            case1 = 1, case2 = 2
        }

        enum TestEnum6 : long
        {
            case1 = 1, case2 = 2
        }

        [TestMethod]
        public void TestGetVarSizeGeneric()
        {
            for (int i = 0; i < 9; i++)
            {
                if (i == 0)
                {
                    int result = new UInt160[] { UInt160.Zero }.GetVarSize();
                    Assert.AreEqual(21, result);
                }
                else if (i == 1)//sbyte
                {
                    List<TestEnum0> initList = new()
                    {
                        TestEnum0.case1
                    };
                    IReadOnlyCollection<TestEnum0> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(2, result);
                }
                else if (i == 2)//byte
                {
                    List<TestEnum1> initList = new()
                    {
                        TestEnum1.case1
                    };
                    IReadOnlyCollection<TestEnum1> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(2, result);
                }
                else if (i == 3)//short
                {
                    List<TestEnum2> initList = new()
                    {
                        TestEnum2.case1
                    };
                    IReadOnlyCollection<TestEnum2> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(3, result);
                }
                else if (i == 4)//ushort
                {
                    List<TestEnum3> initList = new()
                    {
                        TestEnum3.case1
                    };
                    IReadOnlyCollection<TestEnum3> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(3, result);
                }
                else if (i == 5)//int
                {
                    List<TestEnum4> initList = new()
                    {
                        TestEnum4.case1
                    };
                    IReadOnlyCollection<TestEnum4> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(5, result);
                }
                else if (i == 6)//uint
                {
                    List<TestEnum5> initList = new()
                    {
                        TestEnum5.case1
                    };
                    IReadOnlyCollection<TestEnum5> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(5, result);
                }
                else if (i == 7)//long
                {
                    List<TestEnum6> initList = new()
                    {
                        TestEnum6.case1
                    };
                    IReadOnlyCollection<TestEnum6> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(9, result);
                }
                else if (i == 8)
                {
                    List<int> initList = new()
                    {
                        1
                    };
                    IReadOnlyCollection<int> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize<int>();
                    Assert.AreEqual(5, result);
                }
            }
        }

        [TestMethod]
        public void TestGetVarSizeString()
        {
            int result = "AA".GetVarSize();
            Assert.AreEqual(3, result);
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
                    action.Should().Throw<FormatException>();
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
                    action.Should().Throw<ArgumentNullException>();
                }
                else if (i == 1)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    Action action = () => writer.WriteFixedString("AA", Encoding.UTF8.GetBytes("AA").Length - 1);
                    action.Should().Throw<ArgumentException>();
                }
                else if (i == 2)
                {
                    MemoryStream stream = new();
                    BinaryWriter writer = new(stream);
                    Action action = () => writer.WriteFixedString("拉拉", Encoding.UTF8.GetBytes("拉拉").Length - 1);
                    action.Should().Throw<ArgumentException>();
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
                    action.Should().Throw<ArgumentOutOfRangeException>();
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
