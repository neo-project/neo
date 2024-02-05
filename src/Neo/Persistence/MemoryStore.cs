// Copyright (C) 2015-2024 The Neo Project.
//
// MemoryStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Persistence
{
    /// <summary>
    /// An in-memory <see cref="IStore"/> implementation that uses ConcurrentDictionary as the underlying storage.
    /// </summary>
    public class MemoryStore : IStore
    {
        private readonly ConcurrentDictionary<byte[], byte[]> _innerData = new(ByteArrayEqualityComparer.Default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Delete(byte[] key)
        {
            _innerData.TryRemove(key, out _);
        }

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ISnapshot GetSnapshot()
        {
            return new MemorySnapshot(_innerData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Put(byte[] key, byte[] value)
        {
            _innerData[key[..]] = value[..];
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            if (direction == SeekDirection.Backward && keyOrPrefix?.Length == 0) yield break;

            var comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<byte[], byte[]>> records = _innerData;
            if (keyOrPrefix?.Length > 0)
                records = records.Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);
            foreach (var pair in records)
                yield return (pair.Key[..], pair.Value[..]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] TryGet(byte[] key)
        {
            if (!_innerData.TryGetValue(key, out byte[] value)) return null;
            return value[..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(byte[] key)
        {
            return _innerData.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            _innerData.Clear();
        }
    }
}
