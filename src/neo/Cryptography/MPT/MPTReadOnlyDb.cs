using Neo.IO;
using Neo.Persistence;

namespace Neo.Cryptography.MPT
{
    public class MPTReadOnlyDb
    {
        private readonly IReadOnlyStore store;
        protected readonly byte Prefix;

        public MPTReadOnlyDb(IReadOnlyStore store, byte prefix)
        {
            this.store = store;
            this.Prefix = prefix;
        }

        public MPTNode Node(UInt256 hash)
        {
            if (hash is null) return null;
            var data = store.TryGet(Prefix, hash.ToArray());
            return MPTNode.Decode(data);
        }
    }
}
