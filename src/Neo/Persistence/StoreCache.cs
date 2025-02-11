// Copyright (C) 2015-2025 The Neo Project.
//
// StoreCache.cs file belongs to the neo project and is free
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
    public class StoreCache : DataCache, IDisposable
    {
        private readonly IRawReadOnlyStore _store;
        private readonly IStoreSnapshot? _snapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreCache"/> class.
        /// </summary>
        /// <param name="store">An <see cref="IStore"/> to create a readonly cache.</param>
        public StoreCache(IStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreCache"/> class.
        /// </summary>
        /// <param name="snapshot">An <see cref="IStoreSnapshot"/> to create a snapshot cache.</param>
        public StoreCache(IStoreSnapshot snapshot)
        {
            _store = snapshot;
            _snapshot = snapshot;
        }

        #region IStoreSnapshot

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            _snapshot?.Put(key.ToArray(), value.ToArray());
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

        public void Dispose()
        {
            _snapshot?.Dispose();
        }

        #endregion

        #region IReadOnlyStore

        protected override bool ContainsInternal(StorageKey key)
        {
            return _store.Contains(key.ToArray());
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

        #endregion
    }
}
