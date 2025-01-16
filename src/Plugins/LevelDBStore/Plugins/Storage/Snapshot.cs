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

using Neo.IO.Storage.LevelDB;
using Neo.Persistence;
using System.Collections;
using System.Collections.Generic;
using LSnapshot = Neo.IO.Storage.LevelDB.Snapshot;

namespace Neo.Plugins.Storage
{
    /// <summary>
    /// <code>Iterating over the whole dataset can be time-consuming. Depending upon how large the dataset is.</code>
    /// </summary>
    internal class Snapshot : ISnapshot, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        private readonly DB _db;
        private readonly LSnapshot _snapshot;
        private readonly ReadOptions _readOptions;
        private readonly WriteBatch _batch;
        private readonly object _lock = new();

        public Snapshot(DB db)
        {
            _db = db;
            _snapshot = db.CreateSnapshot();
            _readOptions = new ReadOptions { FillCache = false, Snapshot = _snapshot };
            _batch = new WriteBatch();
        }

        public void Commit()
        {
            lock (_lock)
                _db.Write(WriteOptions.Default, _batch);
        }

        public void Delete(byte[] key)
        {
            lock (_lock)
                _batch.Delete(key);
        }

        public void Dispose()
        {
            _snapshot.Dispose();
            _readOptions.Dispose();
        }

        /// <inheritdoc/>
        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            return _db.Seek(_readOptions, keyOrPrefix, direction);
        }

        public void Put(byte[] key, byte[] value)
        {
            lock (_lock)
                _batch.Put(key, value);
        }

        public bool Contains(byte[] key)
        {
            return _db.Contains(_readOptions, key);
        }

        public byte[] TryGet(byte[] key)
        {
            return _db.Get(_readOptions, key);
        }

        public bool TryGet(byte[] key, out byte[] value)
        {
            value = _db.Get(_readOptions, key);
            return value != null;
        }

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator()
        {
            using var iterator = _db.CreateIterator(_readOptions);
            for (iterator.SeekToFirst(); iterator.Valid(); iterator.Next())
                yield return new KeyValuePair<byte[], byte[]>(iterator.Key(), iterator.Value());
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
