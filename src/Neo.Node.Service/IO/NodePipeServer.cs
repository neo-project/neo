// Copyright (C) 2015-2024 The Neo Project.
//
// NodePipeServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Node.Service.Json;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Node.Service.IO
{
    internal sealed class NodeCommandPipeServer
    {
        public static Version Version => NodeUtilities.GetApplicationVersion();
        public static string PipeName => $"neo.node\\{Version.ToString(3)}\\CommandShell";
        public static JsonSerializerOptions JsonOptions => new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = new LowerInvariantCaseNamingPolicy(),
            WriteIndented = false,
        };

        private readonly Logger<NodeCommandPipeServer> _logger;
        private readonly NamedPipeServerStream _neoPipeStream;

        private CancellationTokenSource? _cancellationTokenSource;

        public NodeCommandPipeServer(
            Logger<NodeCommandPipeServer> logger)
        {
            _logger = logger;
            _neoPipeStream = new(PipeName, PipeDirection.In, 4, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly);
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            if (_cancellationTokenSource != null || _cancellationTokenSource!.IsCancellationRequested == false) return;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            var commandTypeNames = Enum.GetNames<CommandType>();

            while (_cancellationTokenSource.IsCancellationRequested == false)
            {
                _logger.LogInformation("Waiting for connection on thread {ThreadId}.", Environment.CurrentManagedThreadId);

                await _neoPipeStream.WaitForConnectionAsync(stoppingToken);

                _logger.LogDebug("Got a connection.");

                try
                {
                    while (_cancellationTokenSource.IsCancellationRequested == false)
                    {
                        var command = await JsonSerializer.DeserializeAsync<PipeCommand>(_neoPipeStream, JsonOptions, _cancellationTokenSource.Token);

                        if (command == null || commandTypeNames.Any(a => a == $"{command}") == false)
                        {
                            _logger.LogDebug("Command null or not found.");
                            _neoPipeStream.Disconnect();
                            continue;
                        }

                        _logger.LogDebug("Exec: {Command} {Arguments}", command.Command, string.Join(' ', command.Arguments));

                        try
                        {
                            var result = await command.ExecuteAsync(_cancellationTokenSource.Token);

                            _logger.LogDebug("Got result {Result}.", result?.GetType().Name);

                            await JsonSerializer.SerializeAsync(_neoPipeStream, result, JsonOptions, _cancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical("{ExceptionType}: {Exception}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                        }
                    }
                }
                catch (IOException ex) // client disconnected or read error
                {
                    _logger.LogInformation("{Exception}", ex.InnerException?.Message ?? ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("{ExceptionType}: {Exception}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                }
                finally
                {
                    if (_neoPipeStream.IsConnected)
                        _neoPipeStream.Disconnect();
                }
            }
        }
    }
}
