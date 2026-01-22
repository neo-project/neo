// Copyright (C) 2015-2026 The Neo Project.
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
using System.Text;

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
        public void TestTrimStartIgnoreCase()
        {
            Assert.AreEqual("010203", "0x010203".AsSpan().TrimStartIgnoreCase("0x").ToString());
            Assert.AreEqual("010203", "0x010203".AsSpan().TrimStartIgnoreCase("0X").ToString());
            Assert.AreEqual("010203", "0X010203".AsSpan().TrimStartIgnoreCase("0x").ToString());
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

        #region Exception Message Tests

        [TestMethod]
        public void TestToStrictUtf8String_ByteArray_WithInvalidBytes_ShouldThrowWithDetailedMessage()
        {
            // Test invalid UTF-8 bytes
            byte[] invalidUtf8 = new byte[] { 0xFF, 0xFE, 0xFD };

            var ex = Assert.ThrowsExactly<DecoderFallbackException>(() => invalidUtf8.ToStrictUtf8String());

            Assert.IsTrue(ex.Message.Contains("Failed to decode byte array to UTF-8 string (strict mode)"));
            Assert.IsTrue(ex.Message.Contains("invalid UTF-8 byte sequences"));
            Assert.IsTrue(ex.Message.Contains("FF-FE-FD"));
            Assert.IsTrue(ex.Message.Contains("Ensure all bytes form valid UTF-8 character sequences"));
        }

        [TestMethod]
        public void TestToStrictUtf8String_ByteArray_WithNull_ShouldThrowWithParameterName()
        {
            byte[]? nullArray = null;

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => nullArray!.ToStrictUtf8String());

            Assert.AreEqual("value", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("Cannot decode null byte array to UTF-8 string"));
        }

        [TestMethod]
        public void TestToStrictUtf8String_ByteArrayWithRange_WithInvalidParameters_ShouldThrowWithDetailedMessage()
        {
            byte[] validArray = new byte[] { 65, 66, 67 }; // "ABC"

            // Test negative start
            var ex1 = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => validArray.ToStrictUtf8String(-1, 2));
            Assert.AreEqual("start", ex1.ParamName);
            Assert.IsTrue(ex1.Message.Contains("Start index cannot be negative"));

            // Test negative count
            var ex2 = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => validArray.ToStrictUtf8String(0, -1));
            Assert.AreEqual("count", ex2.ParamName);
            Assert.IsTrue(ex2.Message.Contains("Count cannot be negative"));

            // Test range exceeds bounds
            var ex3 = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => validArray.ToStrictUtf8String(1, 5));
            Assert.AreEqual("count", ex3.ParamName);
            Assert.IsTrue(ex3.Message.Contains("exceeds the array bounds"));
            Assert.IsTrue(ex3.Message.Contains("length: 3"));
            Assert.IsTrue(ex3.Message.Contains("start + count <= array.Length"));
        }

        [TestMethod]
        public void TestToStrictUtf8String_ReadOnlySpan_WithInvalidBytes_ShouldThrowWithDetailedMessage()
        {
            // Test invalid UTF-8 bytes
            byte[] invalidUtf8 = new byte[] { 0x80, 0x81, 0x82 };

            var ex = Assert.ThrowsExactly<DecoderFallbackException>(() => ((ReadOnlySpan<byte>)invalidUtf8).ToStrictUtf8String());

            Assert.IsTrue(ex.Message.Contains("Failed to decode byte span to UTF-8 string (strict mode)"));
            Assert.IsTrue(ex.Message.Contains("invalid UTF-8 byte sequences"));
            Assert.IsTrue(ex.Message.Contains("0x80, 0x81, 0x82"));
            Assert.IsTrue(ex.Message.Contains("Ensure all bytes form valid UTF-8 character sequences"));
        }

        [TestMethod]
        public void TestToStrictUtf8Bytes_WithNull_ShouldThrowWithParameterName()
        {
            string? nullString = null;

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => nullString!.ToStrictUtf8Bytes());

            Assert.AreEqual("value", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("Cannot encode null string to UTF-8 bytes"));
        }

        [TestMethod]
        public void TestGetStrictUtf8ByteCount_WithNull_ShouldThrowWithParameterName()
        {
            string? nullString = null;

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => nullString!.GetStrictUtf8ByteCount());

            Assert.AreEqual("value", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("Cannot get UTF-8 byte count for null string"));
        }

        [TestMethod]
        public void TestHexToBytes_String_WithInvalidLength_ShouldThrowWithDetailedMessage()
        {
            string invalidHex = "abc"; // Odd length

            var ex = Assert.ThrowsExactly<FormatException>(() => invalidHex.HexToBytes());

            Assert.IsTrue(ex.Message.Contains("Failed to convert hex string to bytes"));
            Assert.IsTrue(ex.Message.Contains("invalid hexadecimal characters"));
            Assert.IsTrue(ex.Message.Contains("Input: 'abc'"));
            Assert.IsTrue(ex.Message.Contains("Valid hex characters are 0-9, A-F, and a-f"));
        }

        [TestMethod]
        public void TestHexToBytes_String_WithInvalidCharacters_ShouldThrowWithDetailedMessage()
        {
            string invalidHex = "abgh"; // Contains 'g' and 'h'

            var ex = Assert.ThrowsExactly<FormatException>(() => invalidHex.HexToBytes());

            Assert.IsTrue(ex.Message.Contains("Failed to convert hex string to bytes"));
            Assert.IsTrue(ex.Message.Contains("invalid hexadecimal characters"));
            Assert.IsTrue(ex.Message.Contains("Input: 'abgh'"));
            Assert.IsTrue(ex.Message.Contains("Valid hex characters are 0-9, A-F, and a-f"));
        }

        [TestMethod]
        public void TestHexToBytes_ReadOnlySpan_WithInvalidCharacters_ShouldThrowWithDetailedMessage()
        {
            string invalidHex = "12xyz";

            var ex = Assert.ThrowsExactly<FormatException>(() => invalidHex.AsSpan().HexToBytes());

            Assert.IsTrue(ex.Message.Contains("Failed to convert hex span to bytes"));
            Assert.IsTrue(ex.Message.Contains("invalid hexadecimal characters"));
            Assert.IsTrue(ex.Message.Contains("Input: '12xyz'"));
            Assert.IsTrue(ex.Message.Contains("Valid hex characters are 0-9, A-F, and a-f"));
        }

        [TestMethod]
        public void TestHexToBytesReversed_WithInvalidCharacters_ShouldThrowWithDetailedMessage()
        {
            string invalidHex = "12zz";

            var ex = Assert.ThrowsExactly<FormatException>(() => invalidHex.AsSpan().HexToBytesReversed());

            Assert.IsTrue(ex.Message.Contains("Failed to convert hex span to reversed bytes"));
            Assert.IsTrue(ex.Message.Contains("invalid hexadecimal characters"));
            Assert.IsTrue(ex.Message.Contains("Input: '12zz'"));
            Assert.IsTrue(ex.Message.Contains("Valid hex characters are 0-9, A-F, and a-f"));
        }

        [TestMethod]
        public void TestGetVarSize_WithNull_ShouldThrowWithParameterName()
        {
            string? nullString = null;

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => nullString!.GetVarSize());

            Assert.AreEqual("value", ex.ParamName);
            Assert.IsTrue(ex.Message.Contains("Cannot calculate variable size for null string"));
        }

        [TestMethod]
        public void TestExceptionMessages_WithLongInputs_ShouldTruncateAppropriately()
        {
            // Test long string truncation in exception messages
            string longString = new string('a', 150);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => ((string?)null)!.GetStrictUtf8ByteCount());
            Assert.IsTrue(ex.Message.Contains("Cannot get UTF-8 byte count for null string"));

            // Test long hex string
            string longHexString = new string('z', 120); // Invalid hex with 'z'

            var hexEx = Assert.ThrowsExactly<FormatException>(() => longHexString.HexToBytes());
            Assert.IsTrue(hexEx.Message.Contains("Input length: 120 characters"));
        }

        [TestMethod]
        public void TestExceptionMessages_WithLargeByteArrays_ShouldShowLimitedBytes()
        {
            // Create a large byte array with some invalid UTF-8 sequences
            byte[] largeInvalidUtf8 = new byte[100];
            for (int i = 0; i < 100; i++)
            {
                largeInvalidUtf8[i] = 0xFF; // Invalid UTF-8
            }

            var ex = Assert.ThrowsExactly<DecoderFallbackException>(() => largeInvalidUtf8.ToStrictUtf8String());

            Assert.IsTrue(ex.Message.Contains("Length: 100 bytes"));
            Assert.IsTrue(ex.Message.Contains("First 16:"));
            Assert.IsTrue(ex.Message.Contains("FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF"));
        }

        [TestMethod]
        public void TestExceptionParameterNames_AreCorrect()
        {
            // Verify that all ArgumentException and ArgumentNullException have correct parameter names

            // ToStrictUtf8String with null byte array
            var ex1 = Assert.ThrowsExactly<ArgumentNullException>(() => ((byte[]?)null)!.ToStrictUtf8String());
            Assert.AreEqual("value", ex1.ParamName);

            // ToStrictUtf8String with invalid range parameters
            byte[] validArray = new byte[] { 65, 66, 67 };
            var ex2 = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => validArray.ToStrictUtf8String(-1, 1));
            Assert.AreEqual("start", ex2.ParamName);

            var ex3 = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => validArray.ToStrictUtf8String(0, -1));
            Assert.AreEqual("count", ex3.ParamName);

            // ToStrictUtf8Bytes with null string
            var ex4 = Assert.ThrowsExactly<ArgumentNullException>(() => ((string?)null)!.ToStrictUtf8Bytes());
            Assert.AreEqual("value", ex4.ParamName);

            // GetStrictUtf8ByteCount with null string
            var ex5 = Assert.ThrowsExactly<ArgumentNullException>(() => ((string?)null)!.GetStrictUtf8ByteCount());
            Assert.AreEqual("value", ex5.ParamName);

            // HexToBytes with invalid hex string
            var ex6 = Assert.ThrowsExactly<FormatException>(() => "abc".HexToBytes());
            // FormatException doesn't have ParamName, so we just check the message
            Assert.IsTrue(ex6.Message.Contains("Failed to convert hex string to bytes"));

            // GetVarSize with null string
            var ex7 = Assert.ThrowsExactly<ArgumentNullException>(() => ((string?)null)!.GetVarSize());
            Assert.AreEqual("value", ex7.ParamName);
        }

        [TestMethod]
        public void TestTryToStrictUtf8String_DoesNotThrowOnInvalidInput()
        {
            // Verify that TryToStrictUtf8String doesn't throw exceptions for invalid input
            byte[] invalidUtf8 = new byte[] { 0xFF, 0xFE, 0xFD };
            ReadOnlySpan<byte> span = invalidUtf8;

            bool result = span.TryToStrictUtf8String(out string? value);

            Assert.IsFalse(result);
            Assert.IsNull(value);
        }

        #endregion
    }
}
