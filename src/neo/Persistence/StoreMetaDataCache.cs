using Neo.IO;
using Neo.IO.Caching;
using System;

namespace Neo.Persistence
{
    public class StoreMetaDataCache<T> : MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        private readonly IReadOnlyStore store;
        private readonly ISnapshot snapshot;
        private readonly byte prefix;

        public StoreMetaDataCache(IReadOnlyStore store, byte prefix, Func<T> factory = null)
            : base(factory)
        {
            this.store = store;
            this.snapshot = store as ISnapshot;
            this.prefix = prefix;
        }

        protected override void AddInternal(T item)
        {
            snapshot?.Put(prefix, null, item.ToArray());
        }

        protected override T TryGetInternal()
        {
            return store.TryGet(prefix, null)?.AsSerializable<T>();
        }

        protected override void UpdateInternal(T item)
        {
            snapshot?.Put(prefix, null, item.ToArray());
        }
    }
}
