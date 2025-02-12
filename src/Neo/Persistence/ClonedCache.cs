// Copyright (C) 2015-2025 The Neo Project.
//
// ClonedCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.SmartContract;
using System.Collections.Generic;

namespace Neo.Persistence
{
    class ClonedCache : DataCache
    {
        private readonly DataCache _innerCache;

        public ClonedCache(DataCache innerCache)
        {
            _innerCache = innerCache;
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            _innerCache.Add(key, value.Clone());
        }

        protected override void DeleteInternal(StorageKey key)
        {
            _innerCache.Delete(key);
        }

        protected override bool ContainsInternal(StorageKey key)
        {
            return _innerCache.Contains(key);
        }

        /// <inheritdoc/>
        protected override StorageItem GetInternal(StorageKey key)
        {
            return _innerCache[key].Clone();
        }

        protected override IEnumerable<(StorageKey, StorageItem)> SeekInternal(byte[] keyOrPreifx, SeekDirection direction)
        {
            foreach (var (key, value) in _innerCache.Seek(keyOrPreifx, direction))
                yield return (key, value.Clone());
        }

        protected override StorageItem? TryGetInternal(StorageKey key)
        {
            if (_innerCache.TryGet(key, out var ret))
                return ret.Clone();

            return null;
        }

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            var entry = _innerCache.GetAndChange(key)
                ?? throw new KeyNotFoundException();

            entry.FromReplica(value);
        }
    }
}

#nullable disable
