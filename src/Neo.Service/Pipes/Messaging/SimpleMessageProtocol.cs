// Copyright (C) 2015-2024 The Neo Project.
//
// SimpleMessageProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Logging;
using Neo.IO.Pipes.Protocols;
using Neo.Network.P2P;
using Neo.Service.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes.Messaging
{
    internal partial class SimpleMessageProtocol(
        NamedPipeConnection connection,
        NeoSystem neoSystem,
        NeoOptions options,
        ILogger<SimpleMessageProtocol> logger) : IThreadPoolWorkItem, IAsyncDisposable
    {
        private readonly NamedPipeConnection _connection = connection;
        private readonly NeoSystem _neoSystem = neoSystem;
        private readonly NeoOptions _options = options;
        private readonly LocalNode _localNode = neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;

        private readonly ILogger<SimpleMessageProtocol> _logger = logger;

        public ValueTask DisposeAsync()
        {
            _logger.LogInformation("Connection shutting down.");
            return _connection.DisposeAsync();
        }

        public void Execute()
        {
            _logger.LogInformation("Connection has started.");

            _ = ProcessReceive();
        }

        private async Task ProcessReceive()
        {
            try
            {
                var input = _connection.Transport.Input.AsStream();
                var output = _connection.Transport.Output.AsStream();

                if (input.CanRead == false)
                    throw new IOException("Input stream of connection can't be read.");

                if (output.CanWrite == false)
                    throw new IOException("Output stream of connection can't be written to.");

                if (NamedPipeMessage.TryDeserialize(input, out var message))
                {
                    switch (message.Command)
                    {
                        case NamedPipeCommand.Echo:
                            await output.WriteAsync(message.ToByteArray());
                            _logger.LogInformation($"Received: {nameof(NamedPipeCommand.Echo)}");
                            break;
                        case NamedPipeCommand.ServerInfo:
                            var response = NamedPipeMessage.Create(NamedPipeCommand.ServerInfo, GetServerInfo());
                            await output.WriteAsync(response.ToByteArray());
                            _logger.LogInformation($"Received: {nameof(NamedPipeCommand.ServerInfo)}");
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                await DisposeAsync();
            }
        }
    }
}
