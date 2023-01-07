// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    /// <summary>
    /// Represents a cache for the snapshot or database of the NEO blockchain.
    /// </summary>
    public class SnapshotCache : DataCache, IDisposable
    {
        private readonly IReadOnlyStore store;
        private readonly ISnapshot snapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotCache"/> class.
        /// </summary>
        /// <param name="store">An <see cref="IReadOnlyStore"/> to create a readonly cache; or an <see cref="ISnapshot"/> to create a snapshot cache.</param>
        public SnapshotCache(IReadOnlyStore store)
        {
            this.store = store;
            this.snapshot = store as ISnapshot;
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            snapshot?.Put(key.ToArray(), value.ToArray());
        }

        protected override void DeleteInternal(StorageKey key)
        {
            snapshot?.Delete(key.ToArray());
        }

        public override void Commit()
        {
            base.Commit();
            snapshot.Commit();
        }

        protected override bool ContainsInternal(StorageKey key)
        {
            return store.Contains(key.ToArray());
        }

        public void Dispose()
        {
            snapshot?.Dispose();
        }

        protected override StorageItem GetInternal(StorageKey key)
        {
            byte[] value = store.TryGet(key.ToArray());
            if (value == null) throw new KeyNotFoundException();
            return new(value);
        }

        protected override IEnumerable<(StorageKey, StorageItem)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction)
        {
            return store.Seek(keyOrPrefix, direction).Select(p => (new StorageKey(p.Key), new StorageItem(p.Value)));
        }

        protected override StorageItem TryGetInternal(StorageKey key)
        {
            byte[] value = store.TryGet(key.ToArray());
            if (value == null) return null;
            return new(value);
        }

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            snapshot?.Put(key.ToArray(), value.ToArray());
        }
    }
}
