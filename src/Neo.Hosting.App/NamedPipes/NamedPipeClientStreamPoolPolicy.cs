// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeClientStreamPoolPolicy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.ObjectPool;
using Neo.Hosting.App.Configuration;
using System.IO.Pipes;
using NamedPipeOptions = System.IO.Pipes.PipeOptions;

namespace Neo.Hosting.App.NamedPipes
{
    internal sealed class NamedPipeClientStreamPoolPolicy
        (NamedPipeEndPoint endPoint,
        NamedPipeClientTransportOptions options) : IPooledObjectPolicy<NamedPipeClientStream>
    {
        private readonly NamedPipeEndPoint _endPoint = endPoint;
        private readonly NamedPipeClientTransportOptions _options = options;

        public NamedPipeClientStream Create()
        {
            var pipeOptions = NamedPipeOptions.Asynchronous | NamedPipeOptions.WriteThrough;
            if (_options.CurrentUserOnly)
                pipeOptions |= NamedPipeOptions.CurrentUserOnly;
            return new(_endPoint.ServerName, _endPoint.PipeName, PipeDirection.InOut, pipeOptions);
        }

        public bool Return(NamedPipeClientStream obj) =>
            obj.IsConnected == false;
    }
}
