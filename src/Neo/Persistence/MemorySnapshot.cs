// Copyright (C) 2015-2025 The Neo Project.
//
// MemorySnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.Persistence
{
    /// <summary>
    /// <remarks>On-chain write operations on a snapshot cannot be concurrent.</remarks>
    /// </summary>
    internal class MemorySnapshot : IStoreSnapshot
    {
        private readonly ConcurrentDictionary<byte[], byte[]> _innerData;
        private readonly ImmutableDictionary<byte[], byte[]> _immutableData;
        private readonly ConcurrentDictionary<byte[], byte[]?> _writeBatch;

        public SerializedCache SerializedCache { get; }

        public IStore Store { get; }

        internal MemorySnapshot(MemoryStore store, ConcurrentDictionary<byte[], byte[]> innerData, SerializedCache serializedCache)
        {
            Store = store;
            _innerData = innerData;
            SerializedCache = serializedCache;
            _immutableData = innerData.ToImmutableDictionary(ByteArrayEqualityComparer.Default);
            _writeBatch = new ConcurrentDictionary<byte[], byte[]?>(ByteArrayEqualityComparer.Default);
        }

        public void Commit()
        {
            foreach (var pair in _writeBatch)
                if (pair.Value is null)
                    _innerData.TryRemove(pair.Key, out _);
                else
                    _innerData[pair.Key] = pair.Value;
        }

        public void Delete(byte[] key)
        {
            _writeBatch[key] = null;
        }

        public void Dispose() { }

        public void Put(byte[] key, byte[] value)
        {
            _writeBatch[key[..]] = value[..];
        }

        /// <inheritdoc/>
        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[]? keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            keyOrPrefix ??= [];
            if (direction == SeekDirection.Backward && keyOrPrefix.Length == 0) yield break;

            var comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<byte[], byte[]>> records = _immutableData;
            if (keyOrPrefix.Length > 0)
                records = records
                    .Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);
            foreach (var pair in records)
                yield return (pair.Key[..], pair.Value[..]);
        }

        public byte[]? TryGet(byte[] key)
        {
            _immutableData.TryGetValue(key, out var value);
            return value?[..];
        }

        public bool TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value)
        {
            return _immutableData.TryGetValue(key, out value);
        }

        public bool Contains(byte[] key)
        {
            return _immutableData.ContainsKey(key);
        }
    }
}
