using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Neo.Persistence;

namespace Neo.Ledger
{
    internal class MemoryPool : IReadOnlyCollection<Transaction>
    {
        private class PoolItem : IComparable
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
            
            public int CompareTo(object obj)
            {
                if (!(obj is PoolItem otherItem)) return 1;
                return CompareTo(otherItem.Transaction, otherItem.FeePerByte);
            }
        }

        private static readonly double MaxSecondsToReverifyHighPrioTx = (double) Blockchain.SecondsPerBlock / 3;
        private static readonly double MaxSecondsToReverifyLowPrioTx = (double) Blockchain.SecondsPerBlock / 5;
        
        // These two are not expected to be hit, they are just safegaurds. 
        private static readonly double MaxSecondsToReverifyHighPrioTxPerIdle = (double) Blockchain.SecondsPerBlock / 15;
        private static readonly double MaxSecondsToReverifyLowPrioTxPerIdle = (double) Blockchain.SecondsPerBlock / 30;
        
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
        
        private int MaxHighPriorityTxsPerBlock => Settings.Default.MaxTransactionsPerBlock 
            - Settings.Default.MaxFreeTransactionsPerBlock;
        
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
        
        public bool TryGetUnverified(UInt256 hash, out Transaction tx)
        {
            bool ret = _unverifiedTransactions.TryGetValue(hash, out PoolItem item);
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
            IEnumerator verifiedTxEnumerator = _unsortedTransactions.Select(p => p.Value.Transaction).GetEnumerator();
            while (verifiedTxEnumerator.MoveNext())
                yield return (Transaction) verifiedTxEnumerator.Current;
        }

        private PoolItem GetLowestFeeTransaction(out SortedSet<PoolItem> sortedPool)
        {
            PoolItem minItem = null;
            sortedPool = null;

            if (_unverifiedSortedLowPriorityTransactions.Count > 0)
            {
                sortedPool = _unverifiedSortedLowPriorityTransactions;
                minItem = _unverifiedSortedLowPriorityTransactions.Min;
            }

            if (_sortedLowPrioTransactions.Count > 0)
            {
                PoolItem verifiedMin = _sortedLowPrioTransactions.Min;
                if (minItem == null || verifiedMin.CompareTo(minItem) < 0)
                {
                    sortedPool = _sortedLowPrioTransactions;
                    minItem = verifiedMin;
                }
            }

            if (minItem != null) return minItem;

            if (_unverifiedSortedHighPriorityTransactions.Count > 0)
            {
                sortedPool = _unverifiedSortedHighPriorityTransactions;
                minItem = _unverifiedSortedHighPriorityTransactions.Min;
            }

            if (_sortedHighPrioTransactions.Count > 0)
            {
                PoolItem verifiedMin = _sortedHighPrioTransactions.Min;
                if (minItem == null || verifiedMin.CompareTo(minItem) < 0)
                {
                    minItem = verifiedMin;
                    sortedPool = _sortedHighPrioTransactions;
                }
            }

            return minItem;
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
            pool.Add(poolItem);
            RemoveOverCapacity();
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
            // First remove the transactions verified in the block.
            foreach (Transaction tx in block.Transactions)
            {
                if (!TryRemoveInternal(tx.Hash, out _))
                {
                    if (_unverifiedTransactions.TryRemove(tx.Hash, out PoolItem item))
                    {
                        SortedSet<PoolItem> pool = tx.IsLowPriority
                            ? _unverifiedSortedLowPriorityTransactions : _unverifiedSortedHighPriorityTransactions;
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
            
            RemoveOverCapacity();
            
            // Clear the verified transactions now, since they all must be reverified.
            _unsortedTransactions.Clear();
            _sortedHighPrioTransactions.Clear();
            _sortedLowPrioTransactions.Clear();

            // If we know about headers of future blocks, no point in verifying transactions from the unverified tx pool
            // until we get caught up.
            if (block.Index < Blockchain.Singleton.HeaderHeight)
                return;

            int maxHighPrioTransactionsPerBlock = MaxHighPriorityTxsPerBlock;

            ReverifyHighPriorityTransactions(maxHighPrioTransactionsPerBlock, MaxSecondsToReverifyHighPrioTx, snapshot);
            ReverifyLowPriorityTransactions(Settings.Default.MaxFreeTransactionsPerBlock, MaxSecondsToReverifyLowPrioTx,
                snapshot);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReverifyHighPriorityTransactions(int count, double secondsTimeout, Snapshot snapshot)
        {
            DateTime reverifyCutOffTimeStamp = DateTime.UtcNow.AddSeconds(secondsTimeout);
            int addedCount = 0;
            
            foreach (PoolItem item in _unverifiedSortedHighPriorityTransactions.Reverse().Take(count).ToArray())
            {
                // Re-verify the top fee max high priority transactions that can be verified in a block
                if (item.Transaction.Verify(snapshot, _unsortedTransactions.Select(p => p.Value.Transaction)))
                {
                    _unsortedTransactions.TryAdd(item.Transaction.Hash, item);
                    _sortedHighPrioTransactions.Add(item);
                    addedCount++;
                    _unverifiedTransactions.TryRemove(item.Transaction.Hash, out _);
                    _unverifiedSortedHighPriorityTransactions.Remove(item);                        
                }

                if (DateTime.UtcNow > reverifyCutOffTimeStamp) break;
            }

            return addedCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ReverifyLowPriorityTransactions(int count, double secondsTimeout, Snapshot snapshot)
        {
            DateTime reverifyCutOffTimeStamp = DateTime.UtcNow.AddSeconds(secondsTimeout);
            int addedCount = 0;            
            
            foreach (PoolItem item in _unverifiedSortedLowPriorityTransactions.Reverse().Take(count).ToArray())
            {
                // Re-verify the top fee max low priority transactions that can be verified in a block
                if (item.Transaction.Verify(snapshot, _unsortedTransactions.Select(p => p.Value.Transaction)))
                {
                    _unsortedTransactions.TryAdd(item.Transaction.Hash, item);
                    _sortedLowPrioTransactions.Add(item);
                    _unverifiedTransactions.TryRemove(item.Transaction.Hash, out _);
                    _unverifiedSortedLowPriorityTransactions.Remove(item);
                }
                
                if (DateTime.UtcNow > reverifyCutOffTimeStamp) break;
            }

            return addedCount;
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
            if (_unverifiedSortedHighPriorityTransactions.Count > 0)
            {
                // Always leave at least 1 tx for low priority tx
                int verifyCount = _sortedHighPrioTransactions.Count > MaxHighPriorityTxsPerBlock || maxToVerify == 1
                    ? 1 : maxToVerify - 1; 
                maxToVerify -= ReverifyHighPriorityTransactions(verifyCount, MaxSecondsToReverifyHighPrioTxPerIdle, 
                    snapshot);
                
                if (maxToVerify == 0) maxToVerify++;
            }

            if (_unverifiedSortedLowPriorityTransactions.Count > 0)
            {
                int verifyCount = _sortedLowPrioTransactions.Count > Settings.Default.MaxFreeTransactionsPerBlock
                    ? 1 : maxToVerify;
                ReverifyLowPriorityTransactions(verifyCount, MaxSecondsToReverifyLowPrioTxPerIdle, snapshot);
            }

            return _unverifiedTransactions.Count > 0;
        }
    }
}
