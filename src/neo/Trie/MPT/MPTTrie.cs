using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;

namespace Neo.Trie.MPT
{
    public partial class MPTTrie<TKey, TValue> : MPTReadOnlyTrie<TKey, TValue>
        where TKey : notnull, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private MPTDb db;

        public MPTTrie(UInt256 root, ISnapshot store, byte prefix) : base(root, store, prefix)
        {
            this.db = new MPTDb(store, prefix);
        }

        public JObject ToJson()
        {
            return root.ToJson();
        }
    }
}
