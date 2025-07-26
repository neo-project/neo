// Copyright (C) 2015-2025 The Neo Project.
//
// MemoryStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Persistence.Providers
{
    /// <summary>
    /// An in-memory <see cref="IStore"/> implementation that uses ConcurrentDictionary as the underlying storage.
    /// </summary>
    public class MemoryStore : IStore
    {
        private readonly ConcurrentDictionary<byte[], byte[]> _innerData = new(ByteArrayEqualityComparer.Default);

        /// <inheritdoc/>
        public event IStore.OnNewSnapshotDelegate? OnNewSnapshot;
        public event OnPutDelegate<byte[], byte[]>? OnPut;
        public event OnDeleteDelegate<byte[]>? OnDelete;
        public event OnTryGetDelegate<byte[]>? OnTryGet;
        public event OnContainsDelegate<byte[]>? OnContains;
        public event OnFindDelegate<byte[]>? OnFind;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Delete(byte[] key)
        {
            OnDelete?.Invoke(key);
            _innerData.TryRemove(key, out _);
        }

        public void Dispose() =>
            GC.SuppressFinalize(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IStoreSnapshot GetSnapshot()
        {
            var snapshot = new MemorySnapshot(this, _innerData);
            OnNewSnapshot?.Invoke(this, snapshot);
            return snapshot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Put(byte[] key, byte[] value)
        {
            OnPut?.Invoke(key, value);
            _innerData[key[..]] = value[..];
        }

        /// <inheritdoc/>
        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte[]? keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            OnFind?.Invoke(keyOrPrefix, direction);

            keyOrPrefix ??= [];
            if (direction == SeekDirection.Backward && keyOrPrefix.Length == 0) yield break;

            var comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;

            IEnumerable<KeyValuePair<byte[], byte[]>> records = _innerData;

            if (keyOrPrefix.Length > 0)
                records = records
                    .Where(p => comparer.Compare(p.Key, keyOrPrefix) >= 0);
            records = records.OrderBy(p => p.Key, comparer);

            foreach (var pair in records)
                yield return (pair.Key[..], pair.Value[..]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[]? TryGet(byte[] key)
        {
            OnTryGet?.Invoke(key);
            if (!_innerData.TryGetValue(key, out var value)) return null;
            return value[..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value)
        {
            OnTryGet?.Invoke(key);
            return _innerData.TryGetValue(key, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(byte[] key)
        {
            OnContains?.Invoke(key);
            return _innerData.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            _innerData.Clear();
        }
    }
}

#nullable disable
