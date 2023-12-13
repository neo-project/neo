// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using Neo.SmartContract;

namespace Neo.Persistence
{
    class ClonedCache : DataCache
    {
        private readonly DataCache innerCache;

        public ClonedCache(DataCache innerCache)
        {
            this.innerCache = innerCache;
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            innerCache.Add(key, value.Clone());
        }

        protected override void DeleteInternal(StorageKey key)
        {
            innerCache.Delete(key);
        }

        protected override bool ContainsInternal(StorageKey key)
        {
            return innerCache.Contains(key);
        }

        protected override StorageItem GetInternal(StorageKey key)
        {
            return innerCache[key].Clone();
        }

        protected override IEnumerable<(StorageKey, StorageItem)> SeekInternal(byte[] keyOrPreifx, SeekDirection direction)
        {
            foreach (var (key, value) in innerCache.Seek(keyOrPreifx, direction))
                yield return (key, value.Clone());
        }

        protected override StorageItem TryGetInternal(StorageKey key)
        {
            return innerCache.TryGet(key)?.Clone();
        }

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            innerCache.GetAndChange(key).FromReplica(value);
        }
    }
}
