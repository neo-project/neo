// Copyright (C) 2015-2024 The Neo Project.
//
// ConsoleMessageProtocol.cs file belongs to the neo project and is free
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
using System.CommandLine.IO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes.Messaging
{
    internal partial class ConsoleMessageProtocol(
        NamedPipeConnection connection,
        ILogger<ConsoleMessageProtocol> logger) : IThreadPoolWorkItem, IAsyncDisposable, IStandardStreamWriter
    {
        private readonly NamedPipeConnection _connection = connection;
        //private readonly NeoSystem _neoSystem = neoSystem;
        //private readonly NeoOptions _options = options;
        //private readonly LocalNode _localNode = neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;

        private readonly ILogger<ConsoleMessageProtocol> _logger = logger;
        private readonly Stream _input = connection.Transport.Input.AsStream();
        private readonly Stream _output = connection.Transport.Output.AsStream();

        public ValueTask DisposeAsync()
        {
            _logger.LogInformation("Connection shutting down.");

            return _connection.DisposeAsync();
        }

        public void Execute()
        {
            _logger.LogInformation("Connection has started.");

            if (_input.CanRead == false)
                throw new IOException("Input stream of connection can't be read.");

            if (_output.CanWrite == false)
                throw new IOException("Output stream of connection can't be written to.");

            _ = ProcessReceive();
        }

        private async Task ProcessReceive()
        {
            try
            {
                if (NamedPipeMessage.TryDeserialize(_input, out var message))
                {
                    switch (message.Command)
                    {
                        case NamedPipeCommand.Test:
                            _logger.LogInformation($"Received: {nameof(NamedPipeCommand.Test)}");
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
