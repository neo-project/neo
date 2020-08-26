using Neo.IO;

namespace Neo.Trie.MPT
{
    public class MPTDb
    {
        private IKVStore store;

        public MPTDb(IKVStore store)
        {
            this.store = store;
        }

        public void Put(MPTNode node)
        {
            if (node is HashNode hn)
            {
                throw new System.InvalidOperationException("Means nothing to store HashNode");
            }
            store.Put(node.GetHash().ToArray(), node.ToArray());
        }

        public MPTNode Node(UInt256 hash)
        {
            if (hash is null) return null;
            var data = store.Get(hash.ToArray());
            return MPTNode.DeserializeFromByteArray(data);
        }
    }
}
