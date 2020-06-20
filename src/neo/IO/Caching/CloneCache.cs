using System;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    internal class CloneCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly DataCache<TKey, TValue> innerCache;

        public CloneCache(DataCache<TKey, TValue> innerCache)
        {
            this.innerCache = innerCache;
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            innerCache.Add(key, value);
        }

        protected override void DeleteInternal(TKey key)
        {
            innerCache.Delete(key);
        }

        protected override IEnumerable<(TKey, TValue)> SeekInternal(byte[] key)
        {
            foreach (var (ikey, ivalue) in innerCache.Seek(key))
                yield return (ikey, ivalue.Clone());
        }

        protected override TValue GetInternal(TKey key)
        {
            return innerCache[key].Clone();
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return innerCache.TryGet(key)?.Clone();
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            innerCache.GetAndChange(key).FromReplica(value);
        }
    }
}
