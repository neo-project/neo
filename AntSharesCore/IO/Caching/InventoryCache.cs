using AntShares.Network;
using System.Collections.Generic;

namespace AntShares.IO.Caching
{
    public class InventoryCache<T> : ConcurrentCache<UInt256, T> where T : Inventory
    {
        public override T this[UInt256 hash]
        {
            get
            {
                lock (SyncRoot)
                {
                    if (!InnerDictionary.ContainsKey(hash)) throw new KeyNotFoundException();
                    CacheItem item = InnerDictionary[hash];
                    item.Update();
                    return item.Value;
                }
            }
        }

        public InventoryCache(int max_capacity)
            : base(max_capacity)
        {
        }

        public override bool Contains(UInt256 hash)
        {
            lock (SyncRoot)
            {
                if (!InnerDictionary.ContainsKey(hash)) return false;
                InnerDictionary[hash].Update();
                return true;
            }
        }

        protected override UInt256 GetKeyForItem(T item)
        {
            return item.Hash;
        }

        public override bool TryGet(UInt256 hash, out T item)
        {
            lock (SyncRoot)
            {
                if (InnerDictionary.ContainsKey(hash))
                {
                    InnerDictionary[hash].Update();
                    item = InnerDictionary[hash].Value;
                    return true;
                }
            }
            item = null;
            return false;
        }
    }
}
