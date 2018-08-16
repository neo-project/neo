using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;

namespace Neo.Persistence
{
    public abstract class Store : IPersistence
    {
        DataCache<UInt256, BlockState> IPersistence.Blocks => GetBlocks();
        DataCache<UInt256, TransactionState> IPersistence.Transactions => GetTransactions();
        DataCache<UInt160, AccountState> IPersistence.Accounts => GetAccounts();
        DataCache<UInt256, UnspentCoinState> IPersistence.UnspentCoins => GetUnspentCoins();
        DataCache<UInt256, SpentCoinState> IPersistence.SpentCoins => GetSpentCoins();
        DataCache<ECPoint, ValidatorState> IPersistence.Validators => GetValidators();
        DataCache<UInt256, AssetState> IPersistence.Assets => GetAssets();
        DataCache<UInt160, ContractState> IPersistence.Contracts => GetContracts();
        DataCache<StorageKey, StorageItem> IPersistence.Storages => GetStorages();
        DataCache<UInt32Wrapper, HeaderHashList> IPersistence.HeaderHashList => GetHeaderHashList();
        MetaDataCache<ValidatorsCountState> IPersistence.ValidatorsCount => GetValidatorsCount();
        MetaDataCache<HashIndexState> IPersistence.BlockHashIndex => GetBlockHashIndex();
        MetaDataCache<HashIndexState> IPersistence.HeaderHashIndex => GetHeaderHashIndex();

        public abstract DataCache<UInt256, BlockState> GetBlocks();
        public abstract DataCache<UInt256, TransactionState> GetTransactions();
        public abstract DataCache<UInt160, AccountState> GetAccounts();
        public abstract DataCache<UInt256, UnspentCoinState> GetUnspentCoins();
        public abstract DataCache<UInt256, SpentCoinState> GetSpentCoins();
        public abstract DataCache<ECPoint, ValidatorState> GetValidators();
        public abstract DataCache<UInt256, AssetState> GetAssets();
        public abstract DataCache<UInt160, ContractState> GetContracts();
        public abstract DataCache<StorageKey, StorageItem> GetStorages();
        public abstract DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList();
        public abstract MetaDataCache<ValidatorsCountState> GetValidatorsCount();
        public abstract MetaDataCache<HashIndexState> GetBlockHashIndex();
        public abstract MetaDataCache<HashIndexState> GetHeaderHashIndex();

        public abstract Snapshot GetSnapshot();
    }
}
