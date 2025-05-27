// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_StringExtensions
    {
        [TestMethod]
        public void TestHexToBytes()
        {
            string? value = null;
            Assert.AreEqual(Array.Empty<byte>().ToHexString(), value.HexToBytes().ToHexString());

            string empty = "";
            Assert.AreEqual(Array.Empty<byte>().ToHexString(), empty.HexToBytes().ToHexString());

            string str1 = "hab";
            Action action = () => str1.HexToBytes();
            Assert.ThrowsExactly<FormatException>(action);

            string str2 = "0102";
            byte[] bytes = str2.HexToBytes();
            Assert.AreEqual(new byte[] { 0x01, 0x02 }.ToHexString(), bytes.ToHexString());

            string str3 = "0A0b0C";
            bytes = str3.AsSpan().HexToBytes();
            Assert.AreEqual(new byte[] { 0x0A, 0x0B, 0x0C }.ToHexString(), bytes.ToHexString());

            bytes = str3.AsSpan().HexToBytesReversed();
            Assert.AreEqual(new byte[] { 0x0C, 0x0B, 0x0A }.ToHexString(), bytes.ToHexString());
        }

        [TestMethod]
        public void TestGetVarSizeString()
        {
            int result = "AA".GetVarSize();
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void TestGetVarSizeInt()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    int result = 1.GetVarSize();
                    Assert.AreEqual(1, result);
                }
                else if (i == 1)
                {
                    int result = 0xFFFF.GetVarSize();
                    Assert.AreEqual(3, result);
                }
                else
                {
                    int result = 0xFFFFFF.GetVarSize();
                    Assert.AreEqual(5, result);
                }
            }
        }

        [TestMethod]
        public void TestGetStrictUTF8String()
        {
            var bytes = new byte[] { (byte)'A', (byte)'B', (byte)'C' };
            Assert.AreEqual("ABC", bytes.ToStrictUtf8String());
            Assert.AreEqual("ABC", bytes.ToStrictUtf8String(0, 3));
            Assert.AreEqual("ABC", ((ReadOnlySpan<byte>)bytes.AsSpan()).ToStrictUtf8String());

            Assert.AreEqual(bytes.ToHexString(), "ABC".ToStrictUtf8Bytes().ToHexString());
            Assert.AreEqual(bytes.Length, "ABC".GetStrictUtf8ByteCount());
        }

        [TestMethod]
        public void TestIsHex()
        {
            Assert.IsTrue("010203".IsHex());
            Assert.IsFalse("01020".IsHex());
            Assert.IsFalse("01020g".IsHex());
            Assert.IsTrue("".IsHex());
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
                    List<TestEnum0> initList = [TestEnum0.case1];
                    IReadOnlyCollection<TestEnum0> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(2, result);
                }
                else if (i == 2)//byte
                {
                    List<TestEnum1> initList = [TestEnum1.case1];
                    IReadOnlyCollection<TestEnum1> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(2, result);
                }
                else if (i == 3)//short
                {
                    List<TestEnum2> initList = [TestEnum2.case1];
                    IReadOnlyCollection<TestEnum2> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(3, result);
                }
                else if (i == 4)//ushort
                {
                    List<TestEnum3> initList = [TestEnum3.case1];
                    IReadOnlyCollection<TestEnum3> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(3, result);
                }
                else if (i == 5)//int
                {
                    List<TestEnum4> initList = [TestEnum4.case1];
                    IReadOnlyCollection<TestEnum4> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(5, result);
                }
                else if (i == 6)//uint
                {
                    List<TestEnum5> initList = [TestEnum5.case1];
                    IReadOnlyCollection<TestEnum5> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(5, result);
                }
                else if (i == 7)//long
                {
                    List<TestEnum6> initList = [TestEnum6.case1];
                    IReadOnlyCollection<TestEnum6> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
                    Assert.AreEqual(9, result);
                }
                else if (i == 8)
                {
                    List<int> initList = [1];
                    IReadOnlyCollection<int> testList = initList.AsReadOnly();
                    int result = testList.GetVarSize();
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
    }
}
