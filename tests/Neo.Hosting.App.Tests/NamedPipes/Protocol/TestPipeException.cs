// Copyright (C) 2015-2024 The Neo Project.
//
// TestPipeException.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.Tests.UTHelpers;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes.Protocol
{
    public class TestPipeException
        (ITestOutputHelper testOutputHelper)
    {
        private static readonly string s_exceptionMessage = "Test1";
        private static readonly string s_exceptionStackTrace = "Test2";

        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public async Task IPipeMessage_CopyFromAsync_WithData()
        {
            var exception1 = new PipeException()
            {
                IsEmpty = false,
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var expectedBytes = exception1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms1 = new MemoryStream();
            await exception1.CopyToAsync(ms1).DefaultTimeout();

            var exception2 = new PipeException() { IsEmpty = true };
            ms1.Position = 0;
            await exception2.CopyFromAsync(ms1).DefaultTimeout();

            var actualBytes = exception2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = nameof(PipeException);
            var methodName = nameof(PipeException.CopyFromAsync);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(exception1.IsEmpty, exception2.IsEmpty);
            Assert.Equal(exception1.Message, exception2.Message);
            Assert.Equal(exception1.StackTrace, exception2.StackTrace);
        }

        [Fact]
        public async Task IPipeMessage_CopyFromAsync_WithNoData()
        {
            var exception1 = new PipeException()
            {
                IsEmpty = true,
            };
            var expectedBytes = exception1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms1 = new MemoryStream();
            await exception1.CopyToAsync(ms1).DefaultTimeout();

            var exception2 = new PipeException() { IsEmpty = true };
            ms1.Position = 0;
            await exception2.CopyFromAsync(ms1).DefaultTimeout();

            var actualBytes = exception2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = nameof(PipeException);
            var methodName = nameof(PipeException.CopyFromAsync);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(exception1.IsEmpty, exception2.IsEmpty);
            Assert.Equal(exception1.Message, exception2.Message);
            Assert.Equal(exception1.StackTrace, exception2.StackTrace);
        }

        [Fact]
        public async Task IPipeMessage_CopyToAsync_WithData()
        {
            var exception = new PipeException()
            {
                IsEmpty = false,
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var expectedBytes = exception.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms = new MemoryStream();
            await exception.CopyToAsync(ms).DefaultTimeout();

            var actualBytes = ms.ToArray();
            var actualBytesWithoutHeader = actualBytes[(sizeof(ulong) + sizeof(uint))..];
            var actualHexString = Convert.ToHexString(actualBytesWithoutHeader);

            var className = nameof(PipeException);
            var methodName = nameof(PipeException.CopyToAsync);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytesWithoutHeader);
        }

        [Fact]
        public async Task IPipeMessage_CopyToAsync_WithNoData()
        {
            var exception = new PipeException()
            {
                IsEmpty = true,
            };
            var expectedBytes = exception.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms = new MemoryStream();
            await exception.CopyToAsync(ms).DefaultTimeout();

            var actualBytes = ms.ToArray();
            var actualBytesWithoutHeader = actualBytes[(sizeof(ulong) + sizeof(uint))..];
            var actualHexString = Convert.ToHexString(actualBytesWithoutHeader);

            var className = nameof(PipeException);
            var methodName = nameof(PipeException.CopyToAsync);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytesWithoutHeader);
        }

        [Fact]
        public void IPipeMessage_CopyToAsync_SetMessageValue()
        {
            var exception = new PipeException()
            {
                IsEmpty = true,
            };
            exception.Message = s_exceptionMessage;

            var expectedBytes = exception.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var actualBytes = exception.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = nameof(PipeException);
            var methodName = nameof(PipeException.Message);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
        }
    }
}
