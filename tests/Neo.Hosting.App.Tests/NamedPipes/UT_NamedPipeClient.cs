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

using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.Host.Service;
using Neo.Hosting.App.NamedPipes;
using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.NamedPipes.Protocol.Payloads;
using Neo.Hosting.App.Tests.UTHelpers.Default;
using Neo.Hosting.App.Tests.UTHelpers.SetupClasses;
using Neo.Network.P2P.Payloads;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Hosting.App.Tests.NamedPipes
{
    public class UT_NamedPipeClient : TestSetupLogging, IDisposable
    {
        private readonly NeoSystemHostedService _neoSystemService;

        public UT_NamedPipeClient(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _neoSystemService = new NeoSystemHostedService(LoggerFactory, TestProtocolSettings.Default, Options.Create(TestNeoOptions.Default));
            _neoSystemService.StartAsync(new CancellationToken()).Wait();
        }

        public void Dispose()
        {
            _neoSystemService.DisposeAsync().AsTask().Wait();
        }

        [Fact]
        public async Task Test_Send_And_Receive_Version_From_Server_And_Client()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName(), loggerFactory: LoggerFactory);
            using var server = new NamedPipeSystemHostedService(_neoSystemService, connectionListener, loggerFactory: LoggerFactory);
            await using var client = new NamedPipeClient(connectionListener.LocalEndPoint, LoggerFactory, null);

            // Server startup
            await server.StartAsync(default).DefaultTimeout();

            // Client connecting
            var clientConnection = await client.ConnectAsync().DefaultTimeout();

            // Client sending data
            var requestId = Random.Shared.Next();
            var versionMessage = PipeMessage.Create(requestId, PipeCommand.GetVersion, PipeMessage.Null);
            await clientConnection.WriteAsync(versionMessage).DefaultTimeout();

            // client accepting message
            var message = await clientConnection.ReadAsync().DefaultTimeout();
            Assert.NotNull(message);
            Assert.IsType<PipeVersionPayload>(message.Payload);

            // Server and Client shutdown
            await server.StopAsync(default).DefaultTimeout();
            await clientConnection.DisposeAsync();

            Assert.Equal(requestId, message.RequestId);
            Assert.NotEqual(PipeMessage.Null, message.Payload);
            Assert.Equal(PipeCommand.Version, message.Command);
        }

        [Fact]
        public async Task Test_Send_And_Receive_Block_From_Server_And_Client()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName(), loggerFactory: LoggerFactory);
            using var server = new NamedPipeSystemHostedService(_neoSystemService, connectionListener, loggerFactory: LoggerFactory);
            await using var client = new NamedPipeClient(connectionListener.LocalEndPoint, LoggerFactory, null);

            // Server startup
            await server.StartAsync(default).DefaultTimeout();

            // Client connecting
            var clientConnection = await client.ConnectAsync().DefaultTimeout();

            // Client sending data
            var requestId = Random.Shared.Next();
            var payload = new PipeUnmanagedPayload<uint>() { Value = 0 };
            var versionMessage = PipeMessage.Create(requestId, PipeCommand.GetBlock, payload);
            await clientConnection.WriteAsync(versionMessage).DefaultTimeout();

            // client accepting message
            var message = await clientConnection.ReadAsync().DefaultTimeout();
            Assert.NotNull(message);
            Assert.IsType<PipeSerializablePayload<Block>>(message.Payload);

            // Server and Client shutdown
            await server.StopAsync(default).DefaultTimeout();
            await clientConnection.DisposeAsync();

            Assert.Equal(requestId, message.RequestId);
            Assert.NotEqual(PipeMessage.Null, message.Payload);
            Assert.Equal(PipeCommand.Block, message.Command);
        }
    }
}
