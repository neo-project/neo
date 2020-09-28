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

        public override DataCache<UInt256, TrimmedBlock> Blocks => new StoreDataCache<UInt256, TrimmedBlock>(store, Prefixes.DATA_Block);
        public override DataCache<UInt256, TransactionState> Transactions => new StoreDataCache<UInt256, TransactionState>(store, Prefixes.DATA_Transaction);
        public override DataCache<UInt160, ContractState> Contracts => new StoreDataCache<UInt160, ContractState>(store, Prefixes.ST_Contract);
        public override DataCache<StorageKey, StorageItem> Storages => new StoreDataCache<StorageKey, StorageItem>(store, Prefixes.ST_Storage);
        public override DataCache<UInt160, ByteArraySerializable> CachedScripts => new StoreDataCache<UInt160, ByteArraySerializable>(store, Prefixes.IX_CachedScripts);
        public override DataCache<SerializableWrapper<uint>, HeaderHashList> HeaderHashList => new StoreDataCache<SerializableWrapper<uint>, HeaderHashList>(store, Prefixes.IX_HeaderHashList);
        public override MetaDataCache<HashIndexState> BlockHashIndex => new StoreMetaDataCache<HashIndexState>(store, Prefixes.IX_CurrentBlock);
        public override MetaDataCache<HashIndexState> HeaderHashIndex => new StoreMetaDataCache<HashIndexState>(store, Prefixes.IX_CurrentHeader);
        public override MetaDataCache<ContractIdState> ContractId => new StoreMetaDataCache<ContractIdState>(store, Prefixes.IX_ContractId);

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
