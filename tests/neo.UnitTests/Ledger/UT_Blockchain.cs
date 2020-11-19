using Akka.Actor;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using System.Reflection;

namespace Neo.UnitTests.Ledger
{
    internal class TestBlock : Block
    {
        public override bool Verify(StoreView snapshot)
        {
            return true;
        }

        public static TestBlock Cast(Block input)
        {
            return input.ToArray().AsSerializable<TestBlock>();
        }
    }

    internal class TestHeader : Header
    {
        public override bool Verify(StoreView snapshot)
        {
            return true;
        }

        public static TestHeader Cast(Header input)
        {
            return input.ToArray().AsSerializable<TestHeader>();
        }
    }

    [TestClass]
    public class UT_Blockchain : TestKit
    {
        private NeoSystem system;
        Transaction txSample = Blockchain.GenesisBlock.Transactions[0];

        [TestInitialize]
        public void Initialize()
        {
            system = TestBlockchain.TheNeoSystem;
            Blockchain.Singleton.MemPool.TryAdd(txSample, Blockchain.Singleton.GetSnapshot());
        }

        [TestMethod]
        public void TestContainsBlock()
        {
            Blockchain.Singleton.ContainsBlock(UInt256.Zero).Should().BeFalse();
        }

        [TestMethod]
        public void TestContainsTransaction()
        {
            Blockchain.Singleton.ContainsTransaction(UInt256.Zero).Should().BeFalse();
            Blockchain.Singleton.ContainsTransaction(txSample.Hash).Should().BeTrue();
        }

        [TestMethod]
        public void TestGetCurrentBlockHash()
        {
            Blockchain.Singleton.CurrentBlockHash.Should().Be(UInt256.Parse("0x2c898ce17d5877da87c97fec1c6d4a79b492ebb002416ef63427bd3014bee7d8"));
        }

        [TestMethod]
        public void TestGetCurrentHeaderHash()
        {
            Blockchain.Singleton.CurrentHeaderHash.Should().Be(UInt256.Parse("0x2c898ce17d5877da87c97fec1c6d4a79b492ebb002416ef63427bd3014bee7d8"));
        }

        [TestMethod]
        public void TestGetBlock()
        {
            Blockchain.Singleton.GetBlock(UInt256.Zero).Should().BeNull();
        }

        [TestMethod]
        public void TestGetBlockHash()
        {
            Blockchain.Singleton.GetBlockHash(0).Should().Be(UInt256.Parse("0x2c898ce17d5877da87c97fec1c6d4a79b492ebb002416ef63427bd3014bee7d8"));
            Blockchain.Singleton.GetBlockHash(10).Should().BeNull();
        }

        [TestMethod]
        public void TestGetTransaction()
        {
            Blockchain.Singleton.GetTransaction(UInt256.Zero).Should().BeNull();
            Blockchain.Singleton.GetTransaction(txSample.Hash).Should().NotBeNull();
        }

        [TestMethod]
        public void TestValidTransaction()
        {
            var senderProbe = CreateTestProbe();
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var walletA = TestUtils.GenerateTestWallet();

            using var unlockA = walletA.Unlock("123");
            var acc = walletA.CreateAccount();

            // Fake balance

            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(acc.ScriptHash);
            var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();

            typeof(Blockchain)
                .GetMethod("UpdateCurrentSnapshot", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(Blockchain.Singleton, null);

            // Make transaction

            var tx = CreateValidTx(walletA, acc.ScriptHash, 0);

            senderProbe.Send(system.Blockchain, tx);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.Succeed);

            senderProbe.Send(system.Blockchain, tx);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.AlreadyExists);
        }

        [TestMethod]
        public void TestInvalidTransactionInPersist()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = Array.Empty<Signer>(),
                NetworkFee = 0,
                Nonce = (uint)Environment.TickCount,
                Script = new byte[] { 1 },
                SystemFee = 0,
                ValidUntilBlock = Blockchain.GenesisBlock.Index + 1,
                Version = 0,
                Witnesses = new Witness[0],
            };
            StoreView clonedSnapshot = snapshot.Clone();
            var state = new TransactionState
            {
                BlockIndex = 0,
                Transaction = tx
            };
            clonedSnapshot.Transactions.Add(tx.Hash, state);
            clonedSnapshot.Transactions.Commit();
            state.VMState = VMState.FAULT;
            snapshot.Transactions.TryGet(tx.Hash).VMState.Should().Be(VMState.FAULT);
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                Id = NativeContract.NEO.Id,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }

        private Transaction CreateValidTx(NEP6Wallet wallet, UInt160 account, uint nonce)
        {
            var tx = wallet.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                        AssetId = NativeContract.GAS.Hash,
                        ScriptHash = account,
                        Value = new BigDecimal(1,8)
                    }
                },
                account);

            tx.Nonce = nonce;

            var data = new ContractParametersContext(tx);
            Assert.IsTrue(wallet.Sign(data));
            Assert.IsTrue(data.Completed);

            tx.Witnesses = data.GetWitnesses();
            return tx;
        }
    }
}
