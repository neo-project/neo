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
using System;
using System.Collections.Generic;
using System.Reflection;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.SmartContract
{
    /// <summary>
    /// Pins pooled <see cref="Buffer"/> memory that leaves the VM
    /// (neo-vm#595 <c>IMemoryOwner</c>). Also pins <see cref="ByteString"/>
    /// when that type exposes <c>KeepAlive()</c>. Walks compounds iteratively
    /// with reference equality so cyclic ResultStack graphs cannot overflow.
    /// Call before the engine disposes items back to <see cref="System.Buffers.MemoryPool{T}"/>.
    /// This type does not invoke <c>Cleanup()</c>.
    /// </summary>
    internal static class StackItemKeepAlive
    {
        private static readonly Action<ByteString>? s_keepByteString = BindKeepAlive<ByteString>();

        public static void Keep(StackItem? item)
            => Keep(item, new HashSet<CompoundType>(ReferenceEqualityComparer.Instance));

        public static void KeepAll(IEnumerable<StackItem> items)
        {
            var visited = new HashSet<CompoundType>(ReferenceEqualityComparer.Instance);
            foreach (var item in items)
                Keep(item, visited);
        }

        private static void Keep(StackItem? item, HashSet<CompoundType> visited)
        {
            if (item is null)
                return;

            var pending = new Stack<StackItem>();
            pending.Push(item);
            while (pending.Count > 0)
            {
                switch (pending.Pop())
                {
                    case Buffer buffer:
                        buffer.KeepAlive();
                        break;
                    case ByteString byteString:
                        s_keepByteString?.Invoke(byteString);
                        break;
                    case CompoundType compound:
                        if (!visited.Add(compound))
                            break;
                        foreach (var child in compound.SubItems)
                            pending.Push(child);
                        break;
                }
            }
        }

        private static Action<T>? BindKeepAlive<T>()
        {
            var method = typeof(T).GetMethod(nameof(Buffer.KeepAlive), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method?.CreateDelegate<Action<T>>();
        }
    }
}
