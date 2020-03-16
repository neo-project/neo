using Neo.IO.Data.LevelDB;
using Neo.Trie;
using Neo.Trie.MPT;
using System;
using System.Text;

namespace Neo.Persistence.LevelDB
{
    public class DbTrieStore : IKVStore
    {
        private readonly DB db;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly byte prefix;

        private readonly byte[] ROOT_KEY = Encoding.ASCII.GetBytes("CURRENT_ROOT");

        public DbTrieStore(DB db, ReadOptions options, WriteBatch batch, byte prefix)
        {
            this.db = db;
            this.options = options;
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

        public byte[] GetRoot()
        {
            var result = db.TryGet(options, StoreKey(ROOT_KEY), out Slice value);
            return result ? value.ToArray() : null;
        }

        public void PutRoot(byte[] root)
        {
            if (root is null || root.Length == 0 ) return;
            batch.Put(StoreKey(ROOT_KEY), root);
        }       
    }
}
