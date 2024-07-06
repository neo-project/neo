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
using System;
using System.Collections.Generic;
using LSnapshot = Neo.IO.Data.LevelDB.Snapshot;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly DB db;
        private readonly LSnapshot snapshot;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private bool isCommitted = false;

        public Snapshot(DB db)
        {
            this.db = db;
            snapshot = db.GetSnapshot();
            options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            batch = new WriteBatch();
        }

        public void Commit()
        {
            db.Write(WriteOptions.Default, batch);
            isCommitted = true;
        }

        public void Delete(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");

            batch.Delete(key);
        }

        public void Dispose()
        {
            snapshot.Dispose();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");
            return db.Seek(options, prefix, direction, (k, v) => (k, v));
        }

        public void Put(byte[] key, byte[] value)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");
            batch.Put(key, value);
        }

        public bool Contains(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");
            return db.Contains(options, key);
        }

        public byte[] TryGet(byte[] key)
        {
            if (isCommitted) throw new InvalidOperationException("Can not read/write a committed snapshot.");
            return db.Get(options, key);
        }
    }
}
