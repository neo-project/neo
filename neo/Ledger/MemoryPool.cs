using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Util.Internal;
using Neo.Network.P2P;
using Neo.Persistence;
using Neo.Plugins;

namespace Neo.Ledger
{
    public class MemoryPool : IReadOnlyCollection<Transaction>
    {
        // Allow a reverified transaction to be rebroadcasted if it has been this many block times since last broadcast.
        private const int BlocksTillRebroadcastLowPriorityPoolTx = 30;
        private const int BlocksTillRebroadcastHighPriorityPoolTx = 10;
        private int RebroadcastMultiplierThreshold => Capacity / 10;

        private static readonly double MaxSecondsToReverifyHighPrioTx = (double)Blockchain.SecondsPerBlock / 3;
        private static readonly double MaxSecondsToReverifyLowPrioTx = (double)Blockchain.SecondsPerBlock / 5;

        // These two are not expected to be hit, they are just safegaurds.
        private static readonly double MaxSecondsToReverifyHighPrioTxPerIdle = (double)Blockchain.SecondsPerBlock / 15;
        private static readonly double MaxSecondsToReverifyLowPrioTxPerIdle = (double)Blockchain.SecondsPerBlock / 30;

        private readonly NeoSystem _system;

        //
        /// <summary>
        /// Guarantees consistency of the pool data structures.
        ///
        /// Note: The data structures are only modified from the `Blockchain` actor; so operations guaranteed to be
        ///       performed by the blockchain actor do not need to acquire the read lock; they only need the write
        ///       lock for write operations.
        /// </summary>
        private readonly ReaderWriterLockSlim _txRwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Store all verified unsorted transactions currently in the pool.
        /// </summary>
        private readonly Dictionary<UInt256, PoolItem> _unsortedTransactions = new Dictionary<UInt256, PoolItem>();
        /// <summary>
        /// Stores the verified high priority sorted transactins currently in the pool.
        /// </summary>
        private readonly SortedSet<PoolItem> _sortedHighPrioTransactions = new SortedSet<PoolItem>();
        /// <summary>
        ///  Stores the verified low priority sorted transactions currently in the pool.
        /// </summary>
        private readonly SortedSet<PoolItem> _sortedLowPrioTransactions = new SortedSet<PoolItem>();

        /// <summary>
        /// Store the unverified transactions currently in the pool.
        ///
        /// Transactions in this data structure were valid in some prior block, but may no longer be valid.
        /// The top ones that could make it into the next block get verified and moved into the verified data structures
        /// (_unsortedTransactions, _sortedLowPrioTransactions, and _sortedHighPrioTransactions) after each block.
        /// </summary>
        private readonly Dictionary<UInt256, PoolItem> _unverifiedTransactions = new Dictionary<UInt256, PoolItem>();
        private readonly SortedSet<PoolItem> _unverifiedSortedHighPriorityTransactions = new SortedSet<PoolItem>();
        private readonly SortedSet<PoolItem> _unverifiedSortedLowPriorityTransactions = new SortedSet<PoolItem>();

        // Internal methods to aid in unit testing
        internal int SortedHighPrioTxCount => _sortedHighPrioTransactions.Count;
        internal int SortedLowPrioTxCount => _sortedLowPrioTransactions.Count;
        internal int UnverifiedSortedHighPrioTxCount => _unverifiedSortedHighPriorityTransactions.Count;
        internal int UnverifiedSortedLowPrioTxCount => _unverifiedSortedLowPriorityTransactions.Count;

        private int _maxTxPerBlock;
        private int _maxLowPriorityTxPerBlock;

