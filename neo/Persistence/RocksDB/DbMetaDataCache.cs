using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Data.RocksDB;
using RocksDbSharp;
using System;

namespace Neo.Persistence.RocksDB
{
    internal class DbMetaDataCache<T> : MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        private readonly DB db;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly ColumnFamilyHandle family;
        private readonly byte[] key;

        public DbMetaDataCache(DB db, ReadOptions options, WriteBatch batch, ColumnFamilyHandle family, Func<T> factory = null)
            : base(factory)
        {
            this.db = db;
            this.options = options ?? DB.ReadDefault;
            this.batch = batch;
            this.family = family;
            this.key = new byte[0];
        }

        protected override void AddInternal(T item)
        {
            batch?.Put(key, item.ToArray(), family);
        }

        protected override T TryGetInternal()
        {
            if (!db.TryGet(family, options, key, out var value))
                return null;
            return value.AsSerializable<T>();
        }

        protected override void UpdateInternal(T item)
        {
            batch?.Put(key, item.ToArray(), family);
        }
    }
}
