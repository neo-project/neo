using AntShares.Core;
using System.Collections.Generic;

namespace AntShares.IO.Caching
{
    internal class BlockCache : Cache<UInt256, Block>
    {
        public override Block this[UInt256 hash]
        {
            get
            {
                if (!Contains(hash)) throw new KeyNotFoundException();
                return base[hash];
            }
        }

        public BlockCache(int max_capacity)
            : base(max_capacity)
        {
        }

        public override bool Contains(UInt256 hash)
        {
            if (!base.Contains(hash)) return false;
            InnerDictionary[hash].Update();
            return true;
        }

        protected override UInt256 GetKeyForItem(Block block)
        {
            return block.Hash;
        }
    }
}
