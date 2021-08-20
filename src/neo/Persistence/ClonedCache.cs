using Neo.SmartContract;
using System.Collections.Generic;

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
