// Copyright (C) 2015-2024 The Neo Project.
//
// PipeServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.IO;
using Neo.Service.IO.Pipes;
using Neo.Service.IO.Pipes.Payloads;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes
{
    internal sealed partial class PipeServer : IDisposable
    {
        public bool IsClientConnected => _neoPipeStream is null ? false : _neoPipeStream.IsConnected;
        public bool IsStreamOpen => _neoPipeStream is not null;

        private readonly ILogger<PipeServer> _logger;

        private NamedPipeServerStream? _neoPipeStream;
        private readonly PipeVersionPayload _versionProtocol;

        public PipeServer(
            int version,
            uint network,
            ILogger<PipeServer> logger)
        {
            _logger = logger;
            _neoPipeStream = new(
                NamedPipeService.PipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly);
            _versionProtocol = PipeVersionPayload.Create(version, network);
        }

        public void Dispose()
        {
            _neoPipeStream?.Dispose();
            _neoPipeStream = null;
        }

        public async Task StartAndListenAsync(CancellationToken cancellationToken = default)
        {
            if (_neoPipeStream is null) throw new NullReferenceException();

            _logger.LogDebug("Waiting for connections.");
            await _neoPipeStream.WaitForConnectionAsync(cancellationToken);

            if (_neoPipeStream is null || cancellationToken.IsCancellationRequested) return;

            _logger.LogDebug("New client connection.");
            TryWriteMessage(PipeMessage.Create(PipeCommand.Version, _versionProtocol));

            while (_neoPipeStream != null &&
                _neoPipeStream.IsConnected &&
                cancellationToken.IsCancellationRequested == false)
            {
                var message = TryReadMessage();
                if (message is null) break;
                try
                {
                    await OnReceive(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    message = PipeMessage.Create(PipeCommand.Error, ExceptionPayload.Create(ex));
                    TryWriteMessage(message);
                }
            }

            _logger.LogDebug("Connection Closed.");
        }

        private async Task OnReceive(PipeMessage message, CancellationToken cancellationToken = default)
        {
            switch (message.Command)
            {
                case PipeCommand.Stop:
                    await StopNeoSystemNodeAsync();
                    break;
                default:
                    break;
            }
        }

        private PipeMessage? TryReadMessage()
        {
            try
            {
                if (_neoPipeStream is null) throw new NullReferenceException();
                var message = PipeMessage.ReadFromStream(_neoPipeStream);
                _logger.LogDebug("New Payload \"{PayloadType}\" from stream.", message?.Payload?.GetType().Name);
                if (message?.Payload is not null)
                    _logger.LogDebug("Payload: {Payload}", message.Payload.ToString());
                return message;
            }
            catch (EndOfStreamException) // Connection lost of closed
            {
                return null;
            }
            catch (Exception ex)
            {
                ex = ex.InnerException ?? ex;
                _logger.LogDebug("{Exception}: {Message}", ex.GetType().Name, ex.Message);
                throw ex;
            }
        }

        private void TryWriteMessage(PipeMessage message)
        {
            if (_neoPipeStream is null) throw new NullReferenceException();
            try
            {
                using var bw = new BinaryWriter(_neoPipeStream, Encoding.UTF8, true);
                bw.Write(message);
                bw.Flush();
                _logger.LogDebug("Sent Payload \"{PayloadType}\" to stream.", message.Payload?.GetType().Name);
                if (message.Payload is not null)
                    _logger.LogDebug("Payload: {Payload}", message.Payload.ToString());
                if (OperatingSystem.IsWindows())
                    _neoPipeStream.WaitForPipeDrain();
            }
            catch (Exception ex)
            {
                ex = ex.InnerException ?? ex;
                _logger.LogDebug("{Exception}: {Message}", ex.GetType().Name, ex.Message);
                throw ex;
            }
        }
    }
}
