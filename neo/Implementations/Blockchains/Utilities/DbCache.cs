using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;

namespace Neo.Implementations.Blockchains.Utilities
{
    internal class
    DbCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ISerializable, new()
    {
        private AbstractDB db;
        private DataEntryPrefix prefix;
        private AbstractEntityFactory f;

        public DbCache(AbstractDB db, DataEntryPrefix prefix, AbstractEntityFactory f)
        {
            this.db = db;
            this.prefix = prefix;
            this.f = f;
        }

        public override IEnumerable<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return db.Find(f.getDefaultReadOptions(), SliceBuilder.Begin(prefix), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(), v.ToArray().AsSerializable<TValue>()));
        }

        public void Commit(AbstractWriteBatch batch)
        {
            foreach (Trackable trackable in GetChangeSet())
                if (trackable.State != TrackState.Deleted)
                    batch.Put(prefix, trackable.Key, trackable.Item);
                else
                    batch.Delete(prefix, trackable.Key);
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return db.Find(f.getDefaultReadOptions(), SliceBuilder.Begin(prefix).Add(key_prefix), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(), v.ToArray().AsSerializable<TValue>()));
        }

        protected override TValue GetInternal(TKey key)
        {
            return db.Get<TValue>(f.getDefaultReadOptions(), prefix, key);
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return db.TryGet<TValue>(f.getDefaultReadOptions(), prefix, key);
        }
    }
}
