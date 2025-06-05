// Copyright (C) 2015-2025 The Neo Project.
//
// FasterDbStore.cs file belongs to the neo project and is free
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Neo.Build.Core.Storage
{
    public class FasterDbStore : IStore, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        public FasterDbStore(
            string dirPath,
            Guid? checkpointId = null)
        {
            _storePath = Path.GetFullPath(dirPath);
            _store = LocalStorageDevice.Create(_storePath, out _logSettings, out _checkpointSettings);

            if (checkpointId.HasValue)
                _store.Recover(checkpointId.Value);

            _sessionPool = new(
                _logSettings.LogDevice.ThrottleLimit,
                () => _store.For(new ByteArrayFunctions()).NewSession<ByteArrayFunctions>());
        }

        private readonly string _storePath;
        private readonly FasterKV<byte[], byte[]> _store;
        private readonly CheckpointSettings _checkpointSettings;
        private readonly LogSettings _logSettings;
        private readonly AsyncPool<
            ClientSession<
                byte[],
                byte[],
                byte[],
                byte[],
                Empty,
                ByteArrayFunctions>> _sessionPool;

        /// <inheritdoc/>
        public event IStore.OnNewSnapshotDelegate? OnNewSnapshot;

        public void Dispose()
        {
            //_store.CheckpointManager.PurgeAll();
            _store.TryInitiateFullCheckpoint(out _, CheckpointType.Snapshot);
            _store.CompleteCheckpointAsync().AsTask().GetAwaiter().GetResult();
            _store.Log.FlushAndEvict(true);
            _store.Dispose();
            _sessionPool.Dispose();
            GC.SuppressFinalize(this);
        }

        public Guid CreateFullCheckPoint()
        {
            _store.TryInitiateFullCheckpoint(out var checkpointId, CheckpointType.Snapshot);
            _store.CompleteCheckpointAsync().AsTask().GetAwaiter().GetResult();
            return checkpointId;
        }

        public void Reset() =>
            _store.Reset();

        public bool Contains(byte[] key) =>
            TryGet(key) != null;

        public void Delete(byte[] key)
        {
            if (_sessionPool.TryGet(out var session) == false)
                session = _sessionPool.Get();

            var status = session.Delete(key);

            if (status.IsPending)
                session.CompletePending(true, true);

            _sessionPool.Return(session);
        }

        public IStoreSnapshot GetSnapshot()
        {
            _store.TryInitiateFullCheckpoint(out var snapshotId, CheckpointType.Snapshot);
            _store.CompleteCheckpointAsync().AsTask().GetAwaiter().GetResult();
            _store.Log.FlushAndEvict(true);

            var snapshot = new FasterDbSnapshot(this, _checkpointSettings, snapshotId);
            OnNewSnapshot?.Invoke(this, snapshot);

            return snapshot;
        }

        public void Put(byte[] key, byte[] value)
        {
            if (_sessionPool.TryGet(out var session) == false)
                session = _sessionPool.Get();

            var status = session.Upsert(key, value);

            if (status.IsPending)
                session.CompletePending(true, true);

            _sessionPool.Return(session);
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte[]? keyOrPrefix, SeekDirection direction)
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
                yield return new(keyValue.Key, keyValue.Value);

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

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
