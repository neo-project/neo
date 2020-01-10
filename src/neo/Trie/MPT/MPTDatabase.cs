
using Neo.Persistence;

namespace Neo.Trie.MPT
{
    public class MPTDatabase: Database
    {
        private IStore store;

        private byte TABLE = 0x54;

        private byte[] GetStoreKey(byte[] hash)
        {
            return hash;
        }

        public MPTDatabase(IStore store)
        {
            this.store = store;
        }

        public MPTNode Node(byte[] hash)
        {
            var data = store.TryGet(TABLE, GetStoreKey(hash));
            var n = MPTNode.Decode(data);
            return n;
        }
    }
}