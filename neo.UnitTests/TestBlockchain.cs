using Moq;
using Neo.Cryptography.ECC;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        private static NeoSystem TheNeoSystem;

        public static NeoSystem InitializeMockNeoSystem()
        {
            if (TheNeoSystem == null)
            {
                var mockSnapshot = CreateMockSnapshot();

                var mockStore = new Mock<Store>();

                var defaultTx = TestUtils.CreateRandomHashInvocationMockTransaction().Object;
                mockStore.Setup(p => p.GetBlocks()).Returns(new TestDataCache<UInt256, BlockState>());
                mockStore.Setup(p => p.GetTransactions()).Returns(new TestDataCache<UInt256, TransactionState>(
                    new TransactionState
                    {
                        BlockIndex = 1,
                        Transaction = defaultTx
                    }));

                mockStore.Setup(p => p.GetAccounts()).Returns(new TestDataCache<UInt160, AccountState>());
                mockStore.Setup(p => p.GetUnspentCoins()).Returns(new TestDataCache<UInt256, UnspentCoinState>());
                mockStore.Setup(p => p.GetSpentCoins()).Returns(new TestDataCache<UInt256, SpentCoinState>());
                mockStore.Setup(p => p.GetValidators()).Returns(new TestDataCache<ECPoint, ValidatorState>());
                mockStore.Setup(p => p.GetAssets()).Returns(new TestDataCache<UInt256, AssetState>());
                mockStore.Setup(p => p.GetContracts()).Returns(new TestDataCache<UInt160, ContractState>());
                mockStore.Setup(p => p.GetStorages()).Returns(new TestDataCache<StorageKey, StorageItem>());
                mockStore.Setup(p => p.GetHeaderHashList()).Returns(new TestDataCache<UInt32Wrapper, HeaderHashList>());
                mockStore.Setup(p => p.GetValidatorsCount()).Returns(new TestMetaDataCache<ValidatorsCountState>());
                mockStore.Setup(p => p.GetBlockHashIndex()).Returns(new TestMetaDataCache<HashIndexState>());
                mockStore.Setup(p => p.GetHeaderHashIndex()).Returns(new TestMetaDataCache<HashIndexState>());
                mockStore.Setup(p => p.GetSnapshot()).Returns(mockSnapshot.Object);

                Console.WriteLine("initialize NeoSystem");
                TheNeoSystem = new NeoSystem(mockStore.Object); // new Mock<NeoSystem>(mockStore.Object);
            }

            return TheNeoSystem;
        }

        public static Mock<Snapshot> CreateMockSnapshot()
        {
            var mockSnapshot = new Mock<Snapshot>();
            mockSnapshot.SetupGet(p => p.Blocks).Returns(new TestDataCache<UInt256, BlockState>());
            mockSnapshot.SetupGet(p => p.Transactions).Returns(new TestDataCache<UInt256, TransactionState>());
            mockSnapshot.SetupGet(p => p.Accounts).Returns(new TestDataCache<UInt160, AccountState>());
            mockSnapshot.SetupGet(p => p.UnspentCoins).Returns(new TestDataCache<UInt256, UnspentCoinState>());
            mockSnapshot.SetupGet(p => p.SpentCoins).Returns(new TestDataCache<UInt256, SpentCoinState>());
            mockSnapshot.SetupGet(p => p.Validators).Returns(new TestDataCache<ECPoint, ValidatorState>());
            mockSnapshot.SetupGet(p => p.Assets).Returns(new TestDataCache<UInt256, AssetState>());
            mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>());
            mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>());
            mockSnapshot.SetupGet(p => p.HeaderHashList)
                .Returns(new TestDataCache<UInt32Wrapper, HeaderHashList>());
            mockSnapshot.SetupGet(p => p.ValidatorsCount).Returns(new TestMetaDataCache<ValidatorsCountState>());
            mockSnapshot.SetupGet(p => p.BlockHashIndex).Returns(new TestMetaDataCache<HashIndexState>());
            mockSnapshot.SetupGet(p => p.HeaderHashIndex).Returns(new TestMetaDataCache<HashIndexState>());

            mockSnapshot.Setup(p => p.GetValidators(It.IsAny<IEnumerable<Transaction>>())).Returns(new List<ECPoint>
                {
                    ECPoint.Parse("02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", Cryptography.ECC.ECCurve.Secp256r1),
                    ECPoint.Parse("03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a", Cryptography.ECC.ECCurve.Secp256r1)
                }
            );

            return mockSnapshot;
        }
    }
}
