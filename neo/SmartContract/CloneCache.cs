using Neo.Core;
using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract
{
    internal class CloneCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private DataCache<TKey, TValue> innerCache;

        public CloneCache(DataCache<TKey, TValue> innerCache)
        {
            this.innerCache = innerCache;
        }

        public void Commit()
        {
            foreach (Trackable trackable in GetChangeSet())
                switch (trackable.State)
                {
                    case TrackState.Added:
                        innerCache.Add(trackable.Key, trackable.Item);
                        break;
                    case TrackState.Changed:
                        innerCache.GetAndChange(trackable.Key).FromReplica(trackable.Item);
                        break;
                    case TrackState.Deleted:
                        innerCache.Delete(trackable.Key);
                        break;
                }
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            foreach (KeyValuePair<TKey, TValue> pair in innerCache.Find(key_prefix))
                yield return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Clone());
        }

        protected override TValue GetInternal(TKey key)
        {
            return innerCache[key].Clone();
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return innerCache.TryGet(key)?.Clone();
        }
    }
}
