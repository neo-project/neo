using AntShares.Network;

namespace AntShares.IO.Caching
{
    internal class RelayCache : FIFOCache<UInt256, Inventory>
    {
        public RelayCache(int max_capacity)
            : base(max_capacity)
        {
        }

        protected override UInt256 GetKeyForItem(Inventory item)
        {
            return item.Hash;
        }
    }
}
