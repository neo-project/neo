// Copyright (C) 2015-2024 The Neo Project.
//
// TestByteArrayBuffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Buffers;
using Neo.Hosting.App.Tests.UTHelpers;
using Neo.Hosting.App.Tests.UTHelpers.DataCases;
using Neo.Hosting.App.Tests.UTHelpers.Extensions;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.Buffers
{
    public class TestByteArrayBuffer
        (ITestOutputHelper testOutputHelper)
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.ByteArrayBuffer_ReadWrite_Cases), MemberType = typeof(UT_MemberDataCases))]
        public void ByteArrayBuffer_Write<T>(T value, byte[] expected)
            where T : unmanaged
        {
            var buffer = new ByteArrayBuffer();
            buffer.Write(value);

            byte[] actual = [.. buffer];

            var className = $"{nameof(ByteArrayBuffer)}";
            var methodName = nameof(ByteArrayBuffer.Write);
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.ByteArrayBuffer_ReadWrite_Cases), MemberType = typeof(UT_MemberDataCases))]
        public void ByteArrayBuffer_Read<T>(T expected, byte[] value)
            where T : unmanaged
        {
            var buffer = new ByteArrayBuffer(value);
            var actual = buffer.Read<T>();

            var className = $"{nameof(ByteArrayBuffer)}";
            var methodName = nameof(ByteArrayBuffer.Read);
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.ByteArrayBuffer_ReadWrite_String_Cases), MemberType = typeof(UT_MemberDataCases))]
        public void ByteArrayBuffer_Write_String(string value, byte[] expected)
        {
            var buffer = new ByteArrayBuffer();
            buffer.Write(value);

            byte[] actual = [.. buffer];

            var className = $"{nameof(ByteArrayBuffer)}";
            var methodName = nameof(ByteArrayBuffer.Write);
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.ByteArrayBuffer_ReadWrite_String_Cases), MemberType = typeof(UT_MemberDataCases))]
        public void ByteArrayBuffer_Read_String(string expected, byte[] value)
        {
            var buffer = new ByteArrayBuffer(value);
            var actual = buffer.ReadString();

            var className = $"{nameof(ByteArrayBuffer)}";
            var methodName = nameof(ByteArrayBuffer.Read);
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
        }
    }
}
