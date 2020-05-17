using Neo.IO;
using Neo.Persistence;
using System;

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
                throw new InvalidOperationException("Means nothing to store HashNode");
            store.Put(Prefix, node.Hash.ToArray(), node.Encode());
        }
    }
}
