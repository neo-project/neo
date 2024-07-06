// Copyright (C) 2015-2024 The Neo Project.
//
// Snapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Persistence;
using RocksDbSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly RocksDb db;
        private readonly RocksDbSharp.Snapshot snapshot;
        private readonly WriteBatch batch;
        private readonly ReadOptions options;
        private readonly ConcurrentDictionary<byte[], byte[]> _dataCache;
        private readonly ReaderWriterLockSlim rwLock = new();

        public Snapshot(RocksDb db)
        {
            this.db = db;
            snapshot = db.CreateSnapshot();
            batch = new WriteBatch();

            options = new ReadOptions();
            options.SetFillCache(false);
            options.SetSnapshot(snapshot);

            _dataCache = new ConcurrentDictionary<byte[], byte[]>(new ByteArrayEqualityComparer());
        }

        public void Commit()
        {
            rwLock.EnterWriteLock();
            try
            {
                _dataCache.Clear();
                db.Write(batch, Options.WriteDefault);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void Delete(byte[] key)
        {
            rwLock.EnterWriteLock();
            try
            {
                _dataCache.TryRemove(key, out _);
                batch.Delete(key);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void Put(byte[] key, byte[] value)
        {
            rwLock.EnterWriteLock();
            try
            {
                _dataCache[key] = value;
                batch.Put(key, value);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction)
        {
            if (keyOrPrefix == null) keyOrPrefix = Array.Empty<byte>();

            using var it = db.NewIterator(readOptions: options);
            rwLock.EnterReadLock();
            try
            {
                if (direction == SeekDirection.Forward)
                {
                    for (it.Seek(keyOrPrefix); it.Valid(); it.Next())
                        yield return (it.Key(), it.Value());
                }
                else
                {
                    for (it.SeekForPrev(keyOrPrefix); it.Valid(); it.Prev())
                        yield return (it.Key(), it.Value());
                }
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        public bool Contains(byte[] key)
        {
            rwLock.EnterReadLock();
            try
            {
                return _dataCache.ContainsKey(key) || db.Get(key, readOptions: options) != null;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        public byte[] TryGet(byte[] key)
        {
            rwLock.EnterReadLock();
            try
            {
                return _dataCache.TryGetValue(key, out byte[] value) ? value : db.Get(key, readOptions: options);
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            rwLock.EnterWriteLock();
            try
            {
                _dataCache.Clear();
                snapshot.Dispose();
                batch.Dispose();
            }
            finally
            {
                rwLock.ExitWriteLock();
                rwLock.Dispose();
            }
        }
    }
}
