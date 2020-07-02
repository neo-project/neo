using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    internal class StoreDataCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly IReadOnlyStore store;
        private readonly ISnapshot snapshot;
        private readonly byte prefix;

        public StoreDataCache(IReadOnlyStore store, byte prefix)
        {
            this.store = store;
            this.snapshot = store as ISnapshot;
            this.prefix = prefix;
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            snapshot?.Put(prefix, key.ToArray(), value.ToArray());
        }

        protected override void DeleteInternal(TKey key)
        {
            snapshot?.Delete(prefix, key.ToArray());
        }

        protected override TValue GetInternal(TKey key)
        {
            return store.TryGet(prefix, key.ToArray()).AsSerializable<TValue>();
        }

        protected override IEnumerable<(TKey, TValue)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction)
        {
            return store.Seek(prefix, keyOrPrefix, direction).Select(p => (p.Key.AsSerializable<TKey>(), p.Value.AsSerializable<TValue>()));
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return store.TryGet(prefix, key.ToArray())?.AsSerializable<TValue>();
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            snapshot?.Put(prefix, key.ToArray(), value.ToArray());
        }
    }
}
