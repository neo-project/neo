using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Consensus;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.UnitTests.Consensus
{

    [TestClass]
    public class UT_ConsensusContext : TestKit
    {
        ConsensusContext _context;
        KeyPair[] _validatorKeys;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();

            var rand = new Random();
            var mockWallet = new Mock<Wallet>();
            mockWallet.Setup(p => p.GetAccount(It.IsAny<UInt160>())).Returns<UInt160>(p => new TestWalletAccount(p));

            // Create dummy validators

            _validatorKeys = new KeyPair[7];
            for (int x = 0; x < _validatorKeys.Length; x++)
            {
                var pk = new byte[32];
                rand.NextBytes(pk);

                _validatorKeys[x] = new KeyPair(pk);
            }

            _context = new ConsensusContext(mockWallet.Object, TestBlockchain.GetStore())
            {
                Validators = _validatorKeys.Select(u => u.PublicKey).ToArray()
            };
            _context.Reset(0);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestMethod]
        public void TestMaxBlockSize_Good()
        {
            // Only one tx, is included

            var tx1 = CreateTransactionWithSize(200);
            _context.EnsureMaxBlockSize(new Transaction[] { tx1 });
            EnsureContext(_context, tx1);

            // All txs included

            var max = (int)NativeContract.Policy.GetMaxTransactionsPerBlock(_context.Snapshot);
            var txs = new Transaction[max];

            for (int x = 0; x < max; x++) txs[x] = CreateTransactionWithSize(100);

            _context.EnsureMaxBlockSize(txs);
            EnsureContext(_context, txs);
        }

        [TestMethod]
        public void TestMaxBlockSize_Exceed()
        {
            // Two tx, the last one exceed the size rule, only the first will be included

            var tx1 = CreateTransactionWithSize(200);
            var tx2 = CreateTransactionWithSize(256 * 1024);
            _context.EnsureMaxBlockSize(new Transaction[] { tx1, tx2 });
            EnsureContext(_context, tx1);

            // Exceed txs number, just MaxTransactionsPerBlock included

            var max = (int)NativeContract.Policy.GetMaxTransactionsPerBlock(_context.Snapshot);
            var txs = new Transaction[max + 1];

            for (int x = 0; x < max; x++) txs[x] = CreateTransactionWithSize(100);

            _context.EnsureMaxBlockSize(txs);
            EnsureContext(_context, txs.Take(max).ToArray());
        }

        private Transaction CreateTransactionWithSize(int v)
        {
            var r = new Random();
            var tx = new Transaction()
            {
                Cosigners = new Cosigner[0],
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

        private Block SignBlock(ConsensusContext context)
        {
            context.Block.MerkleRoot = null;

            for (int x = 0; x < _validatorKeys.Length; x++)
            {
                _context.MyIndex = x;

                var com = _context.MakeCommit();
                _context.CommitPayloads[_context.MyIndex] = com;
            }

            // Manual block sign

            Contract contract = Contract.CreateMultiSigContract(context.M, context.Validators);
            ContractParametersContext sc = new ContractParametersContext(context.Block);
            for (int i = 0, j = 0; i < context.Validators.Length && j < context.M; i++)
            {
                if (context.CommitPayloads[i]?.ConsensusMessage.ViewNumber != context.ViewNumber) continue;
                sc.AddSignature(contract, context.Validators[i], context.CommitPayloads[i].GetDeserializedMessage<Commit>().Signature);
                j++;
            }
            context.Block.Witness = sc.GetWitnesses()[0];
            context.Block.Transactions = context.TransactionHashes.Select(p => context.Transactions[p]).ToArray();
            return context.Block;
        }

        private void EnsureContext(ConsensusContext context, params Transaction[] expected)
        {
            // Check all tx

            Assert.AreEqual(expected.Length, context.Transactions.Count);
            Assert.IsTrue(expected.All(tx => context.Transactions.ContainsKey(tx.Hash)));

            Assert.AreEqual(expected.Length, context.TransactionHashes.Length);
            Assert.IsTrue(expected.All(tx => context.TransactionHashes.Count(t => t == tx.Hash) == 1));

            // Ensure length

            var block = SignBlock(context);

            Assert.AreEqual(context.GetExpectedBlockSize(), block.Size);
            Assert.IsTrue(block.Size < NativeContract.Policy.GetMaxBlockSize(context.Snapshot));
        }
    }
}
