using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract.Native;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.Ledger
{
    internal class TestIMemoryPoolTxObserverPlugin : Plugin, IMemoryPoolTxObserverPlugin
    {
        public override void Configure() { }
        public void TransactionAdded(Transaction tx) { }
        public void TransactionsRemoved(MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions) { }
    }

    [TestClass]
    public class UT_MemoryPool
    {
        private const byte Prefix_MaxTransactionsPerBlock = 23;
        private const byte Prefix_FeePerByte = 10;
        private MemoryPool _unit;
        private MemoryPool _unit2;
        private TestIMemoryPoolTxObserverPlugin plugin;

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
            _unit.Capacity.Should().Be(100);

            _unit.VerifiedCount.Should().Be(0);
            _unit.UnVerifiedCount.Should().Be(0);
            _unit.Count.Should().Be(0);
            _unit2 = new MemoryPool(TheNeoSystem, 0);
            plugin = new TestIMemoryPoolTxObserverPlugin();
        }

        [TestCleanup]
        public void CleanUp()
        {
            Plugin.TxObserverPlugins.Remove(plugin);
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
            mock.Object.Cosigners = new Cosigner[0];
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

        private Transaction CreateTransaction(long fee = -1)
        {
            if (fee != -1)
                return CreateTransactionWithFee(fee);
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

        private void AddTransaction(Transaction txToAdd)
        {
            _unit.TryAdd(txToAdd.Hash, txToAdd);
        }

        [TestMethod]
        public void CapacityTest()
        {
            // Add over the capacity items, verify that the verified count increases each time
            AddTransactions(101);

            Console.WriteLine($"VerifiedCount: {_unit.VerifiedCount} Count {_unit.SortedTxCount}");

            _unit.SortedTxCount.Should().Be(100);
            _unit.VerifiedCount.Should().Be(100);
            _unit.UnVerifiedCount.Should().Be(0);
            _unit.Count.Should().Be(100);
        }

        [TestMethod]
        public void BlockPersistMovesTxToUnverifiedAndReverification()
        {
            AddTransactions(70);

            _unit.SortedTxCount.Should().Be(70);

            var block = new Block
            {
                Transactions = _unit.GetSortedVerifiedTransactions().Take(10)
                    .Concat(_unit.GetSortedVerifiedTransactions().Take(5)).ToArray()
            };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());
            _unit.InvalidateVerifiedTransactions();
            _unit.SortedTxCount.Should().Be(0);
            _unit.UnverifiedSortedTxCount.Should().Be(60);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.Should().Be(10);
            _unit.UnverifiedSortedTxCount.Should().Be(50);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.Should().Be(20);
            _unit.UnverifiedSortedTxCount.Should().Be(40);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.Should().Be(30);
            _unit.UnverifiedSortedTxCount.Should().Be(30);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.Should().Be(40);
            _unit.UnverifiedSortedTxCount.Should().Be(20);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.Should().Be(50);
            _unit.UnverifiedSortedTxCount.Should().Be(10);

            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, Blockchain.Singleton.GetSnapshot());
            _unit.SortedTxCount.Should().Be(60);
            _unit.UnverifiedSortedTxCount.Should().Be(0);
        }

        private void VerifyTransactionsSortedDescending(IEnumerable<Transaction> transactions)
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
            sortedVerifiedTxs.Count.Should().Be(100);
            VerifyTransactionsSortedDescending(sortedVerifiedTxs);

            // move all to unverified
            var block = new Block { Transactions = new Transaction[0] };
            _unit.UpdatePoolForBlockPersisted(block, Blockchain.Singleton.GetSnapshot());
            _unit.InvalidateVerifiedTransactions();
            _unit.SortedTxCount.Should().Be(0);
            _unit.UnverifiedSortedTxCount.Should().Be(100);

            // We can verify the order they are re-verified by reverifying 2 at a time
            while (_unit.UnVerifiedCount > 0)
            {
                _unit.GetVerifiedAndUnverifiedTransactions(out var sortedVerifiedTransactions, out var sortedUnverifiedTransactions);
                sortedVerifiedTransactions.Count().Should().Be(0);
                var sortedUnverifiedArray = sortedUnverifiedTransactions.ToArray();
                VerifyTransactionsSortedDescending(sortedUnverifiedArray);
                var maxTransaction = sortedUnverifiedArray.First();
                var minTransaction = sortedUnverifiedArray.Last();

                // reverify 1 high priority and 1 low priority transaction
                _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(1, Blockchain.Singleton.GetSnapshot());
                var verifiedTxs = _unit.GetSortedVerifiedTransactions().ToArray();
                verifiedTxs.Length.Should().Be(1);
                verifiedTxs[0].Should().BeEquivalentTo(maxTransaction);
                var blockWith2Tx = new Block { Transactions = new[] { maxTransaction, minTransaction } };
                // verify and remove the 2 transactions from the verified pool
                _unit.UpdatePoolForBlockPersisted(blockWith2Tx, Blockchain.Singleton.GetSnapshot());
                _unit.InvalidateVerifiedTransactions();
                _unit.SortedTxCount.Should().Be(0);
            }
            _unit.UnverifiedSortedTxCount.Should().Be(0);
        }

        void VerifyCapacityThresholdForAttemptingToAddATransaction()
        {
            var sortedVerified = _unit.GetSortedVerifiedTransactions().ToArray();

            var txBarelyWontFit = CreateTransactionWithFee(sortedVerified.Last().NetworkFee - 1);
            _unit.CanTransactionFitInPool(txBarelyWontFit).Should().Be(false);
            var txBarelyFits = CreateTransactionWithFee(sortedVerified.Last().NetworkFee + 1);
            _unit.CanTransactionFitInPool(txBarelyFits).Should().Be(true);
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

            _unit.CanTransactionFitInPool(CreateTransaction()).Should().Be(true);
            AddTransactions(1);
            _unit.CanTransactionFitInPool(CreateTransactionWithFee(0)).Should().Be(false);
        }

        [TestMethod]
        public void TestInvalidateAll()
        {
            AddTransactions(30);

            _unit.UnverifiedSortedTxCount.Should().Be(0);
            _unit.SortedTxCount.Should().Be(30);
            _unit.InvalidateAllTransactions();
            _unit.UnverifiedSortedTxCount.Should().Be(30);
            _unit.SortedTxCount.Should().Be(0);
        }

        [TestMethod]
        public void TestContainsKey()
        {
            AddTransactions(10);

            var txToAdd = CreateTransaction();
            _unit.TryAdd(txToAdd.Hash, txToAdd);
            _unit.ContainsKey(txToAdd.Hash).Should().BeTrue();
            _unit.InvalidateVerifiedTransactions();
            _unit.ContainsKey(txToAdd.Hash).Should().BeTrue();
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            AddTransactions(10);
            _unit.InvalidateVerifiedTransactions();
            IEnumerator<Transaction> enumerator = _unit.GetEnumerator();
            foreach (Transaction tx in _unit)
            {
                enumerator.MoveNext();
                enumerator.Current.Should().BeSameAs(tx);
            }
        }

        [TestMethod]
        public void TestIEnumerableGetEnumerator()
        {
            AddTransactions(10);
            _unit.InvalidateVerifiedTransactions();
            IEnumerable enumerable = _unit;
            var enumerator = enumerable.GetEnumerator();
            foreach (Transaction tx in _unit)
            {
                enumerator.MoveNext();
                enumerator.Current.Should().BeSameAs(tx);
            }
        }

        [TestMethod]
        public void TestGetVerifiedTransactions()
        {
            var tx1 = CreateTransaction();
            var tx2 = CreateTransaction();
            _unit.TryAdd(tx1.Hash, tx1);
            _unit.InvalidateVerifiedTransactions();
            _unit.TryAdd(tx2.Hash, tx2);
            IEnumerable<Transaction> enumerable = _unit.GetVerifiedTransactions();
            enumerable.Count().Should().Be(1);
            var enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();
            enumerator.Current.Should().BeSameAs(tx2);
        }

        [TestMethod]
        public void TestReVerifyTopUnverifiedTransactionsIfNeeded()
        {
            NeoSystem TheNeoSystem = TestBlockchain.InitializeMockNeoSystem();
            var s = Blockchain.Singleton.Height;
            _unit = new MemoryPool(TheNeoSystem, 600);
            _unit.LoadPolicy(TestBlockchain.GetStore().GetSnapshot());
            AddTransaction(CreateTransaction(100000001));
            AddTransaction(CreateTransaction(100000001));
            AddTransaction(CreateTransaction(100000001));
            AddTransaction(CreateTransaction(1));
            _unit.VerifiedCount.Should().Be(4);
            _unit.UnVerifiedCount.Should().Be(0);

            _unit.InvalidateVerifiedTransactions();
            _unit.VerifiedCount.Should().Be(0);
            _unit.UnVerifiedCount.Should().Be(4);

            AddTransactions(511); // Max per block currently is 512
            _unit.VerifiedCount.Should().Be(511);
            _unit.UnVerifiedCount.Should().Be(4);

            var result = _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(1, Blockchain.Singleton.GetSnapshot());
            result.Should().BeTrue();
            _unit.VerifiedCount.Should().Be(512);
            _unit.UnVerifiedCount.Should().Be(3);

            result = _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(2, Blockchain.Singleton.GetSnapshot());
            result.Should().BeTrue();
            _unit.VerifiedCount.Should().Be(514);
            _unit.UnVerifiedCount.Should().Be(1);

            result = _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(3, Blockchain.Singleton.GetSnapshot());
            result.Should().BeFalse();
            _unit.VerifiedCount.Should().Be(515);
            _unit.UnVerifiedCount.Should().Be(0);
        }

        [TestMethod]
        public void TestTryAdd()
        {
            var tx1 = CreateTransaction();
            _unit.TryAdd(tx1.Hash, tx1).Should().BeTrue();
            _unit.TryAdd(tx1.Hash, tx1).Should().BeFalse();
            _unit2.TryAdd(tx1.Hash, tx1).Should().BeFalse();
        }

        [TestMethod]
        public void TestTryGetValue()
        {
            var tx1 = CreateTransaction();
            _unit.TryAdd(tx1.Hash, tx1);
            _unit.TryGetValue(tx1.Hash, out Transaction tx).Should().BeTrue();
            tx.Should().BeEquivalentTo(tx1);

            _unit.InvalidateVerifiedTransactions();
            _unit.TryGetValue(tx1.Hash, out tx).Should().BeTrue();
            tx.Should().BeEquivalentTo(tx1);

            var tx2 = CreateTransaction();
            _unit.TryGetValue(tx2.Hash, out tx).Should().BeFalse();
        }

        [TestMethod]
        public void TestUpdatePoolForBlockPersisted()
        {
            var mockSnapshot = new Mock<Snapshot>();
            byte[] transactionsPerBlock = { 0x18, 0x00, 0x00, 0x00 }; // 24
            byte[] feePerByte = { 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00 }; // 1048576
            StorageItem item1 = new StorageItem
            {
                Value = transactionsPerBlock
            };
            StorageItem item2 = new StorageItem
            {
                Value = feePerByte
            };
            var myDataCache = new MyDataCache<StorageKey, StorageItem>();
            var key1 = CreateStorageKey(Prefix_MaxTransactionsPerBlock);
            var key2 = CreateStorageKey(Prefix_FeePerByte);
            key1.ScriptHash = NativeContract.Policy.Hash;
            key2.ScriptHash = NativeContract.Policy.Hash;
            myDataCache.Add(key1, item1);
            myDataCache.Add(key2, item2);
            mockSnapshot.SetupGet(p => p.Storages).Returns(myDataCache);

            var tx1 = CreateTransaction();
            var tx2 = CreateTransaction();
            Transaction[] transactions = { tx1, tx2 };
            _unit.TryAdd(tx1.Hash, tx1);

            var block = new Block { Transactions = transactions };

            _unit.UnVerifiedCount.Should().Be(0);
            _unit.VerifiedCount.Should().Be(1);

            _unit.UpdatePoolForBlockPersisted(block, mockSnapshot.Object);

            _unit.UnVerifiedCount.Should().Be(0);
            _unit.VerifiedCount.Should().Be(0);
        }

        public StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = null,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            if (key != null)
                Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);
            return storageKey;
        }
    }

    public class MyDataCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly TValue _defaultValue;

        public MyDataCache()
        {
            _defaultValue = null;
        }

        public MyDataCache(TValue defaultValue)
        {
            this._defaultValue = defaultValue;
        }
        public override void DeleteInternal(TKey key)
        {
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            Add(key, value);
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return Enumerable.Empty<KeyValuePair<TKey, TValue>>();
        }

        protected override TValue GetInternal(TKey key)
        {
            return TryGet(key);
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return _defaultValue;
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
        }
    }
}
