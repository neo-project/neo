using Neo.Cryptography.MPT;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.Persistence
{
    internal class ClonedView : StoreView
    {
        private StoreView parent;
        public override DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList { get; }
        public override DataCache<SerializableWrapper<uint>, HashIndexState> LocalStateRoot { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }
        public override MetaDataCache<StateRoot> ConfirmedStateRoot { get; }
        public override MetaDataCache<ContractIdState> ContractId { get; }
        public override MPTTrie<StorageKey, StorageItem> Storages { get; set; }

        public ClonedView(StoreView view)
        {
            this.parent = view;
            this.PersistingBlock = view.PersistingBlock;
            this.Blocks = view.Blocks.CreateSnapshot();
            this.Transactions = view.Transactions.CreateSnapshot();
            this.Contracts = view.Contracts.CreateSnapshot();
            this.HeaderHashList = view.HeaderHashList.CreateSnapshot();
            this.LocalStateRoot = view.LocalStateRoot.CreateSnapshot();
            this.BlockHashIndex = view.BlockHashIndex.CreateSnapshot();
            this.HeaderHashIndex = view.HeaderHashIndex.CreateSnapshot();
            this.ConfirmedStateRoot = view.ConfirmedStateRoot.CreateSnapshot();
            this.ContractId = view.ContractId.CreateSnapshot();
            this.Storages = view.Storages.Clone();
        }

        public override void Commit()
        {
            base.Commit();
            parent.Storages = Storages;
        }
    }
}
