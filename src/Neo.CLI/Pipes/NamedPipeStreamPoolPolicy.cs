// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeStreamPoolPolicy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.ObjectPool;
using System.IO.Pipes;
using NamedPipeOptions = System.IO.Pipes.PipeOptions;

namespace Neo.CLI.Pipes
{
    internal class NamedPipeStreamPoolPolicy(
        NamedPipeEndPoint namedPipeEndPoint,
        NamedPipeTransportOptions namedPipeTransportOptions) : IPooledObjectPolicy<NamedPipeServerStream>
    {
        private readonly NamedPipeEndPoint _endPoint = namedPipeEndPoint;
        private readonly NamedPipeTransportOptions _options = namedPipeTransportOptions;
        private bool _hasFirstPipeStarted;

        public void SetFirstPipeStarted() =>
            _hasFirstPipeStarted = true;

        public NamedPipeServerStream Create()
        {
            var pipeOptions = NamedPipeOptions.Asynchronous | NamedPipeOptions.WriteThrough;
            if (_hasFirstPipeStarted == false)
                pipeOptions |= NamedPipeOptions.FirstPipeInstance;
            if (_options.CurrentUserOnly)
                pipeOptions |= NamedPipeOptions.CurrentUserOnly;
            return new(_endPoint.PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, pipeOptions, inBufferSize: 0, outBufferSize: 0);
        }

        public bool Return(NamedPipeServerStream obj) =>
            obj.IsConnected == false;
    }
}
