// Copyright (C) 2015-2024 The Neo Project.
//
// MemoryPoolBlock.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Neo.Service.App.Buffers
{
    internal sealed class MemoryPoolBlock : IMemoryOwner<byte>
    {
        public PinnedBlockMemoryPool Pool { get; }

        internal MemoryPoolBlock(
            PinnedBlockMemoryPool pool,
            int length)
        {
            Pool = pool;

            var pinnedArray = GC.AllocateUninitializedArray<byte>(length, pinned: true);

            Memory = MemoryMarshal.CreateFromPinnedArray(pinnedArray, 0, pinnedArray.Length);
        }

        #region IMemoryOwner

        public Memory<byte> Memory { get; }

        void IDisposable.Dispose()
        {
            Pool.Return(this);
        }

        #endregion
    }
}
