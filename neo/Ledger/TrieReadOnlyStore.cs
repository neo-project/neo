using Neo.Persistence;
using Neo.Persistence.LevelDB;
using Neo.Trie;

namespace Neo.Ledger
{
    public class TrieReadOnlyStore : IKVStore
    {
        private readonly Store store;
        private readonly byte prefix;

        private static readonly byte[] ROOT_KEY = Prefixes.ROOT_KEY;

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

        public void Put(byte[] key, byte[] value) { }

        public void Delete(byte[] key) { }
    }
}