        /// <summary>
        /// Total maximum capacity of transactions the pool can hold.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Total count of transactions in the pool.
        /// </summary>
        public int Count
        {
            get
            {
                _txRwLock.EnterReadLock();
                try
                {
                    return _unsortedTransactions.Count + _unverifiedTransactions.Count;
                }
                finally
                {
                    _txRwLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Total count of verified transactions in the pool.
        /// </summary>
        public int VerifiedCount => _unsortedTransactions.Count; // read of 32 bit type is atomic (no lock)

        public int UnVerifiedCount => _unverifiedTransactions.Count;

        public MemoryPool(NeoSystem system, int capacity)
        {
            _system = system;
            Capacity = capacity;
            LoadMaxTxLimitsFromPolicyPlugins();
        }

        public void LoadMaxTxLimitsFromPolicyPlugins()
        {
            _maxTxPerBlock = int.MaxValue;
            _maxLowPriorityTxPerBlock = int.MaxValue;
            foreach (IPolicyPlugin plugin in Plugin.Policies)
            {
                _maxTxPerBlock = Math.Min(_maxTxPerBlock, plugin.MaxTxPerBlock);
                _maxLowPriorityTxPerBlock = Math.Min(_maxLowPriorityTxPerBlock, plugin.MaxLowPriorityTxPerBlock);
            }
        }

        /// <summary>
        /// Determine whether the pool is holding this transaction and has at some point verified it.
        /// Note: The pool may not have verified it since the last block was persisted. To get only the
        ///       transactions that have been verified during this block use GetVerifiedTransactions()
        /// </summary>
        /// <param name="hash">the transaction hash</param>
        /// <returns>true if the MemoryPool contain the transaction</returns>
        public bool ContainsKey(UInt256 hash)
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _unsortedTransactions.ContainsKey(hash)
                       || _unverifiedTransactions.ContainsKey(hash);
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        public bool TryGetValue(UInt256 hash, out Transaction tx)
        {
            _txRwLock.EnterReadLock();
            try
            {
                bool ret = _unsortedTransactions.TryGetValue(hash, out PoolItem item)
                           || _unverifiedTransactions.TryGetValue(hash, out item);
                tx = ret ? item.Tx : null;
                return ret;
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        // Note: This isn't used in Fill during consensus, fill uses GetSortedVerifiedTransactions()
        public IEnumerator<Transaction> GetEnumerator()
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _unsortedTransactions.Select(p => p.Value.Tx)
                    .Concat(_unverifiedTransactions.Select(p => p.Value.Tx))
                    .ToList()
                    .GetEnumerator();
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<Transaction> GetVerifiedTransactions()
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _unsortedTransactions.Select(p => p.Value.Tx).ToArray();
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        public void GetVerifiedAndUnverifiedTransactions(out IEnumerable<Transaction> verifiedTransactions,
            out IEnumerable<Transaction> unverifiedTransactions)
        {
            _txRwLock.EnterReadLock();
            try
            {
                verifiedTransactions = _sortedHighPrioTransactions.Reverse().Select(p => p.Tx)
                    .Concat(_sortedLowPrioTransactions.Reverse().Select(p => p.Tx)).ToArray();
                unverifiedTransactions = _unverifiedSortedHighPriorityTransactions.Reverse().Select(p => p.Tx)
                    .Concat(_unverifiedSortedLowPriorityTransactions.Reverse().Select(p => p.Tx)).ToArray();
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        public IEnumerable<Transaction> GetSortedVerifiedTransactions()
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _sortedHighPrioTransactions.Reverse().Select(p => p.Tx)
                         .Concat(_sortedLowPrioTransactions.Reverse().Select(p => p.Tx))
                         .ToArray();
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PoolItem GetLowestFeeTransaction(SortedSet<PoolItem> verifiedTxSorted,
            SortedSet<PoolItem> unverifiedTxSorted, out SortedSet<PoolItem> sortedPool)
        {
            PoolItem minItem = unverifiedTxSorted.Min;
            sortedPool = minItem != null ? unverifiedTxSorted : null;

            PoolItem verifiedMin = verifiedTxSorted.Min;
            if (verifiedMin == null) return minItem;

            if (minItem != null && verifiedMin.CompareTo(minItem) >= 0)
                return minItem;

            sortedPool = verifiedTxSorted;
            minItem = verifiedMin;

            return minItem;
        }

        private PoolItem GetLowestFeeTransaction(out Dictionary<UInt256, PoolItem> unsortedTxPool, out SortedSet<PoolItem> sortedPool)
        {
            var minItem = GetLowestFeeTransaction(_sortedLowPrioTransactions, _unverifiedSortedLowPriorityTransactions,
                out sortedPool);

            if (minItem != null)
            {
                unsortedTxPool = Object.ReferenceEquals(sortedPool, _unverifiedSortedLowPriorityTransactions)
                    ? _unverifiedTransactions : _unsortedTransactions;
                return minItem;
            }

            try
            {
                return GetLowestFeeTransaction(_sortedHighPrioTransactions, _unverifiedSortedHighPriorityTransactions,
                    out sortedPool);
            }
            finally
            {
                unsortedTxPool = Object.ReferenceEquals(sortedPool, _unverifiedSortedHighPriorityTransactions)
                    ? _unverifiedTransactions : _unsortedTransactions;
            }
        }

        // Note: this must only be called from a single thread (the Blockchain actor)
        internal bool CanTransactionFitInPool(Transaction tx)
        {
            if (Count < Capacity) return true;

            return GetLowestFeeTransaction(out _, out _).CompareTo(tx) <= 0;
        }

        /// <summary>
        /// Adds an already verified transaction to the memory pool.
        ///
        /// Note: This must only be called from a single thread (the Blockchain actor). To add a transaction to the pool
        ///       tell the Blockchain actor about the transaction.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        internal bool TryAdd(UInt256 hash, Transaction tx)
        {
            var poolItem = new PoolItem(tx);

            if (_unsortedTransactions.ContainsKey(hash)) return false;

            List<Transaction> removedTransactions = null;
            _txRwLock.EnterWriteLock();
            try
            {
                _unsortedTransactions.Add(hash, poolItem);

                SortedSet<PoolItem> pool = tx.IsLowPriority ? _sortedLowPrioTransactions : _sortedHighPrioTransactions;
                pool.Add(poolItem);
                if (Count > Capacity)
                    removedTransactions = RemoveOverCapacity();
            }
            finally
            {
                _txRwLock.ExitWriteLock();
            }

            foreach (IMemoryPoolTxObserverPlugin plugin in Plugin.TxObserverPlugins)
            {
                plugin.TransactionAdded(poolItem.Tx);
                if (removedTransactions != null)
                    plugin.TransactionsRemoved(MemoryPoolTxRemovalReason.CapacityExceeded, removedTransactions);
            }

            return _unsortedTransactions.ContainsKey(hash);
        }

        private List<Transaction> RemoveOverCapacity()
        {
            List<Transaction> removedTransactions = new List<Transaction>();
            do
            {
                PoolItem minItem = GetLowestFeeTransaction(out var unsortedPool, out var sortedPool);

                unsortedPool.Remove(minItem.Tx.Hash);
                sortedPool.Remove(minItem);
                removedTransactions.Add(minItem.Tx);
            } while (Count > Capacity);

            return removedTransactions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRemoveVerified(UInt256 hash, out PoolItem item)
        {
            if (!_unsortedTransactions.TryGetValue(hash, out item))
                return false;

            _unsortedTransactions.Remove(hash);
            SortedSet<PoolItem> pool = item.Tx.IsLowPriority
                ? _sortedLowPrioTransactions : _sortedHighPrioTransactions;
            pool.Remove(item);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryRemoveUnVerified(UInt256 hash, out PoolItem item)
        {
            if (!_unverifiedTransactions.TryGetValue(hash, out item))
                return false;

            _unverifiedTransactions.Remove(hash);
            SortedSet<PoolItem> pool = item.Tx.IsLowPriority
                ? _unverifiedSortedLowPriorityTransactions : _unverifiedSortedHighPriorityTransactions;
            pool.Remove(item);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateVerifiedTransactions()
        {
            foreach (PoolItem item in _sortedHighPrioTransactions)
            {
                if (_unverifiedTransactions.TryAdd(item.Tx.Hash, item))
                    _unverifiedSortedHighPriorityTransactions.Add(item);
            }

            foreach (PoolItem item in _sortedLowPrioTransactions)
            {
                if (_unverifiedTransactions.TryAdd(item.Tx.Hash, item))
                    _unverifiedSortedLowPriorityTransactions.Add(item);
            }

            // Clear the verified transactions now, since they all must be reverified.
            _unsortedTransactions.Clear();
            _sortedHighPrioTransactions.Clear();
            _sortedLowPrioTransactions.Clear();
        }

        // Note: this must only be called from a single thread (the Blockchain actor)
        internal void UpdatePoolForBlockPersisted(Block block, Snapshot snapshot)
        {
            _txRwLock.EnterWriteLock();
            try
            {
                // First remove the transactions verified in the block.
                foreach (Transaction tx in block.Transactions)
                {
                    if (TryRemoveVerified(tx.Hash, out _)) continue;
                    TryRemoveUnVerified(tx.Hash, out _);
                }

                // Add all the previously verified transactions back to the unverified transactions
                InvalidateVerifiedTransactions();
            }
            finally
            {
                _txRwLock.ExitWriteLock();
            }

            // If we know about headers of future blocks, no point in verifying transactions from the unverified tx pool
            // until we get caught up.
            if (block.Index > 0 && block.Index < Blockchain.Singleton.HeaderHeight)
                return;

            if (Plugin.Policies.Count == 0)
                return;

            LoadMaxTxLimitsFromPolicyPlugins();

            ReverifyTransactions(_sortedHighPrioTransactions, _unverifiedSortedHighPriorityTransactions,
                _maxTxPerBlock, MaxSecondsToReverifyHighPrioTx, snapshot);
            ReverifyTransactions(_sortedLowPrioTransactions, _unverifiedSortedLowPriorityTransactions,
                _maxLowPriorityTxPerBlock, MaxSecondsToReverifyLowPrioTx, snapshot);
        }

        internal void InvalidateAllTransactions()
        {
            _txRwLock.EnterWriteLock();
            try
            {
                InvalidateVerifiedTransactions();
            }
            finally
            {
                _txRwLock.ExitWriteLock();
            }
        }

        private int ReverifyTransactions(SortedSet<PoolItem> verifiedSortedTxPool,
            SortedSet<PoolItem> unverifiedSortedTxPool, int count, double secondsTimeout, Snapshot snapshot)
        {
            DateTime reverifyCutOffTimeStamp = DateTime.UtcNow.AddSeconds(secondsTimeout);
            List<PoolItem> reverifiedItems = new List<PoolItem>(count);
            List<PoolItem> invalidItems = new List<PoolItem>();

            // Since unverifiedSortedTxPool is ordered in an ascending manner, we take from the end.
            foreach (PoolItem item in unverifiedSortedTxPool.Reverse().Take(count))
            {
                if (item.Tx.Verify(snapshot, _unsortedTransactions.Select(p => p.Value.Tx)))
                    reverifiedItems.Add(item);
                else // Transaction no longer valid -- it will be removed from unverifiedTxPool.
                    invalidItems.Add(item);

                if (DateTime.UtcNow > reverifyCutOffTimeStamp) break;
            }

            _txRwLock.EnterWriteLock();
            try
            {
                int blocksTillRebroadcast = Object.ReferenceEquals(unverifiedSortedTxPool, _sortedHighPrioTransactions)
                    ? BlocksTillRebroadcastHighPriorityPoolTx : BlocksTillRebroadcastLowPriorityPoolTx;

                if (Count > RebroadcastMultiplierThreshold)
                    blocksTillRebroadcast = blocksTillRebroadcast * Count / RebroadcastMultiplierThreshold;

                var rebroadcastCutOffTime = DateTime.UtcNow.AddSeconds(
                    -Blockchain.SecondsPerBlock * blocksTillRebroadcast);
                foreach (PoolItem item in reverifiedItems)
                {
                    if (_unsortedTransactions.TryAdd(item.Tx.Hash, item))
                    {
                        verifiedSortedTxPool.Add(item);

                        if (item.LastBroadcastTimestamp < rebroadcastCutOffTime)
                        {
                            _system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = item.Tx }, _system.Blockchain);
                            item.LastBroadcastTimestamp = DateTime.UtcNow;
                        }
                    }

                    _unverifiedTransactions.Remove(item.Tx.Hash);
                    unverifiedSortedTxPool.Remove(item);
                }

                foreach (PoolItem item in invalidItems)
                {
                    _unverifiedTransactions.Remove(item.Tx.Hash);
                    unverifiedSortedTxPool.Remove(item);
                }
            }
            finally
            {
                _txRwLock.ExitWriteLock();
            }

            var invalidTransactions = invalidItems.Select(p => p.Tx).ToArray();
            foreach (IMemoryPoolTxObserverPlugin plugin in Plugin.TxObserverPlugins)
                plugin.TransactionsRemoved(MemoryPoolTxRemovalReason.NoLongerValid, invalidTransactions);

            return reverifiedItems.Count;
        }

        /// <summary>
        /// Reverify up to a given maximum count of transactions. Verifies less at a time once the max that can be
        /// persisted per block has been reached.
        ///
        /// Note: this must only be called from a single thread (the Blockchain actor)
        /// </summary>
        /// <param name="maxToVerify">Max transactions to reverify, the value passed should be >=2. If 1 is passed it
        ///                           will still potentially use 2.</param>
        /// <param name="snapshot">The snapshot to use for verifying.</param>
        /// <returns>true if more unsorted messages exist, otherwise false</returns>
        internal bool ReVerifyTopUnverifiedTransactionsIfNeeded(int maxToVerify, Snapshot snapshot)
        {
            if (Blockchain.Singleton.Height < Blockchain.Singleton.HeaderHeight)
                return false;

            if (_unverifiedSortedHighPriorityTransactions.Count > 0)
            {
                // Always leave at least 1 tx for low priority tx
                int verifyCount = _sortedHighPrioTransactions.Count > _maxTxPerBlock || maxToVerify == 1
                    ? 1 : maxToVerify - 1;
                maxToVerify -= ReverifyTransactions(_sortedHighPrioTransactions, _unverifiedSortedHighPriorityTransactions,
                    verifyCount, MaxSecondsToReverifyHighPrioTxPerIdle, snapshot);

                if (maxToVerify == 0) maxToVerify++;
            }

            if (_unverifiedSortedLowPriorityTransactions.Count > 0)
            {
                int verifyCount = _sortedLowPrioTransactions.Count > _maxLowPriorityTxPerBlock
                    ? 1 : maxToVerify;
                ReverifyTransactions(_sortedLowPrioTransactions, _unverifiedSortedLowPriorityTransactions,
                    verifyCount, MaxSecondsToReverifyLowPrioTxPerIdle, snapshot);
            }

            return _unverifiedTransactions.Count > 0;
        }
    }
}
