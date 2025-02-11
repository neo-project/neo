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
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    /// <summary>
    /// Represents a cache for the snapshot or database of the NEO blockchain.
    /// </summary>
    public class SnapshotCache : DataCache, IDisposable
    {
        private readonly IReadOnlyStore _store;
        private readonly ISnapshot? _snapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotCache"/> class.
        /// </summary>
        /// <param name="store">An <see cref="IReadOnlyStore"/> to create a readonly cache; or an <see cref="ISnapshot"/> to create a snapshot cache.</param>
        public SnapshotCache(IReadOnlyStore store)
        {
            _store = store;
            _snapshot = store as ISnapshot;
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            _snapshot?.Put(key.ToArray(), value.ToArray());
        }

        protected override void DeleteInternal(StorageKey key)
        {
            _snapshot?.Delete(key.ToArray());
        }

        public override void Commit()
        {
            base.Commit();
            _snapshot?.Commit();
        }

        protected override bool ContainsInternal(StorageKey key)
        {
            return _store.Contains(key.ToArray());
        }

        public void Dispose()
        {
            _snapshot?.Dispose();
        }

        /// <inheritdoc/>
        protected override StorageItem GetInternal(StorageKey key)
        {
            if (_store.TryGet(key.ToArray(), out var value))
                return new(value);
            throw new KeyNotFoundException();
        }

        protected override IEnumerable<(StorageKey, StorageItem)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction)
        {
            return _store.Seek(keyOrPrefix, direction).Select(p => (new StorageKey(p.Key), new StorageItem(p.Value)));
        }

        /// <inheritdoc/>
        protected override StorageItem? TryGetInternal(StorageKey key)
        {
            return _store.TryGet(key.ToArray(), out var value) ? new(value) : null;
        }

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            _snapshot?.Put(key.ToArray(), value.ToArray());
        }
    }
}

#nullable disable
