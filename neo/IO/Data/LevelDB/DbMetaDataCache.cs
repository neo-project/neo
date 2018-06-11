using Neo.IO.Caching;
using System;

namespace Neo.IO.Data.LevelDB
{
    public class DbMetaDataCache<T> : MetaDataCache<T> where T : class, ISerializable, new()
    {
        private DB db;
        private byte prefix;

        public DbMetaDataCache(DB db, byte prefix, Func<T> factory = null)
            : base(factory)
        {
            this.db = db;
            this.prefix = prefix;
        }

        public void Commit(WriteBatch batch)
        {
            switch (State)
            {
                case TrackState.Added:
                case TrackState.Changed:
                    batch.Put(prefix, Item.ToArray());
                    break;
                case TrackState.Deleted:
                    batch.Delete(prefix);
                    break;
            }
        }

        protected override T TryGetInternal()
        {
            if (!db.TryGet(ReadOptions.Default, prefix, out Slice slice))
                return null;
            return slice.ToArray().AsSerializable<T>();
        }
    }
}
