using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Data;
using Neo.IO.Data.RocksDB;
using RocksDbSharp;
using System;
using System.Collections.Generic;

namespace Neo.Persistence.RocksDB
{
    public class DbCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly DB db;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly ColumnFamilyHandle family;

        public DbCache(DB db, ReadOptions options, WriteBatch batch, ColumnFamilyHandle family)
        {
            this.db = db;
            this.options = options ?? DB.ReadDefault;
            this.batch = batch;
            this.family = family;
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            batch?.Put(family, key, value);
        }

        public override void DeleteInternal(TKey key)
        {
            batch?.Delete(family, key);
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return db.Find(family, options, SliceBuilder.Begin().Add(key_prefix), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(), v.ToArray().AsSerializable<TValue>()));
        }

        protected override TValue GetInternal(TKey key)
        {
            return db.Get<TValue>(family, options, key);
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return db.TryGet<TValue>(family, options, key);
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            batch?.Put(family, key, value);
        }
    }
}
