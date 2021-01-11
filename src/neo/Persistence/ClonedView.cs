using Neo.IO.Caching;
using Neo.Ledger;

namespace Neo.Persistence
{
    internal class ClonedView : StoreView
    {
        public override DataCache<StorageKey, StorageItem> Storages { get; }

        public ClonedView(StoreView view)
        {
            this.Storages = view.Storages.CreateSnapshot();
        }
    }
}
