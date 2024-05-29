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

using Neo;
using Neo.Hosting;
using Neo.Hosting.App;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.NamedPipes.Protocol.Payloads;
using Neo.Hosting.App.Tests;
using Neo.Hosting.App.Tests.NamedPipes;
using Neo.Hosting.App.Tests.NamedPipes.Protocol;
using Neo.Hosting.App.Tests.NamedPipes.Protocol.Payloads;
using Neo.Hosting.App.Tests.UTHelpers;
using Neo.Hosting.App.Tests.UTHelpers.Extensions;
using System;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes.Protocol.Payloads
{
    public class UT_PipeExceptionPayload
        (ITestOutputHelper testOutputHelper)
    {
        private static readonly string s_exceptionMessage = "Hello";
        private static readonly string s_exceptionStackTrace = "World";

        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public void IPipeMessage_FromArray_Data()
        {
            var exception1 = new PipeExceptionPayload()
            {
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var expectedBytes = exception1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var exception2 = new PipeExceptionPayload();
            exception2.FromArray(expectedBytes);

            var actualBytes = exception2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = nameof(PipeExceptionPayload);
            var methodName = nameof(PipeExceptionPayload.FromArray);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(exception1.IsEmpty, exception2.IsEmpty);
            Assert.Equal(exception1.Message, exception2.Message);
            Assert.Equal(exception1.StackTrace, exception2.StackTrace);
        }

        [Fact]
        public void IPipeMessage_ToArray_Data()
        {
            var exception1 = new PipeExceptionPayload()
            {
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var expectedBytes = exception1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var exception2 = new PipeExceptionPayload()
            {
                Message = s_exceptionMessage,
                StackTrace = s_exceptionStackTrace
            };
            var actualBytes = exception2.ToArray();
            var actualBytesWithoutHeader = actualBytes;
            var actualHexString = Convert.ToHexString(actualBytesWithoutHeader);

            var className = nameof(PipeExceptionPayload);
            var methodName = nameof(PipeExceptionPayload.ToArray);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytesWithoutHeader);
        }
    }
}
