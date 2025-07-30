// Copyright (C) 2015-2025 The Neo Project.
//
// WebSocketService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Neo.Build.Core;
using Neo.Build.Core.Exceptions;
using Neo.Build.Core.Models;
using Neo.Build.ToolSet.Net;
using System;
using System.Buffers;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet.Services
{
    internal class WebSocketService : IHostedService
    {
        public WebSocketService(
            IConsole console)
        {
            _console = console;
            _socketOptions = new();
            _webHost = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.AddServerHeader = false;
                    options.ListenLocalhost(30841);
                })
                .Configure(app =>
                {
                    _socketOptions.KeepAliveInterval = TimeSpan.FromMinutes(2);

                    app.UseWebSockets(_socketOptions);
                    app.Run(ProcessRequestsAsync);
                })
                .Build();
        }

        public WebSocketOptions SocketOptions => _socketOptions;

        private static RpcDispatcher JsonRpcDispatcher { get; } = new();
        
        private readonly IWebHost _webHost;
        private readonly WebSocketOptions _socketOptions;
        private readonly IConsole _console;

        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource _tcs = new();
        private bool _isStarted = false;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isStarted)
                // TODO: Make custom exception class
                throw new NeoBuildException($"{nameof(WebSocketService)} is already running.", NeoBuildErrorCodes.General.InternalException);

            _webHost.StartAsync(_cts.Token).ConfigureAwait(false);

            _isStarted = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return _cts.CancelAsync();
        }

        private async Task ProcessRequestsAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest == false)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

            var pipe = new Pipe();
            var writer = FillPipeAsync(webSocket, pipe.Writer);
            var reader = ReadPipeAsync(webSocket, pipe.Reader);

            await _tcs.Task;
        }

        private static bool TryReadJson(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out string? json)
        {
            json = Encoding.UTF8.GetString(buffer.ToArray());
            buffer = buffer.Slice(buffer.End);
            return string.IsNullOrWhiteSpace(json) == false;
        }

        private async Task SendJson(WebSocket webSocket, object message)
        {
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message);
            var segment = new ArraySegment<byte>(jsonBytes);

            await webSocket.SendAsync(segment, WebSocketMessageType.Text, endOfMessage: true, _cts.Token);
        }

        private async Task FillPipeAsync(WebSocket socket, PipeWriter writer)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                writer.Write(buffer.AsSpan(0, result.Count));

                if (result.EndOfMessage)
                    await writer.FlushAsync();
            }

            await writer.CompleteAsync();
            _tcs.TrySetResult();
        }

        private async Task ReadPipeAsync(WebSocket webSocket, PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                while (TryReadJson(ref buffer, out var jsonString))
                {
                    try
                    {
                        var request = JsonSerializer.Deserialize<JsonRpcRequest<object>>(jsonString);
                        var response = await JsonRpcDispatcher.DispatchAsync(request);

                        await SendJson(webSocket, response);

                        //_console.Write(jsonString);
                    }
                    catch (JsonException ex)
                    {
                        await SendJson(webSocket, new JsonRpcError() { Code = -32700, Message = ex.Message });
                    }
                    catch (Exception ex)
                    {
                        _tcs.TrySetException(ex);
                    }
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted) break;
            }

            await reader.CompleteAsync();
            _tcs.TrySetResult();
        }
    }
}
