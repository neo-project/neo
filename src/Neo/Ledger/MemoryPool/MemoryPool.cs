// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
    public partial class MemoryPool : IReadOnlyCollection<Transaction>
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
                    return _verifiedTransactions.Count + _unverifiedTransactions.Count;
                }
                finally
                {
                    _txRwLock.ExitReadLock();
                }
            }
        }

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
            _edenCapacity = _system.Settings.MaxTransactionsPerBlock;
            _survivorCapacity = 10 * _edenCapacity;
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
                return _verifiedTransactions.ContainsKey(hash) || _unverifiedTransactions.ContainsKey(hash);
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
                bool ret = _verifiedTransactions.TryGetValue(hash, out PoolItem item)
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
                return _verifiedTransactions.Select(p => p.Value.Tx)
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
                   ? _unverifiedTransactions : _verifiedTransactions;
            }
        }

        // Note: this must only be called from a single thread (the Blockchain actor)
        internal bool CanTransactionFitInPool(Transaction tx)
        {
            if (Count < Capacity) return true;

            return GetLowestFeeTransaction(out _, out _).CompareTo(tx) <= 0;
        }

        private bool TryRemoveTransaction(Transaction tx)
        {
            _txRwLock.EnterWriteLock();
            var res = TryRemoveVerified(tx.Hash, out _) || TryRemoveUnVerified(tx.Hash, out _);
            _txRwLock.ExitWriteLock();
            return res;
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
                    _verificationContext.RemoveTransaction(minItem.Tx);
            } while (Count > Capacity);

            return removedTransactions;
        }

        // Note: this must only be called from a single thread (the Blockchain actor)
        internal void UpdatePoolForBlockPersisted(Block block, DataCache snapshot)
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
                // update the expiration pool.
                ExpirationUpdate();
            }
            finally
            {
                _txRwLock.ExitWriteLock();
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
    }
}
