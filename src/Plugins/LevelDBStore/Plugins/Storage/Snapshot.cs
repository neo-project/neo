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
using Neo.IO.Data.LevelDB;
using Neo.Persistence;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using LSnapshot = Neo.IO.Data.LevelDB.Snapshot;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly DB db;
        private readonly LSnapshot snapshot;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly ConcurrentDictionary<byte[], byte[]> _dataCache;
        private readonly ReaderWriterLockSlim rwLock = new();

        public Snapshot(DB db)
        {
            this.db = db;
            snapshot = db.GetSnapshot();
            options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            batch = new WriteBatch();
            _dataCache = new ConcurrentDictionary<byte[], byte[]>(new ByteArrayEqualityComparer());
        }

        public void Commit()
        {
            rwLock.EnterWriteLock();
            try
            {
                _dataCache.Clear();
                db.Write(WriteOptions.Default, batch);
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

        public void Dispose()
        {
            rwLock.EnterWriteLock();
            try
            {
                _dataCache.Clear();
                snapshot.Dispose();
            }
            finally
            {
                rwLock.ExitWriteLock();
                rwLock.Dispose();
            }
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward)
        {
            return db.Seek(options, prefix, direction, (k, v) => (k, v));
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

        public bool Contains(byte[] key)
        {
            rwLock.EnterReadLock();
            try
            {
                return _dataCache.ContainsKey(key) || db.Contains(options, key);
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
                return _dataCache.TryGetValue(key, out byte[] value) ? value : db.Get(options, key);
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }
    }
}


