// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeTransportOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Service.Buffers;
using System;
using System.Buffers;

namespace Neo.Service.Pipes
{
    internal class NamedPipeTransportOptions
    {
        public int ListenerQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);
        public long MaxReadBufferSize { get; set; } = 1024 * 1024;
        public long MaxWriteBufferSize { get; set; } = 64 * 1024;
        public bool CurrentUserOnly { get; set; } = true;
        internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = () => new PinnedBlockMemoryPool();
    }
}
