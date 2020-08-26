using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Data.LevelDB;
using Neo.Trie.MPT;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence.LevelDB
{
    public class DbCacheWithTrie<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly byte prefix;
        private MPTTrie mptTrie;
        private DbTrieStore trieDb;

        public DbCacheWithTrie(DB db, ReadOptions options, WriteBatch batch, byte prefix)
        {
            this.prefix = prefix;
            this.trieDb = new DbTrieStore(db, options, batch, prefix);
            this.mptTrie = new MPTTrie(trieDb.GetRoot(), trieDb, !ProtocolSettings.Default.FullState);
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            mptTrie?.Put(key.ToArray(), value.ToArray());
        }

        public override void DeleteInternal(TKey key)
        {
            mptTrie?.TryDelete(key.ToArray());
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return mptTrie.Find(key_prefix).Select(p => new KeyValuePair<TKey, TValue>(p.Key.AsSerializable<TKey>(), p.Value.AsSerializable<TValue>()));
        }

        protected override TValue GetInternal(TKey key)
        {
            var exist = mptTrie.TryGet(key.ToArray(), out byte[] value);
            if (exist) return value.AsSerializable<TValue>();
            return null;
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return GetInternal(key);
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            mptTrie?.Put(key.ToArray(), value.ToArray());
        }

        public override void Commit()
        {
            base.Commit();
            mptTrie.Commit();
            trieDb.PutRoot(mptTrie.GetRoot());
        }
    }
}
