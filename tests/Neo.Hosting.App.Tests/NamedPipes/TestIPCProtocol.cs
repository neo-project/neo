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
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System.Text;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes
{
    public class TestIPCProtocol
        (ITestOutputHelper testOutputHelper)
    {
        private static readonly byte[] s_testData = Encoding.UTF8.GetBytes("Hello world");
        private static readonly PipeMessage<PipeVersion> s_testPipeMessageData = new();

        private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

        [Fact]
        public async Task IPipeMessage_CopyToAsync_CopyFromAsync_OnNamedPipeStream()
        {
            await using var connectionListener = await NamedPipeTransportFactory.CreateConnectionListener();
            var clientConnection = NamedPipeTransportFactory.CreateClientStream(connectionListener.EndPoint);

            // Client connecting
            await clientConnection.ConnectAsync().DefaultTimeout();

            // Server accepting stream
            var serverConnection = await connectionListener.AcceptAsync().DefaultTimeout();

            // Client sending data
            await clientConnection.WriteAsync(s_testPipeMessageData.ToArray());

            // Server reading data
            var result = new PipeMessage<PipeVersion>();

            var readResult = await serverConnection!.Transport.Input.ReadAsync().DefaultTimeout();
            var buffer = readResult.Buffer;

            if (buffer.IsSingleSegment)
                result.CopyFrom(buffer.FirstSpan.ToArray());
            else
            {
                byte[] tmpBuffer = [];

                foreach (var segment in buffer)
                    tmpBuffer = [.. tmpBuffer, .. segment.ToArray()];

                result.CopyFrom(tmpBuffer);
            }

            serverConnection.Transport.Input.AdvanceTo(buffer.End);

            clientConnection.Close();

            readResult = await serverConnection.Transport.Input.ReadAsync();
            Assert.True(readResult.IsCompleted);

            // Server completing input and output
            await serverConnection.Transport.Input.CompleteAsync();
            await serverConnection.Transport.Output.CompleteAsync();

            // Server disposing connection
            await serverConnection.DisposeAsync();

            Assert.Equal(s_testPipeMessageData.Payload.VersionNumber, result.Payload.VersionNumber);
            Assert.Equal(s_testPipeMessageData.Payload.Platform, result.Payload.Platform);
            Assert.Equal(s_testPipeMessageData.Payload.TimeStamp, result.Payload.TimeStamp);
            Assert.Equal(s_testPipeMessageData.Payload.MachineName, result.Payload.MachineName);
            Assert.Equal(s_testPipeMessageData.Payload.UserName, result.Payload.UserName);
            Assert.Equal(s_testPipeMessageData.Payload.ProcessId, result.Payload.ProcessId);
            Assert.Equal(s_testPipeMessageData.Payload.ProcessPath, result.Payload.ProcessPath);
            Assert.Equal(s_testPipeMessageData.Exception.IsEmpty, result.Exception.IsEmpty);
            Assert.Equal(s_testPipeMessageData.Exception.Message, result.Exception.Message);
            Assert.Equal(s_testPipeMessageData.Exception.StackTrace, result.Exception.StackTrace);
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
