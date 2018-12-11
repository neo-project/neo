using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Neo.Persistence;

namespace Neo.Ledger
{
    internal class MemoryPool : IReadOnlyCollection<Transaction>
    {
        private class PoolItem : IComparable<PoolItem>
        {
            public readonly Transaction Transaction;
            public readonly DateTime Timestamp;
            public readonly Fixed8 FeePerByte;

            public PoolItem(Transaction tx)
            {
                Transaction = tx;
                Timestamp = DateTime.UtcNow;
                FeePerByte = Transaction.NetworkFee / Transaction.Size;
            }

            public int CompareTo(Transaction tx, Fixed8 feePerByte)
            {
                if (tx == null) return 1;
                int ret = FeePerByte.CompareTo(feePerByte);
                if (ret != 0) return ret;
                ret = Transaction.NetworkFee.CompareTo(tx.NetworkFee);
                if (ret != 0) return ret;

                return Transaction.Hash.CompareTo(tx.Hash);
            }
            
            public int CompareTo(PoolItem otherItem)
            {
                if (otherItem == null) return 1;
                return CompareTo(otherItem.Transaction, otherItem.FeePerByte);
            }
        }

        private static readonly double MaxSecondsToReverifyHighPrioTx = (double) Blockchain.SecondsPerBlock / 3;
        private static readonly double MaxSecondsToReverifyLowPrioTx = (double) Blockchain.SecondsPerBlock / 5;
        
        // These two are not expected to be hit, they are just safegaurds. 
        private static readonly double MaxSecondsToReverifyHighPrioTxPerIdle = (double) Blockchain.SecondsPerBlock / 15;
        private static readonly double MaxSecondsToReverifyLowPrioTxPerIdle = (double) Blockchain.SecondsPerBlock / 30;
        
        private readonly ReaderWriterLockSlim verifiedTxRwLock 
            = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        
        /// <summary>
        /// Store all verified unsorted transactions currently in the pool
        /// </summary>
        private readonly ConcurrentDictionary<UInt256, PoolItem> _unsortedTransactions = new ConcurrentDictionary<UInt256, PoolItem>();
        /// <summary>
        ///  Stores the verified low priority sorted transactions currently in the pool
        /// </summary>
        private readonly SortedSet<PoolItem> _sortedLowPrioTransactions = new SortedSet<PoolItem>();
        /// <summary>
        /// Stores the verified high priority sorted transactins currently in the pool
        /// </summary>
        private readonly SortedSet<PoolItem> _sortedHighPrioTransactions = new SortedSet<PoolItem>();

        /// <summary>
        /// Store the unverified transactions currently in the pool.
        ///
        /// Transactions in this data structure were valid in some prior block, but may no longer be valid.
        /// The top ones that could make it into the next block get verified and moved into the verified data structures
        /// (_unsortedTransactions, _sortedLowPrioTransactions, and _sortedHighPrioTransactions) after each block.
        /// </summary>
        private readonly ConcurrentDictionary<UInt256, PoolItem> _unverifiedTransactions = new ConcurrentDictionary<UInt256, PoolItem>();
        private readonly SortedSet<PoolItem> _unverifiedSortedHighPriorityTransactions = new SortedSet<PoolItem>();
        private readonly SortedSet<PoolItem> _unverifiedSortedLowPriorityTransactions = new SortedSet<PoolItem>();
        
        /// <summary>
        /// Total maximum capacity of transactions the pool can hold
        /// </summary>
        public int Capacity { get; }
        
        /// <summary>
        /// Total count of transactions in the pool
        /// </summary>
        public int Count => _unsortedTransactions.Count + _unverifiedTransactions.Count;

        /// <summary>
        /// Total count of verified transactions in the pool.
        /// </summary>
        public int VerifiedCount => _unsortedTransactions.Count;
        
        public MemoryPool(int capacity)
        {
            Capacity = capacity;
        }

        /// <summary>
        /// Determine whether the pool is holding this transaction and has at some point verified it.
        /// Note: The pool may not have verified it since the last block was persisted. To get only the
        ///       transactions that have been verified during this block use GetVerifiedTransactions()
        /// </summary>
        /// <param name="hash">the transaction hash</param>
        /// <returns>true if the MemoryPool contain the transaction</returns>
        public bool ContainsKey(UInt256 hash) => _unsortedTransactions.ContainsKey(hash) 
             || _unverifiedTransactions.ContainsKey(hash);
        
        public bool TryGetValue(UInt256 hash, out Transaction tx)
        {
            bool ret = _unsortedTransactions.TryGetValue(hash, out PoolItem item)
                       || _unverifiedTransactions.TryGetValue(hash, out item);
            tx = ret ? item.Transaction : null;
            return ret;
        }
        
