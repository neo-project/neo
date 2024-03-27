// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeServerStreamPoolPolicy.cs file belongs to the neo project and is free
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

namespace Neo.Hosting.App.NamedPipes
{
    internal sealed class NamedPipeServerStreamPoolPolicy(
        NamedPipeEndPoint endPoint,
        NamedPipeTransportOptions options) : IPooledObjectPolicy<NamedPipeServerStream>
    {
        private readonly NamedPipeEndPoint _endPoint = endPoint;
        private readonly NamedPipeTransportOptions _options = options;
        private bool _hasFirstPipeStarted;

        public void SetFirstPipeStarted() =>
            _hasFirstPipeStarted = true;

        #region IPooledObjectPolicy

        NamedPipeServerStream IPooledObjectPolicy<NamedPipeServerStream>.Create()
        {
            var pipeOptions = NamedPipeOptions.Asynchronous | NamedPipeOptions.WriteThrough;
            if (!_hasFirstPipeStarted)
                pipeOptions |= NamedPipeOptions.FirstPipeInstance;
            if (_options.CurrentUserOnly)
                pipeOptions |= NamedPipeOptions.CurrentUserOnly;
            return new(_endPoint.PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, pipeOptions, inBufferSize: 0, outBufferSize: 0);
        }

        bool IPooledObjectPolicy<NamedPipeServerStream>.Return(NamedPipeServerStream obj) =>
            obj.IsConnected == false;

        #endregion
    }
}
