using AntShares.IO;
using AntShares.IO.Caching;
using System;

namespace AntShares.Implementations.Blockchains.LevelDB
{
    internal class DbCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ISerializable, new()
    {
        private DB db;
        private DataEntryPrefix prefix;

        public DbCache(DB db, DataEntryPrefix prefix)
        {
            this.db = db;
            this.prefix = prefix;
        }

        protected override TValue GetInternal(TKey key)
        {
            return db.Get<TValue>(ReadOptions.Default, prefix, key);
        }

        public void Commit(WriteBatch batch)
        {
            foreach (Trackable trackable in GetChangeSet())
                if (trackable.State != TrackState.Deleted)
                    batch.Put(prefix, trackable.Key, trackable.Item);
                else
                    batch.Delete(prefix, trackable.Key);
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return db.TryGet<TValue>(ReadOptions.Default, prefix, key);
        }
    }
}
