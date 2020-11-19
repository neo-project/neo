using Neo.IO.Caching;
using Neo.Persistence;

namespace Neo.Cryptography.MPT
{
    public class MPTCache
    {
        private readonly DataCache<UInt256, MPTNode> cache;

        public MPTCache(ISnapshot store, byte prefix)
        {
            cache = new StoreDataCache<UInt256, MPTNode>(store, prefix);
        }

        public MPTNode Resolve(UInt256 hash)
        {
            return cache.TryGet(hash)?.Clone();
        }

        public void PutNode(MPTNode np)
        {
            var n = Resolve(np.Hash);
            if (n is null)
            {
                np.Reference = 1;
                cache.Add(np.Hash, np.Clone());
                return;
            }
            cache.GetAndChange(np.Hash).Reference++;
        }

        public void DeleteNode(UInt256 hash)
        {
            var n = Resolve(hash);
            if (n is null) return;
            if (1 < n.Reference)
            {
                cache.GetAndChange(hash).Reference--;
                return;
            }
            cache.Delete(hash);
        }

        public void Commit()
        {
            cache.Commit();
        }
    }
}
