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
using System.Runtime.CompilerServices;
using LSnapshot = Neo.IO.Data.LevelDB.Snapshot;

namespace Neo.Plugins.Storage
{
    internal class Snapshot : ISnapshot
    {
        private readonly DB db;
        private readonly LSnapshot snapshot;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly int _threadId;

        public Snapshot(DB db)
        {
            this.db = db;
            snapshot = db.GetSnapshot();
            options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            batch = new WriteBatch();
            _threadId = Environment.CurrentManagedThreadId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureThreadAccess()
        {
            if (_threadId != Environment.CurrentManagedThreadId)
            {
                throw new InvalidOperationException("Snapshot cannot be accessed from multiple threads.");
            }
        }

        public void Commit()
        {
            EnsureThreadAccess();
            db.Write(WriteOptions.Default, batch);
        }

        public void Delete(byte[] key)
        {
            EnsureThreadAccess();
            batch.Delete(key);
        }

        public void Dispose()
        {
            EnsureThreadAccess();
            snapshot.Dispose();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] prefix, SeekDirection direction = SeekDirection.Forward)
        {
            EnsureThreadAccess();
            return db.Seek(options, prefix, direction, (k, v) => (k, v));
        }

        public void Put(byte[] key, byte[] value)
        {
            EnsureThreadAccess();
            batch.Put(key, value);
        }

        public bool Contains(byte[] key)
        {
            EnsureThreadAccess();
            return db.Contains(options, key);
        }

        public byte[] TryGet(byte[] key)
        {
            EnsureThreadAccess();
            return db.Get(options, key);
        }
    }
}
