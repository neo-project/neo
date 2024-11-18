// Copyright (C) 2015-2024 The Neo Project.
//
// Store.cs file belongs to the neo project and is free
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

namespace Neo.Plugins.Storage
{
    internal class Store : IStore
    {
        private readonly DB _db;

        public Store(string path)
        {
            _db = DB.Open(path, new Options { CreateIfMissing = true, FilterPolicy = Native.leveldb_filterpolicy_create_bloom(15) });
        }

        public void Delete(byte[] key)
        {
            _db.Delete(WriteOptions.Default, key);
        }

        public void Dispose() => _db.Dispose();

        public ISnapshot GetSnapshot() =>
            new Snapshot(_db);

        public void Put(byte[] key, byte[] value) =>
            _db.Put(WriteOptions.Default, key, value);

        public void PutSync(byte[] key, byte[] value) =>
            _db.Put(WriteOptions.SyncWrite, key, value);

        public bool Contains(byte[] key) =>
            _db.Contains(ReadOptions.Default, key);

        public byte[] TryGet(byte[] key) =>
            _db.Get(ReadOptions.Default, key);

        public bool TryGet(byte[] key, out byte[] value)
        {
            value = _db.Get(ReadOptions.Default, key);
            return value != null;
        }

        public IEnumerable<(byte[], byte[])> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward) =>
            _db.Seek(ReadOptions.Default, prefix, direction, (k, v) => (k, v));
    }
}
