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

using Neo.IO.Storage.LevelDB;
using Neo.Persistence;
using System.Collections;
using System.Collections.Generic;

namespace Neo.Plugins.Storage
{
    internal class Store : IStore, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        private readonly DB _db;

        public Store(string dir)
        {
            _db = new(dir, new()
            {
                CreateIfMissing = true,
                // Keep whole blockchain open plus future files
                // at lease up to block index 10_000_000
                MaxOpenFiles = 4096,
                FilterPolicy = 10,
                CompressionLevel = CompressionType.SnappyCompression,
            });
        }

        public Store(string dir, Options options)
        {
            _db = new DB(dir, options);
        }

        public void Dispose() =>
            _db.Dispose();

        public ISnapshot GetSnapshot() =>
            new Snapshot(_db);

        public void Delete(byte[] key) =>
            _db.Delete(key, WriteOptions.Default);

        public void Put(byte[] key, byte[] value) =>
            _db.Put(key, value, WriteOptions.Default);

        public void PutSync(byte[] key, byte[] value) =>
            _db.Put(key, value, WriteOptions.SyncWrite);

        public bool Contains(byte[] key) =>
            _db.Contains(key, ReadOptions.Default);

        public byte[] TryGet(byte[] key) =>
            _db.Get(key, ReadOptions.Default);

        public bool TryGet(byte[] key, out byte[] value)
        {
            value = _db.Get(key, ReadOptions.Default);
            return value != null;
        }

        public IEnumerable<(byte[], byte[])> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward) =>
            direction == SeekDirection.Forward
                ? _db.Seek(prefix, ReadOptions.Default)
                : _db.SeekPrev(prefix, ReadOptions.Default);

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator() =>
            _db.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
