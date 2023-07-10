// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Ledger
{

    /// <summary>
    /// Used to cache verified transactions before being written into the block.
    /// </summary>
    public partial class MemoryPool
    {

        /// <summary>
        /// Store all verified unsorted transactions currently in the pool.
        /// </summary>
        private readonly Dictionary<UInt256, PoolItem> _verifiedTransactions = new();
        /// <summary>
        /// Stores the verified sorted transactions currently in the pool.
        /// </summary>
        private readonly SortedSet<PoolItem> _sortedTransactions = new();

        // Internal methods to aid in unit testing
        internal int SortedTxCount => _sortedTransactions.Count;

        /// <summary>
        /// Store all verified unsorted transactions' senders' fee currently in the memory pool.
        /// </summary>
        private TransactionVerificationContext _verificationContext = new();

        /// <summary>
        /// Total count of verified transactions in the pool.
        /// </summary>
        public int VerifiedCount => _verifiedTransactions.Count; // read of 32 bit type is atomic (no lock)

        internal VerifyResult TryAdd(Transaction tx, DataCache snapshot)
        {
            var poolItem = new PoolItem(tx);

            if (_verifiedTransactions.ContainsKey(tx.Hash)) return VerifyResult.AlreadyExists;
            // check the capacity and count of the memory pool
            if (CapacityCheck(tx)) return VerifyResult.InsufficientFunds;
            List<Transaction> removedTransactions = null;
            _txRwLock.EnterWriteLock();
            try
            {
                VerifyResult result = tx.VerifyStateDependent(_system.Settings, snapshot, _verificationContext);
                if (result != VerifyResult.Succeed) return result;

                _verifiedTransactions.Add(tx.Hash, poolItem);
                _verificationContext.AddTransaction(tx);
                _sortedTransactions.Add(poolItem);

                if (Count > Capacity)
                    removedTransactions = RemoveOverCapacity();
            }
            finally
            {
                _txRwLock.ExitWriteLock();
            }

            TransactionAdded?.Invoke(this, poolItem.Tx);
            if (removedTransactions != null)
                TransactionRemoved?.Invoke(this, new()
                {
                    Transactions = removedTransactions,
                    Reason = TransactionRemovalReason.CapacityExceeded
                });

            if (!_verifiedTransactions.ContainsKey(tx.Hash)) return VerifyResult.OutOfMemory;

            return VerifyResult.Succeed;
        }

        /// <summary>
        /// Gets the verified transactions in the <see cref="MemoryPool"/>.
        /// </summary>
        /// <returns>The verified transactions.</returns>
        public IEnumerable<Transaction> GetVerifiedTransactions()
        {
            _txRwLock.EnterReadLock();
            try
            {
                return _verifiedTransactions.Select(p => p.Value.Tx).ToArray();
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
        private bool TryRemoveVerified(UInt256 hash, out PoolItem item)
        {
            if (!_verifiedTransactions.TryGetValue(hash, out item))
                return false;

            _verifiedTransactions.Remove(hash);
            _sortedTransactions.Remove(item);

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
            _verifiedTransactions.Clear();
            _verificationContext = new TransactionVerificationContext();
            _sortedTransactions.Clear();
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
                    if (item.Tx.VerifyStateDependent(_system.Settings, snapshot, _verificationContext) == VerifyResult.Succeed)
                    {
                        reverifiedItems.Add(item);
                        _verificationContext.AddTransaction(item.Tx);
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
                    if (_verifiedTransactions.TryAdd(item.Tx.Hash, item))
                    {
                        verifiedSortedTxPool.Add(item);

                        if (item.LastBroadcastTimestamp < rebroadcastCutOffTime)
                        {
                            _system.LocalNode.Tell(new LocalNode.RelayDirectly { Inventory = item.Tx }, _system.Blockchain);
                            item.LastBroadcastTimestamp = TimeProvider.Current.UtcNow;
                        }
                    }
                    else
                        _verificationContext.RemoveTransaction(item.Tx);

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
