// Copyright (C) 2015-2024 The Neo Project.
//
// TestSpanExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Tests.UTHelpers;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.Extensions
{
    public class TestSpanExtensions
        (ITestOutputHelper testOutputHelper)
    {
        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWriteArray_WithExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public unsafe void ReadArray_WithExtraData<T>(T[] expected, int start, byte[] value)
            where T : unmanaged
        {
            var expectedSpan = expected.AsSpan();
            var expectedReadLength = sizeof(T) * expected.Length + sizeof(int);

            var actualSpan = value.AsSpan();
            var actualReadLength = actualSpan.ReadArray(out T[] actual, start);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.ReadArray)}<{typeof(T)}>";
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedReadLength, actualReadLength);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWriteArray_WithExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public unsafe void WriteArray_WithExtraData<T>(T[] value, int start, byte[] expected)
            where T : unmanaged
        {
            var expectedHexString = Convert.ToHexString(expected);
            var expectedWriteLength = sizeof(T) * value.Length + sizeof(int);

            var actual = new byte[expectedWriteLength + (start * 2)];
            var actualSpan = actual.AsSpan();
            var actualWriteLength = actualSpan.Write(value, start);
            var actualHexString = Convert.ToHexString(actual);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.Write)}<{typeof(T)}[]>";
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedWriteLength, actualWriteLength);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWriteArray_WithNoExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public void ReadArray_WithNoExtraData<T>(T[] expected, byte[] value)
            where T : unmanaged
        {
            var expectedSpan = value.AsSpan();
            var expectedReadLength = expectedSpan.Length;

            var actualReadLength = expectedSpan.ReadArray(out T[] actual);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.ReadArray)}<{typeof(T)}>";
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedReadLength, actualReadLength);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWriteArray_WithNoExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public unsafe void WriteArray_WithNoExtraData<T>(T[] value, byte[] expected)
            where T : unmanaged
        {
            var expectedHexString = Convert.ToHexString(expected);
            var expectedWriteLength = sizeof(T) * value.Length + sizeof(int);

            var actual = new byte[expectedWriteLength];
            var actualSpan = actual.AsSpan();
            var actualWriteLength = actualSpan.Write(value);
            var actualHexString = Convert.ToHexString(actual);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.Write)}<{typeof(T)}[]>";
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedWriteLength, actualWriteLength);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWrite_WithExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public unsafe void Write_TypeOfT_WithExtraData<T>(T value, int start, byte[] expected)
            where T : unmanaged
        {
            var expectedHexString = Convert.ToHexString(expected);
            var expectedWriteLength = sizeof(T);

            var actual = new byte[expectedWriteLength + (start * 2)];
            var actualSpan = actual.AsSpan();
            var actualWriteLength = actualSpan.Write(value, start);
            var actualHexString = Convert.ToHexString(actual);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.Write)}<{typeof(T)}>";
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedWriteLength, actualWriteLength);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWrite_WithExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public unsafe void Read_TypeOfT_WithExtraData<T>(T expected, int start, byte[] value)
            where T : unmanaged
        {
            var expectedSpan = value.AsSpan();
            var expectedReadLength = sizeof(T);

            var actual = new T();
            var actualReadLength = expectedSpan.Read(ref actual, start);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.Read)}<{typeof(T)}>";
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedReadLength, actualReadLength);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWrite_WithNoExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public unsafe void Write_TypeOfT_WithNoExtraData<T>(T value, byte[] expected)
            where T : unmanaged
        {
            var expectedHexString = Convert.ToHexString(expected);
            var expectedWriteLength = sizeof(T);

            var actual = new byte[expectedWriteLength];
            var actualSpan = actual.AsSpan();
            var actualWriteLength = actualSpan.Write(value);
            var actualHexString = Convert.ToHexString(actual);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.Write)}<{typeof(T)}>";
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedWriteLength, actualWriteLength);
        }

        [Theory]
        [MemberData(nameof(UT_MemberDataCases.SpanExtensions_ReadWrite_WithNoExtraData_Cases), MemberType = typeof(UT_MemberDataCases))]
        public void Read_TypeOfT_WithNoExtraData<T>(T expected, byte[] value)
            where T : unmanaged
        {
            var expectedSpan = value.AsSpan();
            var expectedReadLength = expectedSpan.Length;

            var actual = new T();
            var actualReadLength = expectedSpan.Read(ref actual);

            var className = nameof(SpanExtensions);
            var methodName = $"{nameof(SpanExtensions.Read)}<{typeof(T)}>";
            _testOutputHelper.LogDebug(className, methodName, actual, expected);

            Assert.Equal(expected, actual);
            Assert.Equal(expectedReadLength, actualReadLength);
        }
    }
}
