using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_Blockchain : TestKit
    {
        private NeoSystem system;
        private Transaction txSample;
        private TestProbe senderProbe;

        [TestInitialize]
        public void Initialize()
        {
            system = TestBlockchain.TheNeoSystem;
            senderProbe = CreateTestProbe();
            txSample = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = Array.Empty<byte>(),
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                Witnesses = Array.Empty<Witness>()
            };
            system.MemPool.TryAdd(txSample, TestBlockchain.GetTestSnapshot());
        }

        [TestMethod]
        public void TestValidTransaction()
        {
            var snapshot = TestBlockchain.TheNeoSystem.GetSnapshot();
            var walletA = TestUtils.GenerateTestWallet();

            using var unlockA = walletA.Unlock("123");
            var acc = walletA.CreateAccount();

            // Fake balance

            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(acc.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();

            // Make transaction

            var tx = CreateValidTx(snapshot, walletA, acc.ScriptHash, 0);

            senderProbe.Send(system.Blockchain, tx);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.Succeed);

            senderProbe.Send(system.Blockchain, tx);
            senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.AlreadyExists);
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new()
            {
                Id = NativeContract.NEO.Id,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }

        private static Transaction CreateValidTx(DataCache snapshot, NEP6Wallet wallet, UInt160 account, uint nonce)
        {
            var tx = wallet.MakeTransaction(snapshot, new TransferOutput[]
                {
                    new TransferOutput()
                    {
                        AssetId = NativeContract.GAS.Hash,
                        ScriptHash = account,
                        Value = new BigDecimal(BigInteger.One,8)
                    }
                },
                account);

            tx.Nonce = nonce;

            var data = new ContractParametersContext(snapshot, tx, ProtocolSettings.Default.Network);
            Assert.IsNull(data.GetSignatures(tx.Sender));
            Assert.IsTrue(wallet.Sign(data));
            Assert.IsTrue(data.Completed);
            Assert.AreEqual(1, data.GetSignatures(tx.Sender).Count());

            tx.Witnesses = data.GetWitnesses();
            return tx;
        }
    }
}
