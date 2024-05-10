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

using Microsoft.Extensions.Logging;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.Tests.UTHelpers;
using Neo.Hosting.App.Tests.UTHelpers.SetupClasses;
using System.Text;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes
{
    public class TestNamedPipeConnectionListener : UT_SetupTestLogging
    {
        private static readonly byte[] s_testData = Encoding.UTF8.GetBytes("Hello world");

        public TestNamedPipeConnectionListener(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
        {
            await using var connectionListener = await NamedPipeTransportFactory.CreateConnectionListener(loggerFactory: LoggerFactory);
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
