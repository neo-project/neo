// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NamedPipeClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.Host.Service;
using Neo.Hosting.App.NamedPipes;
using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.Tests.UTHelpers.SetupClasses;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes
{
    public class UT_NamedPipeClient : TestSetupLogging
    {
        public UT_NamedPipeClient(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task Test()
        {
            await using var connectionListener = NamedPipeServerFactory.CreateListener(new("NeoTestPipe"), loggerFactory: LoggerFactory);
            using var server = new NamedPipeSystemHostedService(connectionListener, loggerFactory: LoggerFactory);
            await using var client = new NamedPipeClient(connectionListener.LocalEndPoint, LoggerFactory, null);

            // Server startup
            await server.StartAsync(default).DefaultTimeout();

            // Client connecting
            var clientConnection = await client.ConnectAsync().DefaultTimeout();

            // Client sending data
            var versionMessage = PipeMessage.Create(1, PipeCommand.GetVersion, PipeMessage.Null);
            await clientConnection.WriteAsync(versionMessage).DefaultTimeout();

            // client accepting message
            var message = await clientConnection.ReadAsync().DefaultTimeout();
            Assert.NotNull(message);

            // Server and Client shutdown
            await server.StopAsync(default).DefaultTimeout();
            await clientConnection.DisposeAsync();

            Assert.NotEqual(PipeMessage.Null, message.Payload);
            Assert.Equal(PipeCommand.Version, message.Command);
        }
    }
}
