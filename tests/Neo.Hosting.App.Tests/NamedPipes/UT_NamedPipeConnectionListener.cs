// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NamedPipeConnectionListener.cs file belongs to the neo project and is free
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
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes
{
    public class UT_NamedPipeConnectionListener : TestSetupLogging
    {
        private static readonly IPipeMessage s_testPipeMessage = PipeMessage.Create(1, PipeCommand.GetVersion, PipeMessage.Null);

        public UT_NamedPipeConnectionListener(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task BidirectionalStream_ServerReadsDataAndCompletes_GracefullyClosed()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName(), loggerFactory: LoggerFactory);
            var clientConnection = NamedPipeFactory.CreateClientStream(connectionListener.LocalEndPoint);

            // Server startup
            connectionListener.Start();

            // Client connecting
            await clientConnection.ConnectAsync().DefaultTimeout();

            // Server accepting stream
            var serverConnectionTask = connectionListener.AcceptAsync();

            // Client sending data
            var bytes = s_testPipeMessage.ToArray();
            var writeTask = clientConnection.WriteAsync(bytes);

            var serverConnection = await serverConnectionTask.DefaultTimeout();
            await writeTask.DefaultTimeout();

            // Server reading data
            var readResult = await serverConnection.ReadAsync().DefaultTimeout();
            Assert.NotNull(readResult);

            clientConnection.Close();

            var countResult = serverConnection.MessageQueueCount;
            Assert.Equal(0, countResult);

            // Server disposing connection
            await serverConnection.DisposeAsync();
        }
    }
}
