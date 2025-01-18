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

using Neo.IO.Storage.LevelDB;
using Neo.Persistence;
using System.Collections;
using System.Collections.Generic;

namespace Neo.Plugins.Storage
{
    /// <summary>
    /// <code>Iterating over the whole dataset can be time-consuming. Depending upon how large the dataset is.</code>
    /// </summary>
    internal class Store : IStore, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        private readonly DB _db;
        private readonly Options _options;

        public Store(string path)
        {
            _options = new Options
            {
                CreateIfMissing = true,
                FilterPolicy = Native.leveldb_filterpolicy_create_bloom(15),
                CompressionLevel = CompressionType.SnappyCompression,
            };
            _db = DB.Open(path, _options);
        }

        public void Delete(byte[] key)
        {
            _db.Delete(WriteOptions.Default, key);
        }

        public void Dispose()
        {
            _db.Dispose();
            _options.Dispose();
        }

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

        /// <inheritdoc/>
        public IEnumerable<(byte[], byte[])> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward) =>
            _db.Seek(ReadOptions.Default, keyOrPrefix, direction);

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator() =>
            _db.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
