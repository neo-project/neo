using Neo.IO;
using Neo.Persistence;

namespace Neo.Trie.MPT
{
    public class MPTDb : MPTReadOnlyDb
    {
        private IStore store;

        public MPTDb(IStore store, byte prefix) : base(store, prefix)
        {
            this.store = store;
        }

        public void Put(MPTNode node)
        {
            if (node is HashNode hn)
                throw new System.InvalidOperationException("Means nothing to store HashNode");
            store.Put(prefix, node.GetHash().ToArray(), node.Encode());
        }
    }
}
