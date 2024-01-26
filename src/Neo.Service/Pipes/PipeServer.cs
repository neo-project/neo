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
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Neo.Service.Pipes
{
    internal sealed class PipeServer : IDisposable
    {
        public bool IsConnected => _neoPipeStream is null ? false : _neoPipeStream.IsConnected;
        public bool IsShutdown => _neoPipeStream is null;

        private readonly ILogger<PipeServer> _logger;

        private NamedPipeServerStream? _neoPipeStream;

        public PipeServer(
            ILogger<PipeServer> logger)
        {
            _logger = logger;
            _neoPipeStream = new(
                NamedPipeService.PipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            _neoPipeStream.ReadTimeout = _neoPipeStream.WriteTimeout = 1000;
        }

        public void Dispose()
        {
            _neoPipeStream?.Dispose();
            _neoPipeStream = null;
        }

        public void StartAndListen()
        {
            if (_neoPipeStream is null) throw new NullReferenceException();
            _neoPipeStream.WaitForConnection();

            while (_neoPipeStream.IsConnected)
            {
                var message = TryReadMessage();
                if (message is null) break;
            }
        }

        private PipeMessage? TryReadMessage()
        {
            if (_neoPipeStream is null) throw new NullReferenceException();
            using var br = new BinaryReader(_neoPipeStream, Encoding.UTF8, true);
            return PipeMessage.TryDeserialize(br);
        }

        private void TryWriteMessage(PipeMessage message)
        {
            if (_neoPipeStream is null) throw new NullReferenceException();
            using var bw = new BinaryWriter(_neoPipeStream, Encoding.UTF8, true);
            bw.Write(message);
        }
    }
}
