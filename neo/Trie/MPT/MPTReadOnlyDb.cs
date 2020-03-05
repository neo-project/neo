using System.Text;

namespace Neo.Trie.MPT
{
    public class MPTReadOnlyDb
    {
        private IKVReadOnlyStore store;
        protected static readonly byte[] Prefix = Encoding.ASCII.GetBytes("MPT");

        public MPTReadOnlyDb(IKVReadOnlyStore store)
        {
            this.store = store;
        }

        protected byte[] StoreKey(byte[] hash)
        {
            return Prefix.Concat(hash);
        }

        public MPTNode Node(byte[] hash)
        {
            var data = store.Get(StoreKey(hash));
            var n = MPTNode.Decode(data);
            return n;
        }
    }
}
