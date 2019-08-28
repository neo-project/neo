using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Data;
using Neo.IO.Data.LevelDB;
using System;

namespace Neo.Persistence.LevelDB
{
    internal class DbMetaDataCache<T> : MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        private readonly LevelDBCore db;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;
        private readonly byte prefix;

        public DbMetaDataCache(LevelDBCore db, byte prefix, ReadOptions options = null, WriteBatch batch = null, Func<T> factory = null)
            : base(factory)
        {
            this.db = db;
            this.prefix = prefix;
            this.options = options ?? ReadOptions.Default;
            this.batch = batch;
        }

        protected override void AddInternal(T item)
        {
            batch?.Put(prefix, item.ToArray());
        }

        protected override T TryGetInternal()
        {
            if (!db.TryGet(options, prefix, out Slice slice))
                return null;
            return slice.ToArray().AsSerializable<T>();
        }

        protected override void UpdateInternal(T item)
        {
            batch?.Put(prefix, item.ToArray());
        }
    }
}
