// Copyright (C) 2015-2025 The Neo Project.
//
// SnapshotCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Extensions;
using Neo.SmartContract;
using System;
using System.Linq;

namespace Neo.Persistence
{
    /// <summary>
    /// Represents a cache for the snapshot or database of the NEO blockchain.
    /// </summary>
    public class SnapshotCache : SnapshotCacheReadOnly, IDisposable
    {
        private readonly ISnapshot _snapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotCache"/> class.
        /// </summary>
        /// <param name="snapshot">An <see cref="ISnapshot"/> to create a write cache.</param>
        public SnapshotCache(ISnapshot snapshot) : base(snapshot)
        {
            _snapshot = snapshot;
        }

        #region Write

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            _snapshot.Put(key.ToArray(), value.ToArray());
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            _snapshot.Put(key.ToArray(), value.ToArray());
        }

        protected override void DeleteInternal(StorageKey key)
        {
            _snapshot.Delete(key.ToArray());
        }

        public override void Commit()
        {
            base.Commit();
            _snapshot.Commit();
        }

        #endregion

        public void Dispose()
        {
            _snapshot.Dispose();
        }
    }
}
