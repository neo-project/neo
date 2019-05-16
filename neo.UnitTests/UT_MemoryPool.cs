using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests
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
            mock.Setup(p => p.Verify(It.IsAny<Snapshot>(), It.IsAny<IEnumerable<Transaction>>())).Returns(true);
            mock.Object.Script = randomBytes;
            mock.Object.Sender = UInt160.Zero;
            mock.Object.NetworkFee = fee;
            mock.Object.Attributes = new TransactionAttribute[0];
            mock.Object.Witnesses = new Witness[0];
            return mock.Object;
        }

        private Transaction CreateHighPriorityTransaction()
        {
            return CreateTransactionWithFee(LongRandom(100000, 100000000, TestUtils.TestRandom));
        }

        private Transaction CreateLowPriorityTransaction()
        {
            long rNetFee = LongRandom(0, 100000, TestUtils.TestRandom);
            // [0,0.001] GAS a fee lower than the threshold of 0.001 GAS (not enough to be a high priority TX)
            return CreateTransactionWithFee(rNetFee);
        }

        private void AddTransactions(int count, bool isHighPriority = false)
        {
            for (int i = 0; i < count; i++)
            {
                var txToAdd = isHighPriority ? CreateHighPriorityTransaction() : CreateLowPriorityTransaction();
                Console.WriteLine($"created tx: {txToAdd.Hash}");
                _unit.TryAdd(txToAdd.Hash, txToAdd);
            }
        }

        private void AddLowPriorityTransactions(int count) => AddTransactions(count);
        public void AddHighPriorityTransactions(int count) => AddTransactions(count, true);

        [TestMethod]
        public void LowPriorityCapacityTest()
        {
            // Add over the capacity items, verify that the verified count increases each time
            AddLowPriorityTransactions(50);
            _unit.VerifiedCount.ShouldBeEquivalentTo(50);
            AddLowPriorityTransactions(51);
            Console.WriteLine($"VerifiedCount: {_unit.VerifiedCount}  LowPrioCount {_unit.SortedLowPrioTxCount}  HighPrioCount {_unit.SortedHighPrioTxCount}");
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(100);
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(0);

            _unit.VerifiedCount.ShouldBeEquivalentTo(100);
            _unit.UnVerifiedCount.ShouldBeEquivalentTo(0);
            _unit.Count.ShouldBeEquivalentTo(100);
        }

        [TestMethod]
        public void HighPriorityCapacityTest()
        {
            // Add over the capacity items, verify that the verified count increases each time
            AddHighPriorityTransactions(101);

            Console.WriteLine($"VerifiedCount: {_unit.VerifiedCount}  LowPrioCount {_unit.SortedLowPrioTxCount}  HighPrioCount {_unit.SortedHighPrioTxCount}");
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(100);

            _unit.VerifiedCount.ShouldBeEquivalentTo(100);
            _unit.UnVerifiedCount.ShouldBeEquivalentTo(0);
            _unit.Count.ShouldBeEquivalentTo(100);
        }

        [TestMethod]
        public void HighPriorityPushesOutLowPriority()
        {
            // Add over the capacity items, verify that the verified count increases each time
            AddLowPriorityTransactions(70);
            AddHighPriorityTransactions(40);

            Console.WriteLine($"VerifiedCount: {_unit.VerifiedCount}  LowPrioCount {_unit.SortedLowPrioTxCount}  HighPrioCount {_unit.SortedHighPrioTxCount}");
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(60);
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(40);
            _unit.Count.ShouldBeEquivalentTo(100);
        }

        [TestMethod]
        public void LowPriorityDoesNotPushOutHighPrority()
        {
            AddHighPriorityTransactions(70);
            AddLowPriorityTransactions(40);

            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(30);
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(70);
            _unit.Count.ShouldBeEquivalentTo(100);
        }

        [TestMethod]
        public void BlockPersistMovesTxToUnverifiedAndReverification()
        {
            AddHighPriorityTransactions(70);
            AddLowPriorityTransactions(30);

            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(70);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(30);

            var block = new Block
            {
                Transactions = _unit.GetSortedVerifiedTransactions().Take(10)
                    .Concat(_unit.GetSortedVerifiedTransactions().Where(x => x.IsLowPriority).Take(5)).ToArray()
            };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(60);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(25);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(9);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(1);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(51);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(24);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(18);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(2);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(42);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(23);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(27);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(3);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(33);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(22);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(36);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(4);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(24);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(21);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(45);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(5);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(15);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(20);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(54);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(6);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(6);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(19);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(60);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(10);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(15);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(60);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(20);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(5);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(60);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(25);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(0);
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
            AddLowPriorityTransactions(50);
            AddHighPriorityTransactions(50);

            var sortedVerifiedTxs = _unit.GetSortedVerifiedTransactions().ToList();
            // verify all 100 transactions are returned in sorted order
            sortedVerifiedTxs.Count.ShouldBeEquivalentTo(100);
            verifyTransactionsSortedDescending(sortedVerifiedTxs);

            // move all to unverified
            var block = new Block { Transactions = new Transaction[0] };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());

            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(50);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(50);

            // We can verify the order they are re-verified by reverifying 2 at a time
            while (_unit.UnVerifiedCount > 0)
            {
                _unit.GetVerifiedAndUnverifiedTransactions(out IEnumerable<Transaction> sortedVerifiedTransactions,
                    out IEnumerable<Transaction> sortedUnverifiedTransactions);
                sortedVerifiedTransactions.Count().ShouldBeEquivalentTo(0);
                var sortedUnverifiedArray = sortedUnverifiedTransactions.ToArray();
                verifyTransactionsSortedDescending(sortedUnverifiedArray);
                var maxHighPriorityTransaction = sortedUnverifiedArray.First();
                var maxLowPriorityTransaction = sortedUnverifiedArray.First(tx => tx.IsLowPriority);

                // reverify 1 high priority and 1 low priority transaction
                _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(2, Blockchain.Singleton.GetSnapshot());
                var verifiedTxs = _unit.GetSortedVerifiedTransactions().ToArray();
                verifiedTxs.Length.ShouldBeEquivalentTo(2);
                verifiedTxs[0].ShouldBeEquivalentTo(maxHighPriorityTransaction);
                verifiedTxs[1].ShouldBeEquivalentTo(maxLowPriorityTransaction);
                var blockWith2Tx = new Block { Transactions = new Transaction[2] { maxHighPriorityTransaction, maxLowPriorityTransaction } };
                // verify and remove the 2 transactions from the verified pool
                _unit.UpdatePoolForBlockPersisted(blockWith2Tx, Blockchain.Singleton.GetSnapshot());
                _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(0);
                _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(0);
            }
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(0);
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
            AddLowPriorityTransactions(100);
            VerifyCapacityThresholdForAttemptingToAddATransaction();
            AddHighPriorityTransactions(50);
            VerifyCapacityThresholdForAttemptingToAddATransaction();
            AddHighPriorityTransactions(50);
            VerifyCapacityThresholdForAttemptingToAddATransaction();
        }

        [TestMethod]
        public void CapacityTestWithUnverifiedHighProirtyTransactions()
        {
            // Verify that unverified high priority transactions will not be pushed out of the queue by incoming
            // low priority transactions

            // Fill pool with high priority transactions
            AddHighPriorityTransactions(99);

            // move all to unverified
            var block = new Block { Transactions = new Transaction[0] };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());

            _unit.CanTransactionFitInPool(CreateLowPriorityTransaction()).ShouldBeEquivalentTo(true);
            AddHighPriorityTransactions(1);
            _unit.CanTransactionFitInPool(CreateLowPriorityTransaction()).ShouldBeEquivalentTo(false);
        }

        [TestMethod]
        public void TestInvalidateAll()
        {
            AddHighPriorityTransactions(30);
            AddLowPriorityTransactions(60);
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(30);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(60);
            _unit.InvalidateAllTransactions();
            _unit.UnverifiedSortedHighPrioTxCount.ShouldBeEquivalentTo(30);
            _unit.UnverifiedSortedLowPrioTxCount.ShouldBeEquivalentTo(60);
            _unit.SortedHighPrioTxCount.ShouldBeEquivalentTo(0);
            _unit.SortedLowPrioTxCount.ShouldBeEquivalentTo(0);
        }
    }
}
