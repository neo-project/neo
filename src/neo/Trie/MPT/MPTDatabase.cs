
using Neo.Persistence;

namespace Neo.Trie.MPT
{
    public class MPTDatabase
    {
        private IStore store;

        public static readonly byte TABLE = 0x4D;

        private byte[] StoreKey(byte[] hash)
        {
            return hash;
        }

        public MPTDatabase(IStore store)
        {
            this.store = store;
        }

        public MPTNode Node(byte[] hash)
        {
            var data = store.TryGet(TABLE, StoreKey(hash));
            var n = MPTNode.Decode(data);
            return n;
        }

        public void Delete(byte[] hash)
        {
            store.Delete(TABLE, StoreKey(hash));
        }

        public void Put(MPTNode node)
        {
            store.Put(TABLE, StoreKey(node.GetHash()), node.Encode());
        }
    }
}