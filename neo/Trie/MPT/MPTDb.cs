
namespace Neo.Trie.MPT
{
    public class MPTDb : MPTReadOnlyDb
    {
        private IKVStore store;
        public MPTDb(IKVStore store) : base(store)
        {
            this.store = store;
        }

        public void Put(MPTNode node)
        {
            if (node is HashNode hn)
            {
                throw new System.InvalidOperationException("Means nothing to store HashNode");
            }
            store.Put(node.GetHash().ToArray(), node.Encode());
        }
    }
}
