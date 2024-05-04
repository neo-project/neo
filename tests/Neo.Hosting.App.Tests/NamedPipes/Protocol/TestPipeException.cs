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

using Akka.Util;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.NamedPipes.Protocol;
using System.Diagnostics;
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
        public async Task IPipeMessage_CopyFromAsync()
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

            var className = nameof(PipeVersion);
            var methodName = nameof(PipeVersion.CopyFromAsync);
            _testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Class: {className}");
            _testOutputHelper.WriteLine($"   Method: {methodName}");

            _testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"   Actual: {actualHexString}");
            _testOutputHelper.WriteLine($" Expected: {expectedHexString}");
            _testOutputHelper.WriteLine($"-----------------");

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(exception1.IsEmpty, exception2.IsEmpty);
            Assert.Equal(exception1.Message, exception2.Message);
            Assert.Equal(exception1.StackTrace, exception2.StackTrace);
        }

        [Fact]
        public async Task IPipeMessage_CopyToAsync()
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
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = nameof(PipeVersion);
            var methodName = nameof(PipeVersion.CopyToAsync);
            _testOutputHelper.WriteLine(nameof(Debug).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"    Class: {className}");
            _testOutputHelper.WriteLine($"   Method: {methodName}");

            _testOutputHelper.WriteLine(nameof(Result).PadCenter(17, '-'));
            _testOutputHelper.WriteLine($"   Actual: {actualHexString}");
            _testOutputHelper.WriteLine($" Expected: {expectedHexString}");
            _testOutputHelper.WriteLine($"-----------------");

            Assert.Equal(expectedBytes, actualBytes);
        }
    }
}
