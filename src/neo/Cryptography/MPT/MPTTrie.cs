using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;

namespace Neo.Cryptography.MPT
{
    public partial class MPTTrie<TKey, TValue> : MPTReadOnlyTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private readonly MPTDb db;

        public MPTTrie(UInt256 root, ISnapshot store, byte prefix) : base(root, store, prefix)
        {
            this.db = new MPTDb(store, prefix);
        }
    }
}
