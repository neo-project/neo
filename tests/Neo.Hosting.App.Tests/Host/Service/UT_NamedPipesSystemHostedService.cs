// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NeoSystemHostedService.cs file belongs to the neo project and is free
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
using Neo.Hosting.App.Host.Service;
using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using Neo.Hosting.App.Tests.UTHelpers.SetupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Neo.Hosting.App.Tests.Host.Service
{
    public class UT_NamedPipesSystemHostedService : TestSetupLogging
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UT_NamedPipesSystemHostedService(
            ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Test_MessageProcess_GetVersion_From_Server()
        {
            await using var connectionListener = NamedPipeFactory.CreateListener(NamedPipeFactory.GetUniquePipeName(), loggerFactory: LoggerFactory);
            await using var neoSystemService = new NeoSystemHostedService(LoggerFactory, ProtocolSettings.Default, Options.Create(new NeoOptions()));
            using var pipeService = new NamedPipeSystemHostedService(neoSystemService, connectionListener, loggerFactory: LoggerFactory);
            await pipeService.StartAsync(default).DefaultTimeout();

            var client = NamedPipeFactory.CreateClientStream(pipeService.LocalEndPoint);
            var message = PipeMessage.Create(1, PipeCommand.GetVersion, PipeMessage.Null);

            await client.ConnectAsync().DefaultTimeout();
            await client.WriteAsync(message.ToArray()).DefaultTimeout();

            var buffer = new byte[4096];
            var count = await client.ReadAsync(buffer.AsMemory()).DefaultTimeout();

            await pipeService.StopAsync(default).DefaultTimeout();

            message.FromArray(buffer);

            var payload = message.Payload as PipeVersion;

            Assert.True(count <= buffer.Length);
            Assert.Equal(PipeCommand.Version, message.Command);
        }
    }
}
