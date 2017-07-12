using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class DbCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private DB db;
        private DataEntryPrefix prefix;

        public DbCache(DB db, DataEntryPrefix prefix)
        {
            this.db = db;
            this.prefix = prefix;
        }

        public void Commit(WriteBatch batch)
        {
            foreach (Trackable trackable in GetChangeSet())
                if (trackable.State != TrackState.Deleted)
                    batch.Put(prefix, trackable.Key, trackable.Item);
                else
                    batch.Delete(prefix, trackable.Key);
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return db.Find(ReadOptions.Default, SliceBuilder.Begin(prefix).Add(key_prefix), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(), v.ToArray().AsSerializable<TValue>()));
        }

        protected override TValue GetInternal(TKey key)
        {
            return db.Get<TValue>(ReadOptions.Default, prefix, key);
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return db.TryGet<TValue>(ReadOptions.Default, prefix, key);
        }
    }
}
