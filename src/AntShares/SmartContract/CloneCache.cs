using AntShares.Core;
using AntShares.IO;
using AntShares.IO.Caching;
using System;

namespace AntShares.SmartContract
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

        protected override TValue GetInternal(TKey key)
        {
            return innerCache[key].Clone();
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

        protected override TValue TryGetInternal(TKey key)
        {
            return innerCache.TryGet(key)?.Clone();
        }
    }
}
