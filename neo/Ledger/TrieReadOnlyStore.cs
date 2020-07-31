using Neo.IO.Data.LevelDB;
using Neo.Persistence;
using Neo.Persistence.LevelDB;
using Neo.Trie;
using Neo.Trie.MPT;
using System.Text;

namespace Neo.Ledger
{
    public class TrieReadOnlyStore : IKVReadOnlyStore
    {
        private readonly Store store;
        private readonly byte prefix;

        private readonly byte[] ROOT_KEY = Prefixes.ROOT_KEY;

        public TrieReadOnlyStore(Store store, byte prefix)
        {
            this.store = store;
            this.prefix = prefix;
        }

        public byte[] Get(byte[] key)
        {
            return store.Get(prefix, key);
        }

        public UInt256 GetRoot()
        {
            var result = Get(ROOT_KEY);
            return result is null ? UInt256.Zero : new UInt256(result);
        }
    }
}
