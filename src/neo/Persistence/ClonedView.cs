using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;

namespace Neo.Persistence
{
    internal class ClonedView : StoreView
    {
        public override DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public ClonedView(StoreView view)
        {
            this.PersistingBlock = view.PersistingBlock;
            this.Blocks = view.Blocks.CreateSnapshot();
            this.Transactions = view.Transactions.CreateSnapshot();
            this.Contracts = view.Contracts.CreateSnapshot();
            this.Storages = view.Storages.CreateSnapshot();
            this.HeaderHashList = view.HeaderHashList.CreateSnapshot();
            this.BlockHashIndex = view.BlockHashIndex.CreateSnapshot();
            this.HeaderHashIndex = view.HeaderHashIndex.CreateSnapshot();
        }
    }
}
