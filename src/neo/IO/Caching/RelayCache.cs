using Neo.Models;
using Neo.Network.P2P.Payloads;

namespace Neo.IO.Caching
{
    internal class RelayCache : FIFOCache<UInt256, IVerifiable>
    {
        public RelayCache(int max_capacity)
            : base(max_capacity)
        {
        }

        protected override UInt256 GetKeyForItem(IVerifiable item)
        {
            return item.Hash;
        }
    }
}
