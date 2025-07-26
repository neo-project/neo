// Copyright (C) 2015-2025 The Neo Project.
//
// Store.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Neo.Plugins.Storage
{
    internal class Store : IStore
    {
        private readonly RocksDb _db;

        /// <inheritdoc/>
        public event IStore.OnNewSnapshotDelegate? OnNewSnapshot;
        public event OnPutDelegate<byte[], byte[]>? OnPut;
        public event OnDeleteDelegate<byte[]>? OnDelete;
        public event OnTryGetDelegate<byte[]>? OnTryGet;
        public event OnContainsDelegate<byte[]>? OnContains;
        public event OnFindDelegate<byte[]>? OnFind;

        public Store(string path)
        {
            _db = RocksDb.Open(Options.Default, Path.GetFullPath(path));
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public IStoreSnapshot GetSnapshot()
        {
            var snapshot = new Snapshot(this, _db);
            OnNewSnapshot?.Invoke(this, snapshot);
            return snapshot;
        }

        /// <inheritdoc/>
        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte[]? keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            OnFind?.Invoke(keyOrPrefix, direction);

            keyOrPrefix ??= [];

            using var it = _db.NewIterator();
            if (direction == SeekDirection.Forward)
                for (it.Seek(keyOrPrefix); it.Valid(); it.Next())
                    yield return (it.Key(), it.Value());
            else
                for (it.SeekForPrev(keyOrPrefix); it.Valid(); it.Prev())
                    yield return (it.Key(), it.Value());
        }

        public bool Contains(byte[] key)
        {
            OnContains?.Invoke(key);
            return _db.Get(key, Array.Empty<byte>(), 0, 0) >= 0;
        }

        public byte[]? TryGet(byte[] key)
        {
            OnTryGet?.Invoke(key);
            return _db.Get(key);
        }

        public bool TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value)
        {
            OnTryGet?.Invoke(key);
            value = _db.Get(key);
            return value != null;
        }

        public void Delete(byte[] key)
        {
            OnDelete?.Invoke(key);
            _db.Remove(key);
        }

        public void Put(byte[] key, byte[] value)
        {
            OnPut?.Invoke(key, value);
            _db.Put(key, value);
        }

        public void PutSync(byte[] key, byte[] value)
        {
            OnPut?.Invoke(key, value);
            _db.Put(key, value, writeOptions: Options.WriteDefaultSync);
        }
    }
}
