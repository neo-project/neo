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

using Neo.Persistence;
using RocksDbSharp;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly RocksDb db;
        private readonly RocksDbSharp.Snapshot snapshot;
        private readonly WriteBatch batch;
        private readonly ReadOptions options;
        private bool isCommitted = false;

        public Snapshot(RocksDb db)
        {
            this.db = db;
            snapshot = db.CreateSnapshot();
            batch = new WriteBatch();

            options = new ReadOptions();
            options.SetFillCache(false);
            options.SetSnapshot(snapshot);
        }

        public void Commit()
        {
            db.Write(batch, Options.WriteDefault);
            isCommitted = true;
        }

        public void Delete(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            batch.Delete(key);
        }

        public void Put(byte[] key, byte[] value)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            batch.Put(key, value);
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] keyOrPrefix, SeekDirection direction)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            if (keyOrPrefix == null) keyOrPrefix = Array.Empty<byte>();

            using var it = db.NewIterator(readOptions: options);

            if (direction == SeekDirection.Forward)
                for (it.Seek(keyOrPrefix); it.Valid(); it.Next())
                    yield return (it.Key(), it.Value());
            else
                for (it.SeekForPrev(keyOrPrefix); it.Valid(); it.Prev())
                    yield return (it.Key(), it.Value());
        }

        public bool Contains(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            return db.Get(key, Array.Empty<byte>(), 0, 0, readOptions: options) >= 0;
        }

        public byte[] TryGet(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            return db.Get(key, readOptions: options);
        }

        public void Dispose()
        {
            snapshot.Dispose();
            batch.Dispose();
        }
    }
}
