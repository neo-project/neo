// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NamedPipesSystemHostedService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.Host;
using Neo.Hosting.App.Host.Service;
using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.NamedPipes.Protocol.Payloads;
using Neo.Hosting.App.Tests.UTHelpers.Default;
using Neo.Hosting.App.Tests.UTHelpers.SetupClasses;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Neo.Hosting.App.Tests.Host.Service
{
    public class UT_NamedPipesSystemHostedService : TestSetupLogging, IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly NeoSystemHostedService _neoSystemService;

        public UT_NamedPipesSystemHostedService(
            ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _neoSystemService = new NeoSystemHostedService(TestBlockchain.TheNeoSystem, LoggerFactory, ProtocolSettings.Default, Options.Create(new NeoOptions()));
        }

        public void Dispose()
        {
            _neoSystemService.DisposeAsync().AsTask().Wait();
        }

        [Fact]
        public async Task Test_MessageProcess_GetVersion_From_Server()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName(), loggerFactory: LoggerFactory);
            using var pipeService = new NamedPipeSystemHostedService(_neoSystemService, connectionListener, loggerFactory: LoggerFactory);
            await pipeService.StartAsync(default).DefaultTimeout();

            var client = NamedPipeFactory.CreateClientStream(pipeService.LocalEndPoint);
            var message = PipeMessage.Create(Random.Shared.Next(), PipeCommand.GetVersion, PipeMessage.Null);

            await client.ConnectAsync().DefaultTimeout();
            await client.WriteAsync(message.ToArray()).DefaultTimeout();

            var buffer = new byte[4096];
            var count = await client.ReadAsync(buffer.AsMemory()).DefaultTimeout();

            await pipeService.StopAsync(default).DefaultTimeout();

            message.FromArray(buffer);

            var payload = message.Payload as PipeVersionPayload;

            Assert.True(count <= buffer.Length);
            Assert.Equal(PipeCommand.Version, message.Command);
        }

        [Fact]
        public async Task Test_MessageProcess_GetBlock_From_Server()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName(), loggerFactory: LoggerFactory);
            using var pipeService = new NamedPipeSystemHostedService(_neoSystemService, connectionListener, loggerFactory: LoggerFactory);
            await pipeService.StartAsync(default).DefaultTimeout();

            var client = NamedPipeFactory.CreateClientStream(pipeService.LocalEndPoint);
            var payloadRequest = new PipeUnmanagedPayload<uint> { Value = 0 };
            var message = PipeMessage.Create(Random.Shared.Next(), PipeCommand.GetBlock, payloadRequest);

            await client.ConnectAsync().DefaultTimeout();
            await client.WriteAsync(message.ToArray()).DefaultTimeout();

            var buffer = new byte[4096];
            var count = await client.ReadAsync(buffer.AsMemory()).DefaultTimeout();

            await pipeService.StopAsync(default).DefaultTimeout();

            message.FromArray(buffer);

            var payloadResponse = message.Payload as PipeSerializablePayload<Block>;

            Assert.True(count <= buffer.Length);
            Assert.Equal(PipeCommand.Block, message.Command);
        }

        [Fact]
        public async Task Test_MessageProcess_GetTransaction_From_Server()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName(), loggerFactory: LoggerFactory);
            using var pipeService = new NamedPipeSystemHostedService(_neoSystemService, connectionListener, loggerFactory: LoggerFactory);
            await pipeService.StartAsync(default).DefaultTimeout();

            var client = NamedPipeFactory.CreateClientStream(pipeService.LocalEndPoint);
            var payloadRequest = new PipeSerializablePayload<UInt256> { Value = UInt256.Zero };
            var message = PipeMessage.Create(Random.Shared.Next(), PipeCommand.GetTransaction, payloadRequest);

            await client.ConnectAsync().DefaultTimeout();
            await client.WriteAsync(message.ToArray()).DefaultTimeout();

            var buffer = new byte[4096];
            var count = await client.ReadAsync(buffer.AsMemory()).DefaultTimeout();

            await pipeService.StopAsync(default).DefaultTimeout();

            message.FromArray(buffer);

            var payloadResponse = message.Payload as PipeSerializablePayload<Transaction>;

            Assert.True(count <= buffer.Length);
            Assert.Equal(PipeCommand.Transaction, message.Command);
            Assert.Null(payloadResponse.Value); // no transaction
        }
    }
}
