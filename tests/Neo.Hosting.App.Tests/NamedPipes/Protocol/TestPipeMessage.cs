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

using Neo.Hosting.App.NamedPipes.Protocol;
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
        public void IPipeMessage_FromArray_PipeVersion()
        {
            var message1 = PipeMessage.Create(PipeCommand.Version, new PipeVersion());
            var expectedBytes = message1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var message2 = new PipeMessage();
            message2.FromArray(expectedBytes);

            var actualBytes = message2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = $"{nameof(PipeMessage)}<{nameof(PipeVersion)}>";
            var methodName = nameof(PipeMessage.FromArray);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            var versionResult1 = message1.Payload as PipeVersion;
            var versionResult2 = message2.Payload as PipeVersion;

            Assert.Equal(expectedBytes, actualBytes);
            Assert.NotNull(versionResult1);
            Assert.NotNull(versionResult2);
            Assert.Equal(versionResult1.VersionNumber, versionResult2.VersionNumber);
            Assert.Equal(versionResult1.Platform, versionResult2.Platform);
            Assert.Equal(versionResult1.TimeStamp, versionResult2.TimeStamp);
            Assert.Equal(versionResult1.MachineName, versionResult2.MachineName);
            Assert.Equal(versionResult1.UserName, versionResult2.UserName);
            Assert.Equal(versionResult1.ProcessId, versionResult2.ProcessId);
            Assert.Equal(versionResult1.ProcessPath, versionResult2.ProcessPath);
        }

        [Fact]
        public void IPipeMessage_ToArray_PipeVersion()
        {
            var date = DateTime.UtcNow;
            var message1 = PipeMessage.Create(PipeCommand.Version, new PipeVersion() { TimeStamp = date });
            var expectedBytes = message1.ToArray();
            var expectedHexString = Convert.ToHexString(expectedBytes);

            var message2 = PipeMessage.Create(PipeCommand.Version, new PipeVersion() { TimeStamp = date });
            var actualBytes = message2.ToArray();
            var actualHexString = Convert.ToHexString(actualBytes);

            var className = $"{nameof(PipeMessage)}<{nameof(PipeVersion)}>";
            var methodName = nameof(PipeMessage.ToArray);
            _testOutputHelper.LogDebug(className, methodName, actualHexString, expectedHexString);

            var versionResult1 = message1.Payload as PipeVersion;
            var versionResult2 = message2.Payload as PipeVersion;

            Assert.Equal(expectedBytes, actualBytes);
            Assert.NotNull(versionResult1);
            Assert.NotNull(versionResult2);
            Assert.Equal(versionResult1.VersionNumber, versionResult2.VersionNumber);
            Assert.Equal(versionResult1.Platform, versionResult2.Platform);
            Assert.Equal(versionResult1.TimeStamp, versionResult2.TimeStamp);
            Assert.Equal(versionResult1.MachineName, versionResult2.MachineName);
            Assert.Equal(versionResult1.UserName, versionResult2.UserName);
            Assert.Equal(versionResult1.ProcessId, versionResult2.ProcessId);
            Assert.Equal(versionResult1.ProcessPath, versionResult2.ProcessPath);
        }
    }
}