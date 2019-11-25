using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;

namespace Neo.Persistence
{
    public interface IPersistence
    {
        DataCache<UInt256, TrimmedBlock> Blocks { get; }
        DataCache<UInt256, TransactionState> Transactions { get; }
        DataCache<UInt160, ContractState> Contracts { get; }
        DataCache<StorageKey, StorageItem> Storages { get; }
        DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        MetaDataCache<HashIndexState> BlockHashIndex { get; }
        MetaDataCache<HashIndexState> HeaderHashIndex { get; }
    }
}
