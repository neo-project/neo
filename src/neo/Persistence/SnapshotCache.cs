using Neo.IO;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    public class SnapshotCache : DataCache, IDisposable
    {
        private readonly IReadOnlyStore store;
        private readonly ISnapshot snapshot;

        public SnapshotCache(IReadOnlyStore store)
        {
            this.store = store;
            this.snapshot = store as ISnapshot;
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            snapshot?.Put(key.ToArray(), value.ToArray());
        }

        protected override void DeleteInternal(StorageKey key)
        {
            snapshot?.Delete(key.ToArray());
        }

        public override void Commit()
        {
            base.Commit();
            snapshot.Commit();
        }

        protected override bool ContainsInternal(StorageKey key)
        {
            return store.Contains(key.ToArray());
        }

        public void Dispose()
        {
            snapshot?.Dispose();
        }

        protected override StorageItem GetInternal(StorageKey key)
        {
            return store.TryGet(key.ToArray()).AsSerializable<StorageItem>();
        }

        protected override IEnumerable<(StorageKey, StorageItem)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction)
        {
            return store.Seek(keyOrPrefix, direction).Select(p => (p.Key.AsSerializable<StorageKey>(), p.Value.AsSerializable<StorageItem>()));
        }

        protected override StorageItem TryGetInternal(StorageKey key)
        {
            return store.TryGet(key.ToArray())?.AsSerializable<StorageItem>();
        }

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            snapshot?.Put(key.ToArray(), value.ToArray());
        }
    }
}
