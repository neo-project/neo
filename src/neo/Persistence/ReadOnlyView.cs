using Neo.IO;
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

        private StoreDataCache<UInt256, TrimmedBlock> _Blocks;
        private StoreDataCache<UInt256, TransactionState> _Transactions;
        private StoreDataCache<UInt160, ContractState> _Contracts;
        private StoreDataCache<StorageKey, StorageItem> _Storages;
        private StoreDataCache<SerializableWrapper<uint>, HeaderHashList> _HeaderHashList;
        private StoreMetaDataCache<HashIndexState> _BlockHashIndex, _HeaderHashIndex;
        private StoreMetaDataCache<ContractIdState> _ContractId;

        public override DataCache<UInt256, TrimmedBlock> Blocks
        {
            get
            {
                _Blocks ??= new StoreDataCache<UInt256, TrimmedBlock>(store, Prefixes.DATA_Block);
                return _Blocks;
            }
        }
        public override DataCache<UInt256, TransactionState> Transactions
        {
            get
            {
                _Transactions ??= new StoreDataCache<UInt256, TransactionState>(store, Prefixes.DATA_Transaction);
                return _Transactions;
            }
        }
        public override DataCache<UInt160, ContractState> Contracts
        {
            get
            {
                _Contracts ??= new StoreDataCache<UInt160, ContractState>(store, Prefixes.ST_Contract);
                return _Contracts;
            }
        }
        public override DataCache<StorageKey, StorageItem> Storages
        {
            get
            {
                _Storages ??= new StoreDataCache<StorageKey, StorageItem>(store, Prefixes.ST_Storage);
                return _Storages;
            }
        }
        public override DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList
        {
            get
            {
                _HeaderHashList ??= new StoreDataCache<SerializableWrapper<uint>, HeaderHashList>(store, Prefixes.IX_HeaderHashList);
                return _HeaderHashList;
            }
        }
        public override MetaDataCache<HashIndexState> BlockHashIndex
        {
            get
            {
                _BlockHashIndex ??= new StoreMetaDataCache<HashIndexState>(store, Prefixes.IX_CurrentBlock);
                return _BlockHashIndex;
            }
        }
        public override MetaDataCache<HashIndexState> HeaderHashIndex
        {
            get
            {
                _HeaderHashIndex ??= new StoreMetaDataCache<HashIndexState>(store, Prefixes.IX_CurrentHeader);
                return _HeaderHashIndex;
            }
        }
        public override MetaDataCache<ContractIdState> ContractId
        {
            get
            {
                _ContractId ??= new StoreMetaDataCache<ContractIdState>(store, Prefixes.IX_ContractId);
                return _ContractId;
            }
        }

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
