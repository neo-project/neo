using Moq;
using Neo.Cryptography.ECC;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Persistence;
using System;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        private static NeoSystem TheNeoSystem;
        private static Mock<Store> _Store;

        public static Store GetStore()
        {
            if (_Store == null) InitializeMockNeoSystem();
            return _Store.Object;
        }

        public static NeoSystem InitializeMockNeoSystem()
        {
            if (TheNeoSystem == null)
            {
                var mockSnapshot = new Mock<Snapshot>();
                mockSnapshot.SetupGet(p => p.Blocks).Returns(new TestDataCache<UInt256, BlockState>());
                mockSnapshot.SetupGet(p => p.Transactions).Returns(new TestDataCache<UInt256, TransactionState>());
                mockSnapshot.SetupGet(p => p.Accounts).Returns(new TestDataCache<UInt160, AccountState>());
                mockSnapshot.SetupGet(p => p.UnspentCoins).Returns(new TestDataCache<UInt256, UnspentCoinState>());
                mockSnapshot.SetupGet(p => p.Validators).Returns(new TestDataCache<ECPoint, ValidatorState>());
                mockSnapshot.SetupGet(p => p.Assets).Returns(new TestDataCache<UInt256, AssetState>());
                mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>());
                mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>());
                mockSnapshot.SetupGet(p => p.HeaderHashList)
                    .Returns(new TestDataCache<UInt32Wrapper, HeaderHashList>());
                mockSnapshot.SetupGet(p => p.ValidatorsCount).Returns(new TestMetaDataCache<ValidatorsCountState>());
                mockSnapshot.SetupGet(p => p.BlockHashIndex).Returns(new TestMetaDataCache<HashIndexState>());
                mockSnapshot.SetupGet(p => p.HeaderHashIndex).Returns(new TestMetaDataCache<HashIndexState>());

                _Store = new Mock<Store>();

                var defaultTx = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
                _Store.Setup(p => p.GetBlocks()).Returns(new TestDataCache<UInt256, BlockState>());
                _Store.Setup(p => p.GetTransactions()).Returns(new TestDataCache<UInt256, TransactionState>(
                    new TransactionState
                    {
                        BlockIndex = 1,
                        Transaction = defaultTx
                    }));

                _Store.Setup(p => p.GetAccounts()).Returns(new TestDataCache<UInt160, AccountState>());
                _Store.Setup(p => p.GetUnspentCoins()).Returns(new TestDataCache<UInt256, UnspentCoinState>());
                _Store.Setup(p => p.GetValidators()).Returns(new TestDataCache<ECPoint, ValidatorState>());
                _Store.Setup(p => p.GetAssets()).Returns(new TestDataCache<UInt256, AssetState>());
                _Store.Setup(p => p.GetContracts()).Returns(new TestDataCache<UInt160, ContractState>());
                _Store.Setup(p => p.GetStorages()).Returns(new TestDataCache<StorageKey, StorageItem>());
                _Store.Setup(p => p.GetHeaderHashList()).Returns(new TestDataCache<UInt32Wrapper, HeaderHashList>());
                _Store.Setup(p => p.GetValidatorsCount()).Returns(new TestMetaDataCache<ValidatorsCountState>());
                _Store.Setup(p => p.GetBlockHashIndex()).Returns(new TestMetaDataCache<HashIndexState>());
                _Store.Setup(p => p.GetHeaderHashIndex()).Returns(new TestMetaDataCache<HashIndexState>());
                _Store.Setup(p => p.GetSnapshot()).Returns(mockSnapshot.Object);

                Console.WriteLine("initialize NeoSystem");
                TheNeoSystem = new NeoSystem(_Store.Object); // new Mock<NeoSystem>(mockStore.Object);
            }

            return TheNeoSystem;
        }
    }
}