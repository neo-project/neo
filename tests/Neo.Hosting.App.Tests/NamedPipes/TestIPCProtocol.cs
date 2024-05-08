// Copyright (C) 2015-2024 The Neo Project.
//
// TestIPCProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System.Text;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes
{
    public class TestIPCProtocol
        (ITestOutputHelper testOutputHelper)
    {
        private static readonly byte[] s_testData = Encoding.UTF8.GetBytes("Hello world");
        private static readonly PipeMessage s_testPipeMessageData = PipeMessage.Create(PipeCommand.Version, new PipeVersion());

        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public async Task IPipeMessage_CopyTo_Stream_CopyFrom_ByteArray_NamedPipeStream()
        {
            await using var connectionListener = await NamedPipeTransportFactory.CreateConnectionListener();
            var clientConnection = NamedPipeTransportFactory.CreateClientStream(connectionListener.EndPoint);

            // Client connecting
            await clientConnection.ConnectAsync().DefaultTimeout();

            // Server accepting stream
            var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();

            // Client sending data
            clientConnection.Write(s_testPipeMessageData.ToArray().AsSpan());

            // Server reading data
            var result = await serverConnection!.Transport.Input.ReadPipeMessage().DefaultTimeout();

            clientConnection.Close();

            var readResult = await serverConnection.Transport.Input.ReadAsync();
            Assert.True(readResult.IsCompleted);

            // Server completing input and output
            await serverConnection.Transport.Input.CompleteAsync();
            await serverConnection.Transport.Output.CompleteAsync();

            // Server disposing connection
            await serverConnection.DisposeAsync();

            var versionResult1 = s_testPipeMessageData.Payload as PipeVersion;
            var versionResult2 = result.Payload as PipeVersion;

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
        public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
        {
            await using var connectionListener = await NamedPipeTransportFactory.CreateConnectionListener();
            var clientConnection = NamedPipeTransportFactory.CreateClientStream(connectionListener.EndPoint);

            // Client connecting
            await clientConnection.ConnectAsync().DefaultTimeout();

            // Server accepting stream
            var serverConnectionTask = connectionListener.AcceptAsync();

            // Client sending data
            var writeTask = clientConnection.WriteAsync(s_testData);

            var serverConnection = await serverConnectionTask.DefaultTimeout();
            await writeTask.DefaultTimeout();

            // Server reading data
            var readResult = await serverConnection!.Transport.Input.ReadAtLeastAsync(s_testData.Length).DefaultTimeout();
            serverConnection.Transport.Input.AdvanceTo(readResult.Buffer.End);

            clientConnection.Close();

            readResult = await serverConnection.Transport.Input.ReadAsync();
            Assert.True(readResult.IsCompleted);

            // Server completing input and output
            await serverConnection.Transport.Input.CompleteAsync();
            await serverConnection.Transport.Output.CompleteAsync();

            // Server disposing connection
            await serverConnection.DisposeAsync();
        }
    }
}