        // Note: this isn't used in Fill during consensus, fill uses GetVerifiedTransactions()
        public IEnumerator<Transaction> GetEnumerator()
        {
            return _unsortedTransactions.Select(p => p.Value.Transaction)
                .Concat(_unverifiedTransactions.Select(p => p.Value.Transaction))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<Transaction> GetVerifiedTransactions()
        {
            return _unsortedTransactions.Select(p => p.Value.Transaction);
        }
        
        public IEnumerable<Transaction> GetSortedVerifiedTransactions()
        {
            verifiedTxRwLock.EnterReadLock();
            
            try
            {
               return _sortedHighPrioTransactions.Select(p => p.Transaction)
                        .Concat(_sortedLowPrioTransactions.Select(p => p.Transaction))
                        .ToArray();
            }
            finally
            {
                verifiedTxRwLock.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PoolItem GetLowestFeeTransaction(SortedSet<PoolItem> verifiedTxSorted, 
            SortedSet<PoolItem> unverifiedTxSorted, out SortedSet<PoolItem> sortedPool)
        {
            PoolItem minItem;
            if (unverifiedTxSorted.Count > 0)
            {
                sortedPool = unverifiedTxSorted;
                minItem = unverifiedTxSorted.Min;
            }
            else
            {
                sortedPool = null;
                minItem = null;
            }

            if (_sortedLowPrioTransactions.Count == 0)
                return minItem;
            
            PoolItem verifiedMin = verifiedTxSorted.Min;
            if (minItem != null && verifiedMin.CompareTo(minItem) >= 0) 
                return minItem;
            
            sortedPool = verifiedTxSorted;
            minItem = verifiedMin;

            return minItem;
        }

        private PoolItem GetLowestFeeTransaction(out SortedSet<PoolItem> sortedPool)
        {
            var minItem = GetLowestFeeTransaction(_sortedLowPrioTransactions, _unverifiedSortedLowPriorityTransactions,
                out sortedPool);

            if (minItem != null) return minItem;

            return GetLowestFeeTransaction(_sortedHighPrioTransactions, _unverifiedSortedHighPriorityTransactions,
                out sortedPool);
        }

        // Note: this must only be called from a single thread (the Blockchain actor)
        public bool CanTransactionFitInPool(Transaction tx)
        {
            if (Count < Capacity) return true;

            return GetLowestFeeTransaction(out _).CompareTo(tx, tx.NetworkFee / tx.Size) <= 0;
        }

        // Note: this must only be called from a single thread (the Blockchain actor)
        public bool TryAdd(UInt256 hash, Transaction tx)
        {
            var poolItem = new PoolItem(tx);
            if (!_unsortedTransactions.TryAdd(hash, poolItem)) return false;
            
            SortedSet<PoolItem> pool = tx.IsLowPriority ? _sortedLowPrioTransactions : _sortedHighPrioTransactions;
            verifiedTxRwLock.EnterWriteLock();
            try
            {
                pool.Add(poolItem);
                RemoveOverCapacity();
            }
            finally
            {
                verifiedTxRwLock.ExitWriteLock();
            }

            return _unsortedTransactions.ContainsKey(hash);
        }
        
        private void RemoveOverCapacity()
        {
            while (Count > Capacity)
            {
                PoolItem minItem = GetLowestFeeTransaction(out var sortedPool);

                _unsortedTransactions.TryRemove(minItem.Transaction.Hash, out _);    
                sortedPool.Remove(minItem);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRemoveInternal(UInt256 hash, out Transaction tx)
        {
            if (_unsortedTransactions.TryRemove(hash, out PoolItem item))
            {
                SortedSet<PoolItem> pool = item.Transaction.IsLowPriority
                    ? _sortedLowPrioTransactions : _sortedHighPrioTransactions;
                pool.Remove(item);
                tx = item.Transaction;
                return true;
            }

            tx = null;
            return false;
        }

        public void UpdatePoolForBlockPersisted(Block block, Snapshot snapshot)
        {
            verifiedTxRwLock.EnterWriteLock();
            try
            {
                // First remove the transactions verified in the block.
                foreach (Transaction tx in block.Transactions)
                {
                    if (!TryRemoveInternal(tx.Hash, out _))
                    {
                        if (_unverifiedTransactions.TryRemove(tx.Hash, out PoolItem item))
                        {
                            SortedSet<PoolItem> pool = tx.IsLowPriority
                                ? _unverifiedSortedLowPriorityTransactions
                                : _unverifiedSortedHighPriorityTransactions;
                            pool.Remove(item);
                        }
                    }
                }

                // Add all the previously verified transactions back to the unverified transactions
                foreach (PoolItem item in _sortedHighPrioTransactions)
                {
                    _unverifiedTransactions.TryAdd(item.Transaction.Hash, item);
                    _unverifiedSortedHighPriorityTransactions.Add(item);
                }

                // NOTE: This really shouldn't be necessary any more and can actually be counter-productive, since
                //       they can be re-added anyway and would incur a potential addtional verification; setting to 1000
                //       blocks for now. If transactions want to only be valid for short times they can include a script.
                var lowPriorityCutOffTime = DateTime.UtcNow.AddSeconds(-Blockchain.SecondsPerBlock * 1000);
                foreach (PoolItem item in _sortedLowPrioTransactions)
                {
                    // Expire old free transactions
                    if (item.Transaction.IsLowPriority && item.Timestamp < lowPriorityCutOffTime) continue;

                    _unverifiedTransactions.TryAdd(item.Transaction.Hash, item);
                    _unverifiedSortedLowPriorityTransactions.Add(item);
                }

                // Clear the verified transactions now, since they all must be reverified.
                _unsortedTransactions.Clear();
                _sortedHighPrioTransactions.Clear();
                _sortedLowPrioTransactions.Clear();

                // If we know about headers of future blocks, no point in verifying transactions from the unverified tx pool
                // until we get caught up.
                if (block.Index < Blockchain.Singleton.HeaderHeight)
                    return;

                ReverifyTransactions(_sortedHighPrioTransactions, _unverifiedSortedHighPriorityTransactions,
                    Settings.Default.MaxTransactionsPerBlock, MaxSecondsToReverifyHighPrioTx, snapshot);
                ReverifyTransactions(_sortedLowPrioTransactions, _unverifiedSortedLowPriorityTransactions,
                    Settings.Default.MaxFreeTransactionsPerBlock, MaxSecondsToReverifyLowPrioTx, snapshot);
            }
            finally
            {
                verifiedTxRwLock.ExitWriteLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReverifyTransactions(SortedSet<PoolItem> verifiedSortedTxPool,
            SortedSet<PoolItem> unverifiedSortedTxPool, int count, double secondsTimeout, Snapshot snapshot)
        {
            DateTime reverifyCutOffTimeStamp = DateTime.UtcNow.AddSeconds(secondsTimeout);

            List<PoolItem> reverifiedItems = new List<PoolItem>(count);
            foreach (PoolItem item in unverifiedSortedTxPool.Reverse().Take(count).ToArray())
            {
                // Re-verify the top fee max high priority transactions that can be verified in a block
                if (item.Transaction.Verify(snapshot, _unsortedTransactions.Select(p => p.Value.Transaction)))
                    reverifiedItems.Add(item);
                if (DateTime.UtcNow > reverifyCutOffTimeStamp) break;
            }

            
            verifiedTxRwLock.EnterWriteLock();
            try
            {
                foreach (PoolItem item in reverifiedItems)
                {
                    verifiedSortedTxPool.Add(item);
                }
            }
            finally
            {
                verifiedTxRwLock.ExitWriteLock();
            }
            
            
            foreach (PoolItem item in reverifiedItems)
            {
                _unsortedTransactions.TryAdd(item.Transaction.Hash, item);
                
                _unverifiedTransactions.TryRemove(item.Transaction.Hash, out _);
                unverifiedSortedTxPool.Remove(item);
            }

            return reverifiedItems.Count;
        }

        /// <summary>
        /// Reverify up to a given maximum count of transactions. Verifies less at a time once the max that can be
        /// persisted per block has been reached. 
        /// </summary>
        /// <param name="maxToVerify">Max transactions to reverify, the value passed should be >=2. If 1 is passed it
        ///                           will still potentially use 2.</param>
        /// <param name="snapshot">The snapshot to use for verifying.</param>
        /// <returns>true if more unsorted messages exist, otherwise false</returns>
        public bool ReVerifyTopUnverifiedTransactionsIfNeeded(int maxToVerify, Snapshot snapshot)
        {
            if (Blockchain.Singleton.Height < Blockchain.Singleton.HeaderHeight)
                return false;
            
            if (_unverifiedSortedHighPriorityTransactions.Count > 0)
            {
                // Always leave at least 1 tx for low priority tx
                int verifyCount = _sortedHighPrioTransactions.Count > Settings.Default.MaxTransactionsPerBlock || maxToVerify == 1
                    ? 1 : maxToVerify - 1; 
                maxToVerify -= ReverifyTransactions(_sortedHighPrioTransactions, _unverifiedSortedHighPriorityTransactions,
                    verifyCount, MaxSecondsToReverifyHighPrioTxPerIdle, snapshot);

                if (maxToVerify == 0) maxToVerify++;
            }

            if (_unverifiedSortedLowPriorityTransactions.Count > 0)
            {
                int verifyCount = _sortedLowPrioTransactions.Count > Settings.Default.MaxFreeTransactionsPerBlock
                    ? 1 : maxToVerify;
                ReverifyTransactions(_sortedLowPrioTransactions, _unverifiedSortedLowPriorityTransactions,
                    verifyCount, MaxSecondsToReverifyLowPrioTxPerIdle, snapshot);
            }

            return _unverifiedTransactions.Count > 0;
        }
    }
}
