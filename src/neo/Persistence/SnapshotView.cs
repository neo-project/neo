using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Persistence
{
    /// <summary>
    /// Provide a <see cref="StoreView"/> for accessing snapshots.
    /// </summary>
    public class SnapshotView : StoreView, IDisposable
    {
        private readonly ISnapshot snapshot;

        public override DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList { get; }
        public override DataCache<SerializableWrapper<uint>, HashIndexState> LocalStateRoot { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }
        public override MetaDataCache<StateRoot> ValidatorsStateRoot { get; }
        public override MetaDataCache<ContractIdState> ContractId { get; }

        public SnapshotView(IStore store)
        {
            this.snapshot = store.GetSnapshot();
            Blocks = new StoreDataCache<UInt256, TrimmedBlock>(snapshot, Prefixes.DATA_Block);
            Transactions = new StoreDataCache<UInt256, TransactionState>(snapshot, Prefixes.DATA_Transaction);
            Contracts = new StoreDataCache<UInt160, ContractState>(snapshot, Prefixes.ST_Contract);
            HeaderHashList = new StoreDataCache<SerializableWrapper<uint>, HeaderHashList>(snapshot, Prefixes.IX_HeaderHashList);
            LocalStateRoot = new StoreDataCache<SerializableWrapper<uint>, HashIndexState>(snapshot, Prefixes.ST_LocalStateRoot);
            BlockHashIndex = new StoreMetaDataCache<HashIndexState>(snapshot, Prefixes.IX_CurrentBlock);
            HeaderHashIndex = new StoreMetaDataCache<HashIndexState>(snapshot, Prefixes.IX_CurrentHeader);
            ContractId = new StoreMetaDataCache<ContractIdState>(snapshot, Prefixes.IX_ContractId);
            ValidatorsStateRoot = new StoreMetaDataCache<StateRoot>(snapshot, Prefixes.IX_ValidatorsStateRoot);
            Storages = new MPTDataCache<StorageKey, StorageItem>(snapshot, CurrentStateRootHash);//Need BlockHashIndex and LocalStateRoot loaded.
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
