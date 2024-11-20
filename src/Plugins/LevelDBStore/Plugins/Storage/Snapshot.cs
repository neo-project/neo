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

using Neo.IO.Data.LevelDB;
using Neo.Persistence;
using System.Collections.Generic;
using LSnapshot = Neo.IO.Data.LevelDB.Snapshot;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly DB _db;
        private readonly LSnapshot _snapshot;
        private readonly ReadOptions _options;
        private readonly WriteBatch _batch;

        public Snapshot(DB db)
        {
            _db = db;
            _snapshot = db.GetSnapshot();
            _options = new ReadOptions { FillCache = false, Snapshot = _snapshot };
            _batch = new WriteBatch();
        }

        public void Commit()
        {
            _db.Write(WriteOptions.Default, _batch);
        }

        public void Delete(byte[] key)
        {
            _batch.Delete(key);
        }

        public void Dispose()
        {
            _snapshot.Dispose();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward)
        {
            return _db.Seek(_options, prefix, direction, (k, v) => (k, v));
        }

        public void Put(byte[] key, byte[] value)
        {
            _batch.Put(key, value);
        }

        public bool Contains(byte[] key)
        {
            return _db.Contains(_options, key);
        }

        public byte[] TryGet(byte[] key)
        {
            return _db.Get(_options, key);
        }

        public bool TryGet(byte[] key, out byte[] value)
        {
            value = _db.Get(_options, key);
            return value != null;
        }
    }
}
