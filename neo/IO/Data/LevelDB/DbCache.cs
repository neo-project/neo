using Neo.IO.Caching;
using System;
using System.Collections.Generic;

namespace Neo.IO.Data.LevelDB
{
    public class DbCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private DB db;
        private WriteBatch batch;
        private byte prefix;

        public DbCache(DB db, byte prefix, WriteBatch batch = null)
        {
            this.db = db;
            this.batch = batch;
            this.prefix = prefix;
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            batch?.Put(prefix, key, value);
        }

        public override void DeleteInternal(TKey key)
        {
            batch?.Delete(prefix, key);
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return db.Find(ReadOptions.Default, SliceBuilder.Begin(prefix).Add(key_prefix), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(1), v.ToArray().AsSerializable<TValue>()));
        }

        protected override TValue GetInternal(TKey key)
        {
            return db.Get<TValue>(ReadOptions.Default, prefix, key);
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return db.TryGet<TValue>(ReadOptions.Default, prefix, key);
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            batch?.Put(prefix, key, value);
        }
    }
}
