using AntShares.Core;
using System.Collections.Generic;

namespace AntShares.IO.Caching
{
    internal class BlockCache : ConcurrentCache<UInt256, Block>
    {
        public override Block this[UInt256 hash]
        {
            get
            {
                lock (SyncRoot)
                {
                    if (!InnerDictionary.ContainsKey(hash)) throw new KeyNotFoundException();
                    CacheItem<UInt256, Block> item = InnerDictionary[hash];
                    item.Update();
                    return item.Value;
                }
            }
        }

        public BlockCache(int max_capacity)
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

        protected override UInt256 GetKeyForItem(Block block)
        {
            return block.Hash;
        }
    }
}
