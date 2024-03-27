// Copyright (C) 2015-2024 The Neo Project.
//
// PinnedBlockMemoryPool.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace Neo.Hosting.App.Buffers
{
    internal sealed class PinnedBlockMemoryPool : MemoryPool<byte>
    {
        private const int AnySize = -1;

        private static readonly int s_blockSize = 4096;

        public static int BlockSize => s_blockSize;

        public override int MaxBufferSize { get; } = s_blockSize;

        private readonly ConcurrentQueue<MemoryPoolBlock> _blocks = new();
        private readonly object _disposedSync = new();

        private bool _isDisposed;

        public override IMemoryOwner<byte> Rent(int size = AnySize)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(size, s_blockSize);
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_blocks.TryDequeue(out var block))
                return block;

            return new MemoryPoolBlock(this, BlockSize);
        }

        internal void Return(MemoryPoolBlock block)
        {
            if (_isDisposed == false)
                _blocks.Enqueue(block);
        }

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            lock (_disposedSync)
            {
                _isDisposed = true;

                if (disposing)
                {
                    while (_blocks.TryDequeue(out _))
                    {
                    }
                }
            }
        }
    }
}
