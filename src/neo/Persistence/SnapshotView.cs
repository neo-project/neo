using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.SmartContract;
using System;
using System.Collections.Generic;

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
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public SnapshotView(IStore store, HashSet<UInt160> contractHashSet = null)
        {
            this.snapshot = store.GetSnapshot();
            Blocks = new StoreDataCache<UInt256, TrimmedBlock>(snapshot, Prefixes.DATA_Block);
            Transactions = new StoreDataCache<UInt256, TransactionState>(snapshot, Prefixes.DATA_Transaction);
            Storages = new StoreDataCache<StorageKey, StorageItem>(snapshot, Prefixes.ST_Storage);
            HeaderHashList = new StoreDataCache<SerializableWrapper<uint>, HeaderHashList>(snapshot, Prefixes.IX_HeaderHashList);
            BlockHashIndex = new StoreMetaDataCache<HashIndexState>(snapshot, Prefixes.IX_CurrentBlock);
            HeaderHashIndex = new StoreMetaDataCache<HashIndexState>(snapshot, Prefixes.IX_CurrentHeader);
            this.ContractHashSet = contractHashSet;
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
