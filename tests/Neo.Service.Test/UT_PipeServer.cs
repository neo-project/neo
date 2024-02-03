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
using Neo.Service.Tests.Helpers;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Neo.Service.Tests
{
    public partial class UT_PipeServer : IAsyncLifetime, IDisposable
    {
        private const uint TEST_NETWORK = 0x54455354; // Magic code ("TEST")

        private readonly ILoggerFactory _loggerFactory;
        private readonly PipeServer _pipeServer;

        private Task? _pipeServerTask;

        public UT_PipeServer(
            ITestOutputHelper outputHelper)
        {
            _loggerFactory = UT_Utilities.CreateLogFactory(outputHelper);
            _pipeServer = new PipeServer(
                NodeUtilities.GetApplicationVersionNumber(),
                TEST_NETWORK,
                _loggerFactory.CreateLogger<PipeServer>());
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task InitializeAsync()
        {
            _pipeServerTask = _pipeServer.StartAndListenAsync();

            Assert.False(_pipeServerTask.IsFaulted);
            Assert.True(_pipeServer.IsStreamOpen);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _pipeServer?.Dispose();
            await _pipeServerTask!;
        }
    }
}
