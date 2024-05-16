// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Neo.Hosting.App.NamedPipes
{
    internal sealed class NamedPipeClient : IAsyncDisposable
    {
        private readonly NamedPipeClientTransportOptions _options;
        private readonly NamedPipeClientStreamPoolPolicy _poolPolicy;
        private readonly ObjectPool<NamedPipeClientStream> _pool;
        private readonly MemoryPool<byte> _memoryPool;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private readonly PipeOptions _inputOptions;
        private readonly PipeOptions _outputOptions;

        public NamedPipeEndPoint LocalEndPoint { get; }

        public NamedPipeClient(
            NamedPipeEndPoint endPoint,
            ILoggerFactory loggerFactory,
            IOptions<NamedPipeClientTransportOptions> options)
        {
            LocalEndPoint = endPoint;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<NamedPipeClient>();
            _options = options?.Value ?? new NamedPipeClientTransportOptions();
            _poolPolicy = new NamedPipeClientStreamPoolPolicy(LocalEndPoint, _options);
            _memoryPool = _options.MemoryPoolFactory();

            var objectPoolProvider = new DefaultObjectPoolProvider();
            _pool = objectPoolProvider.Create(_poolPolicy);

            var maxReadBufferSize = _options.MaxReadBufferSize;
            var maxWriteBufferSize = _options.MaxWriteBufferSize;

            _inputOptions = new PipeOptions(_memoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
            _outputOptions = new PipeOptions(_memoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);
        }

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
