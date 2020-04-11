using System.Text;

namespace Neo.Trie.MPT
{
    public class MPTReadOnlyDb
    {
        private IKVReadOnlyStore store;

        public MPTReadOnlyDb(IKVReadOnlyStore store)
        {
            this.store = store;
        }

        public MPTNode Node(UInt256 hash)
        {
            if (hash is null) return null;
            var data = store.Get(hash.ToArray());
            return MPTNode.Decode(data);
        }
    }
}
