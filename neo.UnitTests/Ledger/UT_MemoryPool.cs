using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MemoryPool
    {
        private MemoryPool _unit;

        [TestInitialize]
        public void TestSetup()
        {
            // protect against external changes on TimeProvider
            TimeProvider.ResetToDefault();

            NeoSystem TheNeoSystem = TestBlockchain.InitializeMockNeoSystem();

            // Create a MemoryPool with capacity of 100
            _unit = new MemoryPool(TheNeoSystem, 100);
            _unit.LoadPolicy(TestBlockchain.GetStore().GetSnapshot());

            // Verify capacity equals the amount specified
            _unit.Capacity.ShouldBeEquivalentTo(100);

            _unit.VerifiedCount.ShouldBeEquivalentTo(0);
            _unit.UnVerifiedCount.ShouldBeEquivalentTo(0);
            _unit.Count.ShouldBeEquivalentTo(0);
        }

        long LongRandom(long min, long max, Random rand)
        {
            // Only returns positive random long values.
            long longRand = (long)rand.NextBigInteger(63);
            return longRand % (max - min) + min;
        }

        private Transaction CreateTransactionWithFee(long fee)
        {
            Random random = new Random();
            var randomBytes = new byte[16];
            random.NextBytes(randomBytes);
            Mock<Transaction> mock = new Mock<Transaction>();
            mock.Setup(p => p.Reverify(It.IsAny<Snapshot>(), It.IsAny<IEnumerable<Transaction>>())).Returns(true);
            mock.Setup(p => p.Verify(It.IsAny<Snapshot>(), It.IsAny<IEnumerable<Transaction>>())).Returns(true);
            mock.Object.Script = randomBytes;
            mock.Object.Sender = UInt160.Zero;
            mock.Object.NetworkFee = fee;
            mock.Object.Attributes = new TransactionAttribute[0];
            mock.Object.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                }
            };
            return mock.Object;
        }

        private Transaction CreateTransaction()
        {
            return CreateTransactionWithFee(LongRandom(100000, 100000000, TestUtils.TestRandom));
        }

        private void AddTransactions(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var txToAdd = CreateTransaction();
                _unit.TryAdd(txToAdd.Hash, txToAdd);
            }

            Console.WriteLine($"created {count} tx");
        }


        [TestMethod]
        public void CapacityTest()
        {
            // Add over the capacity items, verify that the verified count increases each time
            AddTransactions(101);

            Console.WriteLine($"VerifiedCount: {_unit.VerifiedCount} Count {_unit.SortedTxCount}");

            _unit.SortedTxCount.ShouldBeEquivalentTo(100);
            _unit.VerifiedCount.ShouldBeEquivalentTo(100);
            _unit.UnVerifiedCount.ShouldBeEquivalentTo(0);
            _unit.Count.ShouldBeEquivalentTo(100);
        }

        [TestMethod]
        public void BlockPersistMovesTxToUnverifiedAndReverification()
        {
            AddTransactions(70);

            _unit.SortedTxCount.ShouldBeEquivalentTo(70);

            var block = new Block
            {
                Transactions = _unit.GetSortedVerifiedTransactions().Take(10)
                    .Concat(_unit.GetSortedVerifiedTransactions().Take(5)).ToArray()
            };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());
            _unit.InvalidateVerifiedTransactions();
            _unit.SortedTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(60);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.ShouldBeEquivalentTo(10);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(50);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.ShouldBeEquivalentTo(20);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(40);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.ShouldBeEquivalentTo(30);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(30);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.ShouldBeEquivalentTo(40);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(20);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.ShouldBeEquivalentTo(50);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(10);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.ShouldBeEquivalentTo(60);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(0);
        }

        private void verifyTransactionsSortedDescending(IEnumerable<Transaction> transactions)
        {
            Transaction lastTransaction = null;
            foreach (var tx in transactions)
            {
                if (lastTransaction != null)
                {
                    if (lastTransaction.FeePerByte == tx.FeePerByte)
                    {
                        if (lastTransaction.NetworkFee == tx.NetworkFee)
                            lastTransaction.Hash.Should().BeLessThan(tx.Hash);
                        else
                            lastTransaction.NetworkFee.Should().BeGreaterThan(tx.NetworkFee);
                    }
                    else
                    {
                        lastTransaction.FeePerByte.Should().BeGreaterThan(tx.FeePerByte);
                    }
                }
                lastTransaction = tx;
            }
        }

        [TestMethod]
        public void VerifySortOrderAndThatHighetFeeTransactionsAreReverifiedFirst()
        {
            AddTransactions(100);

            var sortedVerifiedTxs = _unit.GetSortedVerifiedTransactions().ToList();
            // verify all 100 transactions are returned in sorted order
            sortedVerifiedTxs.Count.ShouldBeEquivalentTo(100);
            verifyTransactionsSortedDescending(sortedVerifiedTxs);

            // move all to unverified
            var block = new Block { Transactions = new Transaction[0] };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());
            _unit.InvalidateVerifiedTransactions();
            _unit.SortedTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(100);

            // We can verify the order they are re-verified by reverifying 2 at a time
            while (_unit.UnVerifiedCount > 0)
            {
                _unit.GetVerifiedAndUnverifiedTransactions(out var sortedVerifiedTransactions, out var sortedUnverifiedTransactions);
                sortedVerifiedTransactions.Count().ShouldBeEquivalentTo(0);
                var sortedUnverifiedArray = sortedUnverifiedTransactions.ToArray();
                verifyTransactionsSortedDescending(sortedUnverifiedArray);
                var maxTransaction = sortedUnverifiedArray.First();
                var minTransaction = sortedUnverifiedArray.Last();

                // reverify 1 high priority and 1 low priority transaction
                _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(1, Blockchain.Singleton.GetSnapshot());
                var verifiedTxs = _unit.GetSortedVerifiedTransactions().ToArray();
                verifiedTxs.Length.ShouldBeEquivalentTo(1);
                verifiedTxs[0].ShouldBeEquivalentTo(maxTransaction);
                var blockWith2Tx = new Block { Transactions = new[] { maxTransaction, minTransaction } };
                // verify and remove the 2 transactions from the verified pool
                _unit.UpdatePoolForBlockPersisted(blockWith2Tx, Blockchain.Singleton.GetSnapshot());
                _unit.InvalidateVerifiedTransactions();
                _unit.SortedTxCount.ShouldBeEquivalentTo(0);
            }
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(0);
        }

        void VerifyCapacityThresholdForAttemptingToAddATransaction()
        {
            var sortedVerified = _unit.GetSortedVerifiedTransactions().ToArray();

            var txBarelyWontFit = CreateTransactionWithFee(sortedVerified.Last().NetworkFee - 1);
            _unit.CanTransactionFitInPool(txBarelyWontFit).ShouldBeEquivalentTo(false);
            var txBarelyFits = CreateTransactionWithFee(sortedVerified.Last().NetworkFee + 1);
            _unit.CanTransactionFitInPool(txBarelyFits).ShouldBeEquivalentTo(true);
        }

        [TestMethod]
        public void VerifyCanTransactionFitInPoolWorksAsIntended()
        {
            AddTransactions(100);
            VerifyCapacityThresholdForAttemptingToAddATransaction();
            AddTransactions(50);
            VerifyCapacityThresholdForAttemptingToAddATransaction();
            AddTransactions(50);
            VerifyCapacityThresholdForAttemptingToAddATransaction();
        }

        [TestMethod]
        public void CapacityTestWithUnverifiedHighProirtyTransactions()
        {
            // Verify that unverified high priority transactions will not be pushed out of the queue by incoming
            // low priority transactions

            // Fill pool with high priority transactions
            AddTransactions(99);

            // move all to unverified
            var block = new Block { Transactions = new Transaction[0] };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());

            _unit.CanTransactionFitInPool(CreateTransaction()).ShouldBeEquivalentTo(true);
            AddTransactions(1);
            _unit.CanTransactionFitInPool(CreateTransactionWithFee(0)).ShouldBeEquivalentTo(false);
        }

        [TestMethod]
        public void TestInvalidateAll()
        {
            AddTransactions(30);

            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(0);
            _unit.SortedTxCount.ShouldBeEquivalentTo(30);
            _unit.InvalidateAllTransactions();
            _unit.UnverifiedSortedTxCount.ShouldBeEquivalentTo(30);
            _unit.SortedTxCount.ShouldBeEquivalentTo(0);
        }
    }
}
