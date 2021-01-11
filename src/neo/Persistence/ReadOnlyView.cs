using Neo.IO.Caching;
using Neo.Ledger;
using System;

namespace Neo.Persistence
{
    /// <summary>
    /// Provide a read-only <see cref="StoreView"/> for accessing directly from database instead of from snapshot.
    /// </summary>
    public class ReadOnlyView : StoreView
    {
        private readonly IReadOnlyStore store;

        public override DataCache<StorageKey, StorageItem> Storages => new StoreDataCache<StorageKey, StorageItem>(store);

        public ReadOnlyView(IReadOnlyStore store)
        {
            this.store = store;
        }

        public override void Commit()
        {
            throw new NotSupportedException();
        }
    }
}
