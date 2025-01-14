// Copyright (C) 2015-2024 The Neo Project.
//
// MemorySnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Neo.Persistence
{
    /// <summary>
    /// <remarks>On-chain write operations on a snapshot cannot be concurrent.</remarks>
    /// </summary>
    internal class MemorySnapshot : ISnapshot
    {
        private readonly ConcurrentDictionary<byte[], byte[]> innerData;
        private readonly ImmutableDictionary<byte[], byte[]> immutableData;
        private readonly ConcurrentDictionary<byte[], byte[]> writeBatch;

        public MemorySnapshot(ConcurrentDictionary<byte[], byte[]> innerData)
        {
            this.innerData = innerData;
            immutableData = innerData.ToImmutableDictionary(ByteArrayEqualityComparer.Default);
            writeBatch = new ConcurrentDictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);
        }

        public void Commit()
        {
            foreach (var pair in writeBatch)
                if (pair.Value is null)
                    innerData.TryRemove(pair.Key, out _);
                else
                    innerData[pair.Key] = pair.Value;
        }

        public void Delete(byte[] key)
        {
            writeBatch[key] = null;
        }

        public void Dispose() { }

        public void Put(byte[] key, byte[] value)
        {
            writeBatch[key[..]] = value[..];
        }

        /// <inheritdoc/>
        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            ByteArrayComparer comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<byte[], byte[]>> records = immutableData;
            if (keyOrPrefix?.Length > 0)
                records = records.Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);
            return records.Select(p => (p.Key[..], p.Value[..]));
        }

        public byte[] TryGet(byte[] key)
        {
            immutableData.TryGetValue(key, out byte[] value);
            return value?[..];
        }

        public bool TryGet(byte[] key, out byte[] value)
        {
            return immutableData.TryGetValue(key, out value);
        }

        public bool Contains(byte[] key)
        {
            return immutableData.ContainsKey(key);
        }
    }
}
