// Copyright (C) 2015-2025 The Neo Project.
//
// ITransportConnection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network.P2P.Transports
{
    internal interface ITransportConnection : IAsyncDisposable
    {
        IPEndPoint RemoteEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }

        void Start(IActorRef receiver);

        Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

        Task CloseAsync(bool abort, CancellationToken cancellationToken);
    }
}
