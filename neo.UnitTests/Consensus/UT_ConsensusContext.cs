using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Consensus;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.UnitTests.Consensus
{

    [TestClass]
    public class UT_ConsensusContext : TestKit
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestMethod]
        public void TestMaxBlockSize()
        {
            var mockWallet = new Mock<Wallet>();

            mockWallet.Setup(p => p.GetAccount(It.IsAny<UInt160>())).Returns<UInt160>(p => new TestWalletAccount(p));
            ConsensusContext context = new ConsensusContext(mockWallet.Object, TestBlockchain.GetStore());
            context.Reset(0);

            // Only one tx

            var tx1 = CreateTransactionWithSize(200);
            context.EnsureMaxBlockSize(new Transaction[] { tx1 });
            EnsureContext(context, tx1);

            // Two tx, the last one exceed

            var tx2 = CreateTransactionWithSize(256 * 1024);
            context.EnsureMaxBlockSize(new Transaction[] { tx1, tx2 });
            EnsureContext(context, tx1);

            // Exceed txs

            var max = (int)NativeContract.Policy.GetMaxTransactionsPerBlock(context.Snapshot);
            var txs = new Transaction[max + 1];

            for (int x = 0; x < max; x++) txs[x] = CreateTransactionWithSize(100);

            context.EnsureMaxBlockSize(txs);
            EnsureContext(context, txs.Take(max).ToArray());

            // All txs

            txs = txs.Take(max).ToArray();

            context.EnsureMaxBlockSize(txs);
            EnsureContext(context, txs);
        }

        private Transaction CreateTransactionWithSize(int v)
        {
            var r = new Random();
            var tx = new Transaction()
            {
                Attributes = new TransactionAttribute[0],
                NetworkFee = 0,
                Nonce = (uint)Environment.TickCount,
                Script = new byte[0],
                Sender = UInt160.Zero,
                SystemFee = 0,
                ValidUntilBlock = (uint)r.Next(),
                Version = 0,
                Witnesses = new Witness[0],
            };

            // Could be higher (few bytes) if varSize grows
            tx.Script = new byte[v - tx.Size];
            return tx;
        }

        private void EnsureContext(ConsensusContext context, params Transaction[] expected)
        {
            // Check all tx

            Assert.AreEqual(expected.Length, context.Transactions.Count);
            Assert.IsTrue(expected.All(tx => context.Transactions.ContainsKey(tx.Hash)));

            Assert.AreEqual(expected.Length, context.TransactionHashes.Length);
            Assert.IsTrue(expected.All(tx => context.TransactionHashes.Count(t => t == tx.Hash) == 1));

            // Ensure length

            // TODO:  Should check the size? we need to mock the signatures

            //var block = context.CreateBlock();
            //Assert.IsTrue(block.Size < NativeContract.Policy.GetMaxBlockSize(context.Snapshot));
        }
    }
}
