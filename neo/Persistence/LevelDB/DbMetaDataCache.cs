using LevelDB;
using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Reflection;

namespace Neo.Persistence.LevelDB
{
    internal class DbMetaDataCache<T> : MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        private readonly DB db;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly byte prefix;

        public DbMetaDataCache(DB db, ReadOptions options, WriteBatch batch, byte prefix, Func<T> factory = null)
            : base(factory)
        {
            this.db = db;
            this.options = options ?? new ReadOptions();
            this.batch = batch;
            this.prefix = prefix;
        }

        protected override void AddInternal(T item)
        {
            batch?.Put(new byte[] { prefix }, item.ToArray());
        }

        protected override T TryGetInternal()
        {
            var value = db.Get(new byte[] { prefix }, options);
            return value?.AsSerializable<T>();
        }

        protected override void UpdateInternal(T item)
        {
            batch?.Put(new byte[] { prefix }, item.ToArray());
        }

        public override void Dispose()
        {
            options?.Dispose();
        }
    }
}
