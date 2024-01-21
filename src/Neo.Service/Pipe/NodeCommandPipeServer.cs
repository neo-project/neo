// Copyright (C) 2015-2024 The Neo Project.
//
// NodeCommandPipeServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Service.Json;
using Neo.Service.Pipe;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes
{
    internal sealed class NodeCommandPipeServer : IDisposable
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

        private readonly int _readTimeout;
        private readonly ILogger<NodeCommandPipeServer> _logger;
        private readonly NamedPipeServerStream _neoPipeStream;

        public NodeCommandPipeServer(
            int instances,
            int readTimeout,
            ILogger<NodeCommandPipeServer> logger)
        {
            _readTimeout = readTimeout;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _neoPipeStream ??= new(PipeName, PipeDirection.InOut, instances, PipeTransmissionMode.Byte);
        }

        public void Dispose()
        {
            _neoPipeStream?.Dispose();
        }

        public async Task ListenAndWaitAsync(CancellationToken stoppingToken)
        {
            await _neoPipeStream.WaitForConnectionAsync(stoppingToken);
            var stopReadTokenSource = new CancellationTokenSource(_readTimeout);

            try
            {
                while (stoppingToken.IsCancellationRequested == false)
                {
                    try
                    {
                        var jsonString = await _neoPipeStream.ReadLineAsync(stopReadTokenSource.Token);

                        if (string.IsNullOrEmpty(jsonString)) break;
                        var command = JsonSerializer.Deserialize<PipeCommand>(jsonString, JsonOptions);

                        if (stoppingToken.IsCancellationRequested) break;
                        if (command is null) break;
                        _logger.LogInformation("Exec: {Command} {Arguments} by {User}", command.Exec, command.Arguments, _neoPipeStream.GetImpersonationUserName());

                        var result = await command.ExecuteAsync(stoppingToken);
                        jsonString = JsonSerializer.Serialize(result, JsonOptions);

                        await _neoPipeStream.WriteLineAsync(jsonString);
                    }
                    catch (Exception ex)
                    {
                        if (_neoPipeStream.IsConnected)
                            await _neoPipeStream.WriteLineAsync(JsonSerializer.Serialize(new
                            {
                                Type = ex.GetType().Name,
                                Code = ex.HResult,
                                ex.Message,
                            }, typeof(object), JsonOptions));
                        throw;
                    }
                }
            }
            catch (IOException) // client disconnected or IO error
            {

            }
            catch (Exception ex)
            {
                _logger.LogCritical("{ExceptionType}: {Exception}.",
                    ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
            }
            finally
            {
                if (_neoPipeStream.IsConnected)
                    _neoPipeStream.Disconnect();
                _neoPipeStream.Close();
            }
        }
    }
}
