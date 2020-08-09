using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Data.LevelDB;
using Neo.Trie.MPT;
using System;
using System.Collections.Generic;

namespace Neo.Persistence.LevelDB
{
    public class DbCacheWithTrie<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly DB db;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly byte prefix;
        private MPTTrie mptTrie;
        private DbTrieStore trieDb;

        public DbCacheWithTrie(DB db, ReadOptions options, WriteBatch batch, byte prefix)
        {
            this.db = db;
            this.options = options ?? ReadOptions.Default;
            this.batch = batch;
            this.prefix = prefix;
            this.trieDb = new DbTrieStore(db, options, batch, Prefixes.DATA_MPT);
            this.mptTrie = new MPTTrie(trieDb.GetRoot(), trieDb);
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            batch?.Put(prefix, key, value);
            mptTrie.Put(key.ToArray(), value.ToArray());
        }

        public override void DeleteInternal(TKey key)
        {
            batch?.Delete(prefix, key);
            mptTrie.TryDelete(key.ToArray());
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return db.Find(options, SliceBuilder.Begin(prefix).Add(key_prefix), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(1), v.ToArray().AsSerializable<TValue>()));
        }

        protected override TValue GetInternal(TKey key)
        {
            return db.Get<TValue>(options, prefix, key);
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return db.TryGet<TValue>(options, prefix, key);
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            batch?.Put(prefix, key, value);
            mptTrie.Put(key.ToArray(), value.ToArray());
        }

        public override void Commit()
        {
            base.Commit();
            trieDb.PutRoot(mptTrie.GetRoot());
        }
    }
}
