using Neo.IO;
using Neo.Persistence;
using System;

namespace Neo.Cryptography.MPT
{
    public class MPTDb
    {
        internal const byte Prefix = 0xf0;

        private readonly ISnapshot store;

        public MPTDb(ISnapshot store)
        {
            this.store = store;
        }

        public MPTNode Node(UInt256 hash)
        {
            if (hash is null) return null;
            var data = store.TryGet(Prefix, hash.ToArray());
            return MPTNode.Decode(data);
        }

        public void Put(MPTNode node)
        {
            if (node is HashNode)
                throw new InvalidOperationException("Means nothing to store HashNode");
            store.Put(Prefix, node.Hash.ToArray(), node.Encode());
        }
    }
}
