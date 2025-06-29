// Copyright (C) 2015-2025 The Neo Project.
//
// NeoMessageReceiver.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Neo.IO
{
    internal abstract class NeoMessageReceiver<T>
    {
        private readonly Channel<T> _messageQueue = Channel.CreateUnbounded<T>(
                new UnboundedChannelOptions()
                {
                    SingleReader = true,
                });

        public NeoMessageReceiver()
        {
            Task.Run(() => StartAsync(CancellationToken.None));
        }

        public abstract void OnReceive(T message);

        public async Task Tell(T message)
        {
            if (_messageQueue.Writer.TryWrite(message) == false)
            {
                if (await _messageQueue.Writer.WaitToWriteAsync(CancellationToken.None) == false)
                    throw new InvalidOperationException("Message queue writer was unexpectedly closed.");
            }
        }

        public async Task Tell(params T[] messages)
        {
            foreach (var message in messages)
            {
                await Tell(message);
            }
        }

        private async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            while (await _messageQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_messageQueue.Reader.TryRead(out var message))
                    OnReceive(message);
            }
        }
    }
}
