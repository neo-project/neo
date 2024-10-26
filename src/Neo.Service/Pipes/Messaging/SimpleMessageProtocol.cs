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

using Microsoft.Extensions.Logging;
using Neo.IO.Pipes.Protocols;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes.Messaging
{
    internal class SimpleMessageProtocol(
        NamedPipeConnection connection,
        ILogger<SimpleMessageProtocol> logger) : IThreadPoolWorkItem, IAsyncDisposable
    {
        private readonly NamedPipeConnection _connection = connection;
        private readonly ILogger<SimpleMessageProtocol> _logger = logger;

        public ValueTask DisposeAsync()
        {
            return _connection.DisposeAsync();
        }

        public void Execute()
        {
            _logger.LogInformation("Connection has started.");

            _ = ProcessRequests();
        }

        private async Task ProcessRequests()
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
                            break;
                        case NamedPipeCommand.ServerInfo:
                            break;
                        case NamedPipeCommand.Exception:
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
