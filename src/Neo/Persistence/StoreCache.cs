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
using Serilog;
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
        private readonly ILogger _log = Log.ForContext<StoreCache>();
        private readonly IReadOnlyStore<byte[], byte[]> _store;
        private readonly IStoreSnapshot? _snapshot;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreCache"/> class.
        /// </summary>
        /// <param name="store">An <see cref="IStore"/> to create a readonly cache.</param>
        /// <param name="readOnly">True if you don't want to track write changes</param>
        public StoreCache(IStore store, bool readOnly = true) : base(readOnly)
        {
            if (store is null) throw new ArgumentNullException(nameof(store));
            _log.Verbose("Creating StoreCache (ReadOnly={ReadOnly}) from IStore ({StoreType})", readOnly, store.GetType().Name);
            _store = store;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreCache"/> class.
        /// </summary>
        /// <param name="snapshot">An <see cref="IStoreSnapshot"/> to create a snapshot cache.</param>
        public StoreCache(IStoreSnapshot snapshot) : base(false)
        {
            if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));
            _log.Verbose("Creating StoreCache (Writable) from IStoreSnapshot ({SnapshotType})", snapshot.GetType().Name);
            _store = snapshot;
            _snapshot = snapshot;
        }

        #region IStoreSnapshot

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            _log.Verbose("[SnapshotCache] Updating key {KeyId}:{KeyHex}", key.Id, key.Key.Span.ToHexString());
            _snapshot?.Put(key.ToArray(), value.ToArray());
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            _log.Verbose("[SnapshotCache] Adding key {KeyId}:{KeyHex}", key.Id, key.Key.Span.ToHexString());
            _snapshot?.Put(key.ToArray(), value.ToArray());
        }

        protected override void DeleteInternal(StorageKey key)
        {
            _log.Verbose("[SnapshotCache] Deleting key {KeyId}:{KeyHex}", key.Id, key.Key.Span.ToHexString());
            _snapshot?.Delete(key.ToArray());
        }

        public override void Commit()
        {
            _log.Debug("Committing StoreCache (Base commit first)");
            base.Commit();
            _log.Information("Committing underlying IStoreSnapshot (if present)");
            _snapshot?.Commit();
            _log.Debug("StoreCache commit finished");
        }

        public void Dispose()
        {
            _log.Debug("Disposing StoreCache");
            _snapshot?.Dispose();
            _log.Debug("StoreCache disposed");
        }

        #endregion

        #region IReadOnlyStore

        protected override bool ContainsInternal(StorageKey key)
        {
            _log.Verbose("ContainsInternal check for key {KeyId}:{KeyHex}", key.Id, key.Key.Span.ToHexString());
            return _store.Contains(key.ToArray());
        }

        /// <inheritdoc/>
        protected override StorageItem GetInternal(StorageKey key)
        {
            _log.Verbose("GetInternal for key {KeyId}:{KeyHex}", key.Id, key.Key.Span.ToHexString());
            byte[] keyBytes = key.ToArray();
            if (_store.TryGet(keyBytes, out var value))
                return new(value);
            _log.Warning("GetInternal failed for key {KeyId}:{KeyHex} - Key not found", key.Id, key.Key.Span.ToHexString());
            throw new KeyNotFoundException($"Key {key.Key.Span.ToHexString()} not found");
        }

        protected override IEnumerable<(StorageKey, StorageItem)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction)
        {
            _log.Verbose("SeekInternal with prefix {PrefixHex}, Direction: {Direction}", keyOrPrefix != null ? keyOrPrefix.ToHexString() : "<null>", direction);
            return _store.Find(keyOrPrefix, direction).Select(p => (new StorageKey(p.Key), new StorageItem(p.Value)));
        }

        /// <inheritdoc/>
        protected override StorageItem? TryGetInternal(StorageKey key)
        {
            _log.Verbose("TryGetInternal for key {KeyId}:{KeyHex}", key.Id, key.Key.Span.ToHexString());
            return _store.TryGet(key.ToArray(), out var value) ? new(value) : null;
        }

        #endregion
    }
}

#nullable disable
