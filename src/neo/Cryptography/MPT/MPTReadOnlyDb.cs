using Neo.IO;
using Neo.Persistence;

namespace Neo.Cryptography.MPT
{
    public class MPTReadOnlyDb
    {
        private readonly IReadOnlyStore store;
        protected byte prefix;

        public MPTReadOnlyDb(IReadOnlyStore store, byte prefix)
        {
            this.store = store;
            this.prefix = prefix;
        }

        public MPTNode Node(UInt256 hash)
        {
            if (hash is null) return null;
            var data = store.TryGet(prefix, hash.ToArray());
            return MPTNode.Decode(data);
        }
    }
}
