using Neo.IO;
using Neo.Persistence;
using System;

namespace Neo.Cryptography.MPT
{
    public class MPTDb
    {
        private readonly ISnapshot Store;
        private readonly byte Prefix;

        public MPTDb(ISnapshot store, byte prefix)
        {
            this.Store = store;
            this.Prefix = prefix;
        }

        public MPTNode Node(UInt256 hash)
        {
            if (hash is null) return null;
            var data = Store.TryGet(Prefix, hash.ToArray());
            return MPTNode.Decode(data);
        }

        public void Put(MPTNode node)
        {
            if (node is HashNode)
                throw new InvalidOperationException("Means nothing to store HashNode");
            Store.Put(Prefix, node.Hash.ToArray(), node.Encode());
        }
    }
}
