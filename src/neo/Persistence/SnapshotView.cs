using Neo.IO.Caching;
using Neo.Ledger;
using System;

namespace Neo.Persistence
{
    /// <summary>
    /// Provide a <see cref="StoreView"/> for accessing snapshots.
    /// </summary>
    public class SnapshotView : StoreView, IDisposable
    {
        private readonly ISnapshot snapshot;

        public override DataCache<StorageKey, StorageItem> Storages { get; }

        public SnapshotView(IStore store)
        {
            this.snapshot = store.GetSnapshot();
            Storages = new StoreDataCache<StorageKey, StorageItem>(snapshot);
        }

        public override void Commit()
        {
            base.Commit();
            snapshot.Commit();
        }

        public void Dispose()
        {
            snapshot.Dispose();
        }
    }
}
