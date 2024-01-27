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
using Neo.Service.Pipes.Payloads;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Neo.Service.Pipes
{
    internal sealed class PipeServer : IDisposable
    {
        public bool IsConnected => _neoPipeStream is null ? false : _neoPipeStream.IsConnected;
        public bool HasStream => _neoPipeStream is not null;

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

        public void StartAndListen()
        {
            if (_neoPipeStream is null) throw new NullReferenceException();

            _logger.LogDebug("Waiting for connections.");
            _neoPipeStream.WaitForConnection();

            _logger.LogDebug("New client connection.");
            TryWriteMessage(PipeMessage.Create(PipeMessageCommand.Version, _versionProtocol));

            while (_neoPipeStream != null && _neoPipeStream.IsConnected)
            {
                var message = TryReadMessage();
                if (message is null) break;
            }

            _logger.LogDebug("Connection Closed.");
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
            }
        }
    }
}
