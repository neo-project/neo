// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Service.Pipes;
using Neo.Service.Pipes.Payloads;
using Neo.Service.Tests.Helpers;
using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Service.Tests
{
    public class UT_PipeServer : IAsyncLifetime, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly PipeServer _pipeServer;
        private Task? _pipeServerTask;

        public UT_PipeServer(
            ITestOutputHelper outputHelper)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddProvider(new UT_XUnitLoggerProvider(outputHelper));
            });
            _pipeServer = new PipeServer(
                NodeUtilities.GetApplicationVersionNumber(),
                ProtocolSettings.Default.Network,
                _loggerFactory.CreateLogger<PipeServer>()
                );
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task InitializeAsync()
        {
            _pipeServerTask = Task.Factory.StartNew(_pipeServer.StartAndListen);
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _pipeServer?.Dispose();
            return _pipeServerTask!;
        }

        [Fact]
        public void Test_SerializationOfProtocolVersionMessage()
        {
            Assert.False(_pipeServerTask!.IsCompleted);
            Assert.False(_pipeServerTask!.IsFaulted);

            using var clientStream = new NamedPipeClientStream(".", NamedPipeService.PipeName);
            clientStream.Connect();

            Assert.True(clientStream.IsConnected);
            Assert.True(clientStream.CanWrite);
            Assert.True(clientStream.CanRead);

            var resultMessage = PipeMessage.ReadFromStream(clientStream);

            Assert.NotNull(resultMessage);
            Assert.NotNull(resultMessage.Payload);
            Assert.IsType<PipeVersionPayload>(resultMessage.Payload);

            var resultVersion = (PipeVersionPayload)resultMessage.Payload;

            Assert.InRange(resultVersion.Version, 0, int.MaxValue);
            Assert.InRange(resultVersion.Nonce, 1u, uint.MaxValue);
        }
    }
}
