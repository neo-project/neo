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
using System.Collections.Generic;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly DB _db;
        private readonly SnapShot _snapshot;
        private readonly WriteBatch _batch;
        private readonly ReadOptions _readOptions;

        private readonly object _lock = new();

        public Snapshot(DB db)
        {
            _db = db;
            _snapshot = db.CreateSnapshot();
            _batch = new();
            _readOptions = new ReadOptions { FillCache = false, Snapshot = _snapshot };
        }

        public void Commit()
        {
            lock (_lock)
                _db.Write(_batch);
        }

        public void Delete(byte[] key)
        {
            lock (_lock)
                _batch.Delete(key);
        }

        public void Dispose()
        {
            _snapshot.Dispose();
            _batch.Dispose();
            _readOptions.Dispose();
        }

        public void Put(byte[] key, byte[] value)
        {
            lock (_lock)
                _batch.Put(key, value);
        }

        public bool Contains(byte[] key) =>
            _db.Contains(key, _readOptions);

        public byte[] TryGet(byte[] key) =>
            _db.Get(key, _readOptions);

        public bool TryGet(byte[] key, out byte[] value)
        {
            value = _db.Get(key, _readOptions);
            return value != null;
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward) =>
            direction == SeekDirection.Forward
                ? _db.Seek(prefix, _readOptions)
                : _db.SeekPrev(prefix, _readOptions);
    }
}
