// Copyright (C) 2015-2024 The Neo Project.
//
// TestBinaryUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Util;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Helpers;
using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.Helpers
{
    public class TestBinaryUtility
        (ITestOutputHelper testOutputHelper)
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Theory]
        [InlineData(null, new byte[] { 0xaa, 0x00 })]
        [InlineData("a", new byte[] { 0xaa, 0x01, 0x61 })]
        [InlineData("Hello World", new byte[] { 0xaa, 0x0b, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 })]
        public void Test_WriteUtf8String(string? testValue, byte[] expected)
        {
            var className = nameof(BinaryUtility);
            var methodName = nameof(BinaryUtility.WriteUtf8String);
            var expectedHexString = Convert.ToHexString(expected);

            var actualByteCount = Encoding.UTF8.GetByteCount(testValue ?? string.Empty);
            var size = actualByteCount switch
            {
                <= byte.MaxValue => sizeof(byte) + 1,
                <= ushort.MaxValue and >= byte.MaxValue => sizeof(ushort) + 1,
                _ => sizeof(int) + 1,
            };
            var actual = new byte[actualByteCount + size];

            var actualBytesWritten = BinaryUtility.WriteUtf8String(testValue, 0, actual, 0, actualByteCount);

            _testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Class: {className}");
            _testOutputHelper.WriteLine($"   Method: {methodName}");

            _testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Value: {testValue ?? "null"}");
            _testOutputHelper.WriteLine($"   Result: {Convert.ToHexString(actual)}");
            _testOutputHelper.WriteLine($" Expected: {expectedHexString}");
            _testOutputHelper.WriteLine($"-----------------");

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new byte[] { 0xaa, 0x00 }, null)]
        [InlineData(new byte[] { 0xaa, 0x01, 0x61 }, "a")]
        [InlineData(new byte[] { 0xaa, 0x0b, 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 }, "Hello World")]
        public void Test_ReadUtf8String(byte[] encodedTestValue, string? expected)
        {
            var className = nameof(BinaryUtility);
            var methodName = nameof(BinaryUtility.ReadUtf8String);
            var encodedHexString = Convert.ToHexString(encodedTestValue);

            // Change "sizeof(byte)" to size of unmanaged type value if greater than
            // byte.MaxValue to ushort.MaxValue or int.MaxValue
            var exceptedByteCount = expected == null
                ? sizeof(byte) + 1
                : Encoding.UTF8.GetByteCount(expected) + sizeof(byte) + 1;

            var actual = BinaryUtility.ReadUtf8String(encodedTestValue, out var count);

            _testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Class: {className}");
            _testOutputHelper.WriteLine($"   Method: {methodName}");

            _testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Value: {encodedHexString}");
            _testOutputHelper.WriteLine($"   Result: {actual ?? "null"}");
            _testOutputHelper.WriteLine($" Expected: {expected ?? "null"}");
            _testOutputHelper.WriteLine($"-----------------");

            Assert.Equal(expected, actual);
            Assert.Equal(exceptedByteCount, count);
        }

        [Theory]
        [InlineData(0, new byte[] { 0xfc, 0x00 })]
        [InlineData((uint)0, new byte[] { 0xfc, 0x00 })]
        [InlineData((ushort)0, new byte[] { 0xfc, 0x00 })]
        [InlineData((ulong)0, new byte[] { 0xfc, 0x00 })]
        [InlineData((short)0, new byte[] { 0xfc, 0x00 })]
        [InlineData((long)0, new byte[] { 0xfc, 0x00 })]
        [InlineData(byte.MaxValue, new byte[] { 0xfc, 0xff })]
        [InlineData(ushort.MaxValue, new byte[] { 0xfd, 0xff, 0xff })]
        [InlineData(uint.MaxValue, new byte[] { 0xfe, 0xff, 0xff, 0xff, 0xff })]
        [InlineData(ulong.MaxValue, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff })]
        [InlineData(sbyte.MaxValue, new byte[] { 0xfc, 0x7f })]
        [InlineData(short.MaxValue, new byte[] { 0xfd, 0xff, 0x7f })]
        [InlineData(int.MaxValue, new byte[] { 0xfe, 0xff, 0xff, 0xff, 0x7f })]
        [InlineData(long.MaxValue, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f })]
        public void Test_WriteEncodedInteger<T>(T testValue, byte[] expected)
            where T : unmanaged
        {
            var className = nameof(BinaryUtility);
            var methodName = nameof(BinaryUtility.WriteEncodedInteger);
            var expectedHexString = Convert.ToHexString(expected);

            var actual = new byte[expected.Length];

            BinaryUtility.WriteEncodedInteger(testValue, actual);

            _testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Class: {className}");
            _testOutputHelper.WriteLine($"   Method: {methodName}");
            _testOutputHelper.WriteLine($"     Type: {typeof(T).Name}");

            _testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Value: {testValue}");
            _testOutputHelper.WriteLine($"   Result: {Convert.ToHexString(actual)}");
            _testOutputHelper.WriteLine($" Expected: {expectedHexString}");
            _testOutputHelper.WriteLine($"-----------------");

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new byte[] { 0xfc, 0xff }, 255)]
        [InlineData(new byte[] { 0xfc, 0xff }, (uint)255)]
        [InlineData(new byte[] { 0xfc, 0xff }, (ulong)255)]
        [InlineData(new byte[] { 0xfc, 0xff }, (ushort)255)]
        [InlineData(new byte[] { 0xfc, 0xff }, (short)255)]
        [InlineData(new byte[] { 0xfd, 0xff, 0xff }, ushort.MaxValue)]
        [InlineData(new byte[] { 0xfe, 0xff, 0xff, 0xff, 0xff }, uint.MaxValue)]
        [InlineData(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, ulong.MaxValue)]
        [InlineData(new byte[] { 0xfc, 0x7f }, sbyte.MaxValue)]
        [InlineData(new byte[] { 0xfd, 0xff, 0x7f }, short.MaxValue)]
        [InlineData(new byte[] { 0xfe, 0xff, 0xff, 0xff, 0x7f }, int.MaxValue)]
        [InlineData(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f }, long.MaxValue)]
        public void Test_ReadEncodedInteger<T>(byte[] encodedTestValue, T expected)
            where T : unmanaged
        {
            var className = nameof(BinaryUtility);
            var methodName = nameof(BinaryUtility.ReadEncodedInteger);
            var encodedHexString = Convert.ToHexString(encodedTestValue);

            var actual = BinaryUtility.ReadEncodedInteger(encodedTestValue, expected);

            _testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Class: {className}");
            _testOutputHelper.WriteLine($"   Method: {methodName}");
            _testOutputHelper.WriteLine($"     Type: {typeof(T).Name}");

            _testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Value: {encodedHexString}");
            _testOutputHelper.WriteLine($"   Result: {actual}");
            _testOutputHelper.WriteLine($" Expected: {expected}");
            _testOutputHelper.WriteLine($"-----------------");

            Assert.Equal(expected, actual);
        }
    }
}
