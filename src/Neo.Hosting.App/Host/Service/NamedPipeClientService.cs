// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeClientService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.NamedPipes;
using Neo.Hosting.App.NamedPipes.Protocol;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.Host.Service
{
    internal sealed class NamedPipeClientService
    {
        public NamedPipeEndPoint EndPoint => _endPoint;

        private readonly ILoggerFactory _loggerFactory;
        private readonly NeoOptions _neoOptions;

        private NamedPipeClientConnection? _connection;
        private NamedPipeEndPoint _endPoint;
        private NamedPipeClient? _client;

        public NamedPipeClientService(
            ILoggerFactory loggerFactory,
            IOptions<NeoOptions> options)
        {
            _neoOptions = options.Value;
            _loggerFactory = loggerFactory;
            _endPoint = new NamedPipeEndPoint(options);
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) =>
            ConnectAsync(_endPoint, cancellationToken);

        public async Task ConnectAsync(NamedPipeEndPoint endPoint, CancellationToken cancellationToken = default)
        {
            _endPoint = endPoint;
            _client = new NamedPipeClient(_endPoint, _loggerFactory, Options.Create<NamedPipeClientTransportOptions>(new()));
            _connection = await _client.ConnectAsync(cancellationToken);
        }

        public async Task<PipeVersion?> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            if (_connection is null)
                return null;

            var requestId = Random.Shared.Next();
            var message = PipeMessage.Create(requestId, PipeCommand.GetVersion, PipeMessage.Null);
            await _connection.WriteAsync(message, cancellationToken);

            var response = await _connection.ReadAsync(cancellationToken);

            if (response is not null && response.RequestId != requestId)
                throw new IOException("Invalid response");

            return response?.Payload as PipeVersion;
        }
    }
}
