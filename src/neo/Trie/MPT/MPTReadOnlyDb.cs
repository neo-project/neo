
namespace Neo.Trie.MPT
{
    public class MPTReadOnlyDb
    {
        private IKVReadOnlyStore store;

        public MPTReadOnlyDb(IKVReadOnlyStore store)
        {
            this.store = store;
        }

        public MPTNode Node(byte[] hash)
        {
            var data = store.Get(hash);
            return MPTNode.Decode(data);
        }
    }
}
