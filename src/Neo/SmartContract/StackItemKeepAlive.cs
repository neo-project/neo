// Copyright (C) 2015-2026 The Neo Project.
//
// StackItemKeepAlive.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
using System.Collections.Generic;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.SmartContract
{
    /// <summary>
    /// Pins pooled <see cref="Buffer"/> memory that leaves the VM
    /// (neo-vm#595 <c>IMemoryOwner</c>). Call before the engine disposes
    /// items back to <see cref="System.Buffers.MemoryPool{T}"/>.
    /// </summary>
    internal static class StackItemKeepAlive
    {
        public static void Keep(StackItem? item)
        {
            switch (item)
            {
                case Buffer buffer:
                    buffer.KeepAlive();
                    break;
                case CompoundType compound:
                    foreach (var child in compound.SubItems)
                        Keep(child);
                    break;
            }
        }

        public static void KeepAll(IEnumerable<StackItem> items)
        {
            foreach (var item in items)
                Keep(item);
        }
    }
}
