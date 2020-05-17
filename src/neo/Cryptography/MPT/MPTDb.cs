using Neo.IO;
using Neo.Persistence;

namespace Neo.Cryptography.MPT
{
    public class MPTDb : MPTReadOnlyDb
    {
        private readonly ISnapshot store;

        public MPTDb(ISnapshot store, byte prefix) : base(store, prefix)
        {
            this.store = store;
        }

        public void Put(MPTNode node)
        {
            if (node is HashNode)
                throw new System.InvalidOperationException("Means nothing to store HashNode");
            store.Put(prefix, node.Hash.ToArray(), node.Encode());
        }
    }
}
