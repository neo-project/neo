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
        public override DataCache<SerializableWrapper<uint>, HashState> LocalRoot { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }
        public override MetaDataCache<StateRoot> ConfirmedRootHashIndex { get; }
        public override MetaDataCache<ContractIdState> ContractId { get; }

        public ClonedView(StoreView view)
        {
            this.parent = view;
            this.PersistingBlock = view.PersistingBlock;
            this.Blocks = view.Blocks.CreateSnapshot();
            this.Transactions = view.Transactions.CreateSnapshot();
            this.Contracts = view.Contracts.CreateSnapshot();
            this.HeaderHashList = view.HeaderHashList.CreateSnapshot();
            this.LocalRoot = view.LocalRoot.CreateSnapshot();
            this.BlockHashIndex = view.BlockHashIndex.CreateSnapshot();
            this.HeaderHashIndex = view.HeaderHashIndex.CreateSnapshot();
            this.ConfirmedRootHashIndex = view.ConfirmedRootHashIndex.CreateSnapshot();
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
