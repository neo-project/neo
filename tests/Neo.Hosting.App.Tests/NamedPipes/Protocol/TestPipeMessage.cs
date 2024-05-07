// Copyright (C) 2015-2024 The Neo Project.
//
// TestPipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.Tests.UTHelpers;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes.Protocol
{
    public class TestPipeMessage
        (ITestOutputHelper testOutputHelper)
    {
        private static readonly string s_exceptionMessage = "Test";
        private static readonly Exception s_testException = new(s_exceptionMessage);

        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;


        [Fact]
        public void IPipeMessage_CopyFromAsync_NoException()
        {
            var message1 = new PipeMessage<PipeVersion>();
            var expectedBytes = message1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms1 = new MemoryStream();
            message1.CopyTo(ms1);

            var message2 = new PipeMessage<PipeVersion>();
            ms1.Position = 0;
            message2.CopyFrom(ms1);

            var actualBytes = message2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = $"{nameof(PipeMessage<PipeVersion>)}<{nameof(PipeVersion)}>";
            var methodName = nameof(PipeMessage<PipeVersion>.CopyFrom);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(message1.Payload.VersionNumber, message2.Payload.VersionNumber);
            Assert.Equal(message1.Payload.Platform, message2.Payload.Platform);
            Assert.Equal(message1.Payload.TimeStamp, message2.Payload.TimeStamp);
            Assert.Equal(message1.Payload.MachineName, message2.Payload.MachineName);
            Assert.Equal(message1.Payload.UserName, message2.Payload.UserName);
            Assert.Equal(message1.Payload.ProcessId, message2.Payload.ProcessId);
            Assert.Equal(message1.Payload.ProcessPath, message2.Payload.ProcessPath);
            Assert.True(message2.Exception.IsEmpty);
            Assert.Empty(message2.Exception.Message);
            Assert.Empty(message2.Exception.StackTrace);
        }

        [Fact]
        public void IPipeMessage_CopyFromAsync_WithException()
        {
            var version = new PipeVersion();
            var message1 = PipeMessage<PipeVersion>.Create(version, s_testException);
            var expectedBytes = message1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms1 = new MemoryStream();
            message1.CopyTo(ms1);

            var message2 = new PipeMessage<PipeVersion>();
            ms1.Position = 0;
            message2.CopyFrom(ms1);

            var actualBytes = message2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = $"{nameof(PipeMessage<PipeVersion>)}<{nameof(PipeVersion)}>";
            var methodName = nameof(PipeMessage<PipeVersion>.CopyFrom);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(message1.Payload.VersionNumber, message2.Payload.VersionNumber);
            Assert.Equal(message1.Payload.Platform, message2.Payload.Platform);
            Assert.Equal(message1.Payload.TimeStamp, message2.Payload.TimeStamp);
            Assert.Equal(message1.Payload.MachineName, message2.Payload.MachineName);
            Assert.Equal(message1.Payload.UserName, message2.Payload.UserName);
            Assert.Equal(message1.Payload.ProcessId, message2.Payload.ProcessId);
            Assert.Equal(message1.Payload.ProcessPath, message2.Payload.ProcessPath);
            Assert.False(message2.Exception.IsEmpty);
            Assert.Equal(s_exceptionMessage, message2.Exception.Message);
            Assert.Empty(message2.Exception.StackTrace);
        }

        [Fact]
        public void IPipeMessage_CopyToAsync_WithException()
        {
            var version = new PipeVersion();
            var message = PipeMessage<PipeVersion>.Create(version, s_testException);
            var expectedBytes = message.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms = new MemoryStream();
            message.CopyTo(ms);

            var actualBytes = ms.ToArray()[PipeMessage<PipeVersion>.HeaderSize..];
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = $"{nameof(PipeMessage<PipeVersion>)}<{nameof(PipeVersion)}>";
            var methodName = nameof(PipeMessage<PipeVersion>.CopyTo);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public void IPipeMessage_CopyToAsync_NoException()
        {
            var message1 = new PipeMessage<PipeVersion>();
            var expectedBytes = message1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            using var ms = new MemoryStream();
            message1.CopyTo(ms);

            var actualBytes = ms.ToArray()[PipeMessage<PipeVersion>.HeaderSize..];
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = $"{nameof(PipeMessage<PipeVersion>)}<{nameof(PipeVersion)}>";
            var methodName = nameof(PipeMessage<PipeVersion>.CopyTo);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            Assert.Equal(expectedBytes, actualBytes);
        }
    }
}
