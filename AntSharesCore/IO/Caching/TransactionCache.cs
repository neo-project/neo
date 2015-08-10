using AntShares.Core;
using System.Collections.Generic;

namespace AntShares.IO.Caching
{
    internal class TransactionCache : ConcurrentCache<UInt256, Transaction>
    {
        public override Transaction this[UInt256 hash]
        {
            get
            {
                lock (SyncRoot)
                {
                    if (!InnerDictionary.ContainsKey(hash)) throw new KeyNotFoundException();
                    CacheItem<UInt256, Transaction> item = InnerDictionary[hash];
                    item.Update();
                    return item.Value;
                }
            }
        }

        public TransactionCache(int max_capacity)
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

        protected override UInt256 GetKeyForItem(Transaction tx)
        {
            return tx.Hash;
        }
    }
}
