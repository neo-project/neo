// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Util.Internal;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neo.Ledger
{
    /// <summary>
    /// Used to cache verified transactions before being written into the block.
    /// </summary>
    public class MemoryPool : IReadOnlyCollection<Transaction>
    {
        public event EventHandler<Transaction> TransactionAdded;
        public event EventHandler<TransactionRemovedEventArgs> TransactionRemoved;

        // Allow a reverified transaction to be rebroadcast if it has been this many block times since last broadcast.
        private const int BlocksTillRebroadcast = 10;
        private int RebroadcastMultiplierThreshold => Capacity / 10;

        private readonly double MaxMillisecondsToReverifyTx;

        // These two are not expected to be hit, they are just safeguards.
        private readonly double MaxMillisecondsToReverifyTxPerIdle;

        private readonly NeoSystem _system;

        //
        /// <summary>
        /// Guarantees consistency of the pool data structures.
        ///
        /// Note: The data structures are only modified from the `Blockchain` actor; so operations guaranteed to be
        ///       performed by the blockchain actor do not need to acquire the read lock; they only need the write
        ///       lock for write operations.
        /// </summary>
        private readonly ReaderWriterLockSlim _txRwLock = new(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Store all verified unsorted transactions currently in the pool.
        /// </summary>
        private readonly Dictionary<UInt256, PoolItem> _unsortedTransactions = new();
        /// <summary>
        /// Store transaction hashes that conflict with verified mempooled transactions.
        /// </summary>
        private readonly Dictionary<UInt256, List<UInt256>> _conflicts = new();
        /// <summary>
        /// Stores the verified sorted transactions currently in the pool.
        /// </summary>
        private readonly SortedSet<PoolItem> _sortedTransactions = new();

        /// <summary>
        /// Store the unverified transactions currently in the pool.
        ///
        /// Transactions in this data structure were valid in some prior block, but may no longer be valid.
        /// The top ones that could make it into the next block get verified and moved into the verified data structures
        /// (_unsortedTransactions, and _sortedTransactions) after each block.
        /// </summary>
        private readonly Dictionary<UInt256, PoolItem> _unverifiedTransactions = new();
        private readonly SortedSet<PoolItem> _unverifiedSortedTransactions = new();

        // Internal methods to aid in unit testing
        internal int SortedTxCount => _sortedTransactions.Count;
        internal int UnverifiedSortedTxCount => _unverifiedSortedTransactions.Count;

        /// <summary>
        /// Total maximum capacity of transactions the pool can hold.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the memory pool.
        /// </summary>
        private TransactionVerificationContext VerificationContext = new();

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

        /// <summary>
        /// Total count of unverified transactions in the pool.
        /// </summary>
        public int UnVerifiedCount => _unverifiedTransactions.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryPool"/> class.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object that contains the <see cref="MemoryPool"/>.</param>
        public MemoryPool(NeoSystem system)
        {
            _system = system;
            Capacity = system.Settings.MemoryPoolMaxTransactions;
            MaxMillisecondsToReverifyTx = (double)system.Settings.MillisecondsPerBlock / 3;
            MaxMillisecondsToReverifyTxPerIdle = (double)system.Settings.MillisecondsPerBlock / 15;
        }

        /// <summary>
        /// Determine whether the pool is holding this transaction and has at some point verified it.
        /// </summary>
        /// <param name="hash">The transaction hash.</param>
        /// <returns><see langword="true"/> if the <see cref="MemoryPool"/> contains the transaction; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Note: The pool may not have verified it since the last block was persisted. To get only the
        ///       transactions that have been verified during this block use <see cref="GetVerifiedTransactions"/>.
        /// </remarks>
        public bool ContainsKey(UInt256 hash)
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _unsortedTransactions.ContainsKey(hash) || _unverifiedTransactions.ContainsKey(hash);
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the <see cref="Transaction"/> associated with the specified hash.
        /// </summary>
        /// <param name="hash">The hash of the <see cref="Transaction"/> to get.</param>
        /// <param name="tx">When this method returns, contains the <see cref="Transaction"/> associated with the specified hash, if the hash is found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="MemoryPool"/> contains a <see cref="Transaction"/> with the specified hash; otherwise, <see langword="false"/>.</returns>
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

        /// <summary>
        /// Gets the verified transactions in the <see cref="MemoryPool"/>.
        /// </summary>
        /// <returns>The verified transactions.</returns>
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

        /// <summary>
        /// Gets both the verified and the unverified transactions in the <see cref="MemoryPool"/>.
        /// </summary>
        /// <param name="verifiedTransactions">The verified transactions.</param>
        /// <param name="unverifiedTransactions">The unverified transactions.</param>
        public void GetVerifiedAndUnverifiedTransactions(out IEnumerable<Transaction> verifiedTransactions,
            out IEnumerable<Transaction> unverifiedTransactions)
        {
            _txRwLock.EnterReadLock();
            try
            {
                verifiedTransactions = _sortedTransactions.Reverse().Select(p => p.Tx).ToArray();
                unverifiedTransactions = _unverifiedSortedTransactions.Reverse().Select(p => p.Tx).ToArray();
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the sorted verified transactions in the <see cref="MemoryPool"/>.
        /// </summary>
        /// <returns>The sorted verified transactions.</returns>
        public IEnumerable<Transaction> GetSortedVerifiedTransactions()
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _sortedTransactions.Reverse().Select(p => p.Tx).ToArray();
            }
            finally
            {
                _txRwLock.ExitReadLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PoolItem GetLowestFeeTransaction(SortedSet<PoolItem> verifiedTxSorted,
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
            sortedPool = null;

            try
            {
                return GetLowestFeeTransaction(_sortedTransactions, _unverifiedSortedTransactions, out sortedPool);
            }
            finally
            {
                unsortedTxPool = Object.ReferenceEquals(sortedPool, _unverifiedSortedTransactions)
                   ? _unverifiedTransactions : _unsortedTransactions;
            }
        }

        // Note: this must only be called from a single thread (the Blockchain actor) and
        // doesn't take into account conflicting transactions.
        internal bool CanTransactionFitInPool(Transaction tx)
        {
            if (Count < Capacity) return true;

            return GetLowestFeeTransaction(out _, out _).CompareTo(tx) <= 0;
        }

        internal VerifyResult TryAdd(Transaction tx, DataCache snapshot)
        {
            var poolItem = new PoolItem(tx);

            if (_unsortedTransactions.ContainsKey(tx.Hash)) return VerifyResult.AlreadyExists;

            List<Transaction> removedTransactions = null;
            _txRwLock.EnterWriteLock();
            try
            {
                if (!CheckConflicts(tx, out List<PoolItem> conflictsToBeRemoved)) return VerifyResult.HasConflicts;
                VerifyResult result = tx.VerifyStateDependent(_system.Settings, snapshot, VerificationContext, conflictsToBeRemoved.Select(c => c.Tx));
                if (result != VerifyResult.Succeed) return result;

                _unsortedTransactions.Add(tx.Hash, poolItem);
                VerificationContext.AddTransaction(tx);
                _sortedTransactions.Add(poolItem);
                foreach (var conflict in conflictsToBeRemoved)
                {
                    if (TryRemoveVerified(conflict.Tx.Hash, out var _))
                        VerificationContext.RemoveTransaction(conflict.Tx);
                }
                removedTransactions = conflictsToBeRemoved.Select(itm => itm.Tx).ToList();
                foreach (var attr in tx.GetAttributes<Conflicts>())
                {
                    if (!_conflicts.TryGetValue(attr.Hash, out var pooled))
                    {
                        pooled = new List<UInt256>();
                    }
                    pooled.Add(tx.Hash);
                    _conflicts.AddOrSet(attr.Hash, pooled);
                }

                if (Count > Capacity)
                    removedTransactions.AddRange(RemoveOverCapacity());
            }
            finally
            {
                _txRwLock.ExitWriteLock();
            }

            TransactionAdded?.Invoke(this, poolItem.Tx);
            if (removedTransactions.Count() > 0)
                TransactionRemoved?.Invoke(this, new()
                {
                    Transactions = removedTransactions,
                    Reason = TransactionRemovalReason.CapacityExceeded
                });

            if (!_unsortedTransactions.ContainsKey(tx.Hash)) return VerifyResult.OutOfMemory;
            return VerifyResult.Succeed;
        }

        /// <summary>
        /// Checks whether there is no mismatch in Conflicts attributes between the current transaction
        /// and mempooled unsorted transactions. If true, then these unsorted transactions will be added
        /// into conflictsList.
        /// </summary>
        /// <param name="tx">The <see cref="Transaction"/>current transaction needs to be checked.</param>
        /// <param name="conflictsList">The list of conflicting verified transactions that should be removed from the pool if tx fits the pool.</param>
        /// <returns>True if transaction fits the pool, otherwise false.</returns>
        private bool CheckConflicts(Transaction tx, out List<PoolItem> conflictsList)
        {
            conflictsList = new();
            long conflictsFeeSum = 0;
            // Step 1: check if `tx` was in Conflicts attributes of unsorted transactions.
            if (_conflicts.TryGetValue(tx.Hash, out var conflicting))
            {
                foreach (var hash in conflicting)
                {
                    var unsortedTx = _unsortedTransactions[hash];
                    if (unsortedTx.Tx.Signers.Select(s => s.Account).Contains(tx.Sender))
                        conflictsFeeSum += unsortedTx.Tx.NetworkFee;
                    conflictsList.Add(unsortedTx);
                }
            }
            // Step 2: check if unsorted transactions were in `tx`'s Conflicts attributes.
            foreach (var hash in tx.GetAttributes<Conflicts>().Select(p => p.Hash))
            {
                if (_unsortedTransactions.TryGetValue(hash, out PoolItem unsortedTx))
                {
                    if (!tx.Signers.Select(p => p.Account).Intersect(unsortedTx.Tx.Signers.Select(p => p.Account)).Any()) return false;
                    conflictsFeeSum += unsortedTx.Tx.NetworkFee;
                    conflictsList.Add(unsortedTx);
                }
            }
            // Network fee of tx have to be larger than the sum of conflicting txs network fees.
            if (conflictsFeeSum != 0 && conflictsFeeSum >= tx.NetworkFee)
                return false;

            // Step 3: take into account sender's conflicting transactions while balance check,
            // this will be done in VerifyStateDependant.

            return true;
        }

        private List<Transaction> RemoveOverCapacity()
        {
            List<Transaction> removedTransactions = new();
            do
            {
                PoolItem minItem = GetLowestFeeTransaction(out var unsortedPool, out var sortedPool);

                unsortedPool.Remove(minItem.Tx.Hash);
                sortedPool.Remove(minItem);
                removedTransactions.Add(minItem.Tx);

                if (ReferenceEquals(sortedPool, _sortedTransactions))
                {
                    RemoveConflictsOfVerified(minItem);
                    VerificationContext.RemoveTransaction(minItem.Tx);
                }
            } while (Count > Capacity);

            return removedTransactions;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRemoveVerified(UInt256 hash, out PoolItem item)
        {
            if (!_unsortedTransactions.TryGetValue(hash, out item))
                return false;

            _unsortedTransactions.Remove(hash);
            _sortedTransactions.Remove(item);

            RemoveConflictsOfVerified(item);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveConflictsOfVerified(PoolItem item)
        {
            foreach (var h in item.Tx.GetAttributes<Conflicts>().Select(attr => attr.Hash))
            {
                if (_conflicts.TryGetValue(h, out List<UInt256> conflicts))
                {
                    conflicts.Remove(item.Tx.Hash);
                    if (conflicts.Count() == 0)
                    {
                        _conflicts.Remove(h);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryRemoveUnVerified(UInt256 hash, out PoolItem item)
        {
            if (!_unverifiedTransactions.TryGetValue(hash, out item))
                return false;

            _unverifiedTransactions.Remove(hash);
            _unverifiedSortedTransactions.Remove(item);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvalidateVerifiedTransactions()
        {
            foreach (PoolItem item in _sortedTransactions)
            {
                if (_unverifiedTransactions.TryAdd(item.Tx.Hash, item))
                    _unverifiedSortedTransactions.Add(item);
            }

            // Clear the verified transactions now, since they all must be reverified.
            _unsortedTransactions.Clear();
            VerificationContext = new TransactionVerificationContext();
            _sortedTransactions.Clear();
            _conflicts.Clear();
        }

        // Note: this must only be called from a single thread (the Blockchain actor)
        internal void UpdatePoolForBlockPersisted(Block block, DataCache snapshot)
        {
            var conflictingItems = new List<Transaction>();
            _txRwLock.EnterWriteLock();
            try
            {
                Dictionary<UInt256, List<UInt160>> conflicts = new Dictionary<UInt256, List<UInt160>>();
                // First remove the transactions verified in the block.
                // No need to modify VerificationContext as it will be reset afterwards.
                foreach (Transaction tx in block.Transactions)
                {
                    if (!TryRemoveVerified(tx.Hash, out _)) TryRemoveUnVerified(tx.Hash, out _);
                    var conflictingSigners = tx.Signers.Select(s => s.Account);
                    foreach (var h in tx.GetAttributes<Conflicts>().Select(a => a.Hash))
                    {
                        if (conflicts.TryGetValue(h, out var signersList))
                        {
                            signersList.AddRange(conflictingSigners);
                            continue;
                        }
                        signersList = conflictingSigners.ToList();
                        conflicts.Add(h, signersList);
                    }
                }

                // Then remove the transactions conflicting with the accepted ones.
                // No need to modify VerificationContext as it will be reset afterwards.
                var persisted = block.Transactions.Select(t => t.Hash);
                var stale = new List<UInt256>();
                foreach (var item in _sortedTransactions)
                {
                    if ((conflicts.TryGetValue(item.Tx.Hash, out var signersList) && signersList.Intersect(item.Tx.Signers.Select(s => s.Account)).Any()) || item.Tx.GetAttributes<Conflicts>().Select(a => a.Hash).Intersect(persisted).Any())
                    {
                        stale.Add(item.Tx.Hash);
                        conflictingItems.Add(item.Tx);
                    }
                }
                foreach (var h in stale)
                {
                    if (!TryRemoveVerified(h, out _)) TryRemoveUnVerified(h, out _);
                }

                // Add all the previously verified transactions back to the unverified transactions and clear mempool conflicts list.
                InvalidateVerifiedTransactions();
            }
            finally
            {
                _txRwLock.ExitWriteLock();
            }
            if (conflictingItems.Count() > 0)
            {
                TransactionRemoved?.Invoke(this, new()
                {
                    Transactions = conflictingItems.ToArray(),
                    Reason = TransactionRemovalReason.Conflict,
                });
            }

            // If we know about headers of future blocks, no point in verifying transactions from the unverified tx pool
            // until we get caught up.
            if (block.Index > 0 && _system.HeaderCache.Count > 0)
                return;

            ReverifyTransactions(_sortedTransactions, _unverifiedSortedTransactions, (int)_system.Settings.MaxTransactionsPerBlock, MaxMillisecondsToReverifyTx, snapshot);
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
            SortedSet<PoolItem> unverifiedSortedTxPool, int count, double millisecondsTimeout, DataCache snapshot)
        {
            DateTime reverifyCutOffTimeStamp = TimeProvider.Current.UtcNow.AddMilliseconds(millisecondsTimeout);
            List<PoolItem> reverifiedItems = new(count);
            List<PoolItem> invalidItems = new();

            _txRwLock.EnterWriteLock();
            try
            {
                // Since unverifiedSortedTxPool is ordered in an ascending manner, we take from the end.
                foreach (PoolItem item in unverifiedSortedTxPool.Reverse().Take(count))
                {
                    if (CheckConflicts(item.Tx, out List<PoolItem> conflictsToBeRemoved) &&
                        item.Tx.VerifyStateDependent(_system.Settings, snapshot, VerificationContext, conflictsToBeRemoved.Select(c => c.Tx)) == VerifyResult.Succeed)
                    {
                        reverifiedItems.Add(item);
                        if (_unsortedTransactions.TryAdd(item.Tx.Hash, item))
                        {
                            verifiedSortedTxPool.Add(item);
                            foreach (var attr in item.Tx.GetAttributes<Conflicts>())
                            {
                                if (!_conflicts.TryGetValue(attr.Hash, out var pooled))
                                {
                                    pooled = new List<UInt256>();
                                }
                                pooled.Add(item.Tx.Hash);
                                _conflicts.AddOrSet(attr.Hash, pooled);
                            }
                            VerificationContext.AddTransaction(item.Tx);
                            foreach (var conflict in conflictsToBeRemoved)
                            {
                                if (TryRemoveVerified(conflict.Tx.Hash, out var _))
                                    VerificationContext.RemoveTransaction(conflict.Tx);
                                invalidItems.Add(conflict);
                            }

                        }
                    }
                    else // Transaction no longer valid -- it will be removed from unverifiedTxPool.
                        invalidItems.Add(item);

                    if (TimeProvider.Current.UtcNow > reverifyCutOffTimeStamp) break;
                }

                int blocksTillRebroadcast = BlocksTillRebroadcast;
                // Increases, proportionally, blocksTillRebroadcast if mempool has more items than threshold bigger RebroadcastMultiplierThreshold
                if (Count > RebroadcastMultiplierThreshold)
                    blocksTillRebroadcast = blocksTillRebroadcast * Count / RebroadcastMultiplierThreshold;

                var rebroadcastCutOffTime = TimeProvider.Current.UtcNow.AddMilliseconds(-_system.Settings.MillisecondsPerBlock * blocksTillRebroadcast);
                foreach (PoolItem item in reverifiedItems)
                {
                    if (_unsortedTransactions.ContainsKey(item.Tx.Hash))
                    {
                        if (item.LastBroadcastTimestamp < rebroadcastCutOffTime)
                        {
                            _system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = item.Tx }, _system.Blockchain);
                            item.LastBroadcastTimestamp = TimeProvider.Current.UtcNow;
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
            TransactionRemoved?.Invoke(this, new()
            {
                Transactions = invalidTransactions,
                Reason = TransactionRemovalReason.NoLongerValid
            });

            return reverifiedItems.Count;
        }

        /// <summary>
        /// Reverify up to a given maximum count of transactions. Verifies less at a time once the max that can be
        /// persisted per block has been reached.
        ///
        /// Note: this must only be called from a single thread (the Blockchain actor)
        /// </summary>
        /// <param name="maxToVerify">Max transactions to reverify, the value passed can be >=1</param>
        /// <param name="snapshot">The snapshot to use for verifying.</param>
        /// <returns>true if more unsorted messages exist, otherwise false</returns>
        internal bool ReVerifyTopUnverifiedTransactionsIfNeeded(int maxToVerify, DataCache snapshot)
        {
            if (_system.HeaderCache.Count > 0)
                return false;

            if (_unverifiedSortedTransactions.Count > 0)
            {
                int verifyCount = _sortedTransactions.Count > _system.Settings.MaxTransactionsPerBlock ? 1 : maxToVerify;
                ReverifyTransactions(_sortedTransactions, _unverifiedSortedTransactions,
                    verifyCount, MaxMillisecondsToReverifyTxPerIdle, snapshot);
            }

            return _unverifiedTransactions.Count > 0;
        }
    }
}
