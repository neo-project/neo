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

using Neo.Persistence;
using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.Plugins.Storage
{
    internal class Store : IStore
    {
        private readonly RocksDb db;

        public Store(string path)
        {
            db = RocksDb.Open(Options.Default, Path.GetFullPath(path));
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public ISnapshot GetSnapshot()
        {
            return new Snapshot(db);
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            if (keyOrPrefix == null) keyOrPrefix = [];

            using var it = db.NewIterator();
            if (direction == SeekDirection.Forward)
                for (it.Seek(keyOrPrefix); it.Valid(); it.Next())
                    yield return (it.Key(), it.Value());
            else
                for (it.SeekForPrev(keyOrPrefix); it.Valid(); it.Prev())
                    yield return (it.Key(), it.Value());
        }

        public bool Contains(byte[] key)
        {
            return db.Get(key, [], 0, 0) >= 0;
        }

        public byte[] TryGet(byte[] key)
        {
            return db.Get(key);
        }

        public void Delete(byte[] key)
        {
            db.Remove(key);
        }

        public void Put(byte[] key, byte[] value)
        {
            db.Put(key, value);
        }

        public void PutSync(byte[] key, byte[] value)
        {
            db.Put(key, value, writeOptions: Options.WriteDefaultSync);
        }
    }
}
