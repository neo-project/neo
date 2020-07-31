using Neo.IO.Data.LevelDB;
using Neo.Trie;
using Neo.Trie.MPT;
using Neo.Ledger;

namespace Neo.Persistence.LevelDB
{
    public class DbTrieStore : IKVStore
    {
        private readonly DB db;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly byte prefix;

        private readonly byte[] ROOT_KEY = Prefixes.ROOT_KEY;

        public DbTrieStore(DB db, ReadOptions options, WriteBatch batch, byte prefix)
        {
            this.db = db;
            this.options = options ?? ReadOptions.Default;
            this.batch = batch;
            this.prefix = prefix;
        }

        private byte[] StoreKey(byte[] key)
        {
            return new byte[] { prefix }.Concat(key);
        }

        public byte[] Get(byte[] key)
        {
            var result = db.TryGet(options, StoreKey(key), out Slice value);
            return result ? value.ToArray() : null;
        }

        public void Put(byte[] key, byte[] value)
        {
            batch.Put(StoreKey(key), value);
        }

        public UInt256 GetRoot()
        {
            var result = db.TryGet(options, StoreKey(ROOT_KEY), out Slice value);
            return result ? new UInt256(value.ToArray()) : null;
        }

        public void PutRoot(UInt256 root)
        {
            if (root is null) return;
            batch.Put(StoreKey(ROOT_KEY), root.ToArray());
        }
    }
}
