// Copyright (C) 2015-2025 The Neo Project.
//
// FasterDbSnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FASTER.core;
using Neo.Extensions;
using Neo.Persistence;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.Build.Core.Storage
{
    public class FasterDbSnapshot : IStoreSnapshot, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        public FasterDbSnapshot(
            FasterDbStore store,
            CheckpointSettings checkpointSettings,
            Guid snapshotId)
        {
            _db = store;
            _snapshotId = snapshotId;
            _writeBatch = new(ByteArrayEqualityComparer.Default);

            _snapshot = NullStorageDevice.Create(checkpointSettings, out _logSettings);

            _snapshot.Recover(_snapshotId);

            _sessionPool = new(
                _logSettings.LogDevice.ThrottleLimit,
                () => _snapshot.For(new ByteArrayFunctions()).NewSession<ByteArrayFunctions>());
        }

        private readonly FasterKV<byte[], byte[]> _snapshot;
        private readonly LogSettings _logSettings;
        private readonly FasterDbStore _db;

        private readonly Guid _snapshotId;
        private readonly ConcurrentDictionary<byte[], byte[]?> _writeBatch;

        private readonly AsyncPool<
            ClientSession<
                byte[],
                byte[],
                byte[],
                byte[],
                Empty,
                ByteArrayFunctions>> _sessionPool;

        public IStore Store => _db;

        public void Dispose()
        {
            _snapshot.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Commit()
        {
            foreach (var kvp in _writeBatch)
            {
                if (kvp.Value is null)
                    _db.Delete(kvp.Key);
                else
                    _db.Put(kvp.Key, kvp.Value);
            }
        }

        public bool Contains(byte[] key) =>
            TryGet(key) != null;

        public void Delete(byte[] key) =>
            _writeBatch[key] = null;

        public void Put(byte[] key, byte[] value) =>
            _writeBatch[key] = value;

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[]? keyOrPrefix, SeekDirection direction)
        {
            keyOrPrefix ??= [];
            if (direction == SeekDirection.Backward && keyOrPrefix.Length == 0) yield break;

            if (_sessionPool.TryGet(out var session) == false)
                session = _sessionPool.Get();

            var keyComparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;

            IEnumerable<KeyValuePair<byte[], byte[]>> items = this;

            if (keyOrPrefix.Length > 0)
                items = items.Where(w => keyComparer.Compare(w.Key, keyOrPrefix) >= 0);

            foreach (var keyValue in items.OrderBy(o => o.Key, keyComparer))
                yield return new(keyValue.Key, keyValue.Value ?? []);

            _sessionPool.Return(session);
        }

        public byte[]? TryGet(byte[] key)
        {
            if (_sessionPool.TryGet(out var session) == false)
                session = _sessionPool.Get();

            var (status, output) = session.Read(key);
            byte[]? value = null;

            if (status.Found)
                value = output;
            if (status.IsPending && session.CompletePendingWithOutputs(out var iter, true, true))
            {
                using (iter)
                {
                    while (iter.Next())
                    {
                        if (iter.Current.Key.AsSpan().SequenceEqual(key))
                        {
                            value = iter.Current.Output;
                            break;
                        }
                    }
                }
            }

            _sessionPool.Return(session);

            return value;
        }

        public bool TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value)
        {
            value = TryGet(key);
            return value != null;
        }

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator()
        {
            if (_sessionPool.TryGet(out var session) == false)
                session = _sessionPool.Get();

            using var iter = session.Iterate();

            while (iter.GetNext(out _))
            {
                var key = iter.GetKey();
                var value = iter.GetValue();

                yield return new(key, value);
            }

            _sessionPool.Return(session);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
