// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using Neo.Network.P2P.Payloads;

namespace Neo.Ledger
{
    /// <summary>
    /// Used to cache verified transactions before being written into the block.
    /// </summary>
    public partial class MemoryPool
    {
        // Transactions that will expire in the next block.
        private readonly SortedSet<WeakReference<PoolItem>> _edenTransactions = new();
        // Transactions that will expire in the next 10 blocks.
        private readonly SortedSet<WeakReference<PoolItem>> _survivorTransactions = new();
        private readonly SortedSet<WeakReference<PoolItem>> _tenuredTransactions = new();

        private uint _edenCapacity;
        private uint _survivorCapacity;

        private int EdenCount => _edenTransactions.Count;
        private int SurvivorCount => _survivorTransactions.Count;
        private int TenuredCount => _tenuredTransactions.Count;


        /// <summary>
        /// Checks whether the new transaction can be added to the pool.
        /// if it expires in the next block, it will be added to the eden pool.
        /// if it expires in the next 10 blocks, it will be added to the survivor pool.
        /// if it expires in more than 10 blocks, it will be added to the tenured pool (not really).
        /// already expired transactions will be rejected.
        /// </summary>
        /// <param name="tx">new transaction</param>
        /// <returns>if the transaction is valid and can be added to the pool.</returns>
        private bool ExpiringCheck(Transaction tx)
        {
            // Header cache might not be a good choice here, not sure if it works
            // while the node is syncing.
            var lifeTime = tx.ValidUntilBlock - _system.HeaderCache.Count;

            if (lifeTime < 1)
            {
                return false;
            }

            return lifeTime switch
            {
                1 => ProcessTransaction(_edenTransactions, _edenCapacity, tx),
                >= 2 and <= 9 => ProcessTransaction(_survivorTransactions, _survivorCapacity, tx),
                _ => true
            };
        }

        private bool ProcessTransaction(SortedSet<WeakReference<PoolItem>> transactionSet, uint capacity, Transaction tx)
        {
            RemoveEmptyWeakReferences(transactionSet);

            if (transactionSet.Count > capacity)
            {
                // get lowest fee transaction in set
                transactionSet.Min.TryGetTarget(out var item);

                if (item.CompareTo(tx) > 0)
                {
                    return false;
                }
                // otherwise the lowest one will be deleted from the pool.
                if (!TryRemoveTransaction(item.Tx))
                {
                    return false;
                }
                _txRwLock.EnterWriteLock();
                transactionSet.Add(new WeakReference<PoolItem>(new PoolItem(tx)));
                _txRwLock.ExitWriteLock();
                return true;
            }

            if (!CanTransactionFitInPool(tx))
            {
                return false;
            }

            _txRwLock.EnterWriteLock();
            transactionSet.Add(new WeakReference<PoolItem>(new PoolItem(tx)));
            _txRwLock.ExitWriteLock();
            return true;
        }

        /// <summary>
        /// Clear the eden pool after a new block is generated.
        /// </summary>
        /// <returns></returns>
        private bool ClearEden()
        {
            RemoveEmptyWeakReferences(_edenTransactions);
            foreach (var weakReference in _edenTransactions)
            {
                weakReference.TryGetTarget(out var item);
                if (item == null) continue;
                if (!TryRemoveTransaction(item.Tx)) return false;
            }
            _edenTransactions.Clear();
            return true;
        }

        /// <summary>
        /// Clear the survivor pool to refresh it.
        /// Call after a new block is persisted.
        /// </summary>
        /// <returns></returns>
        public bool ClearSurvivor()
        {
            RemoveEmptyWeakReferences(_survivorTransactions);
            foreach (var weakReference in _survivorTransactions)
            {
                weakReference.TryGetTarget(out var item);
                if (item == null) continue;
                if (!TryRemoveTransaction(item.Tx)) return false;
            }
            _survivorTransactions.Clear();
            return true;
        }

        // called everytime a new block is generated,
        // which means the current height is changed.
        public void ExpirationUpdate()
        {
            //delete Eden after a new block is generated.
            ClearEden();

            // if the survivor is too small, refresh the survivor
            if (SurvivorCount < _edenCapacity)
            {
                // clean the survivor for the convenience of refresh.
                ClearSurvivor();

                // Loop through verified and unverified transaction pools
                // to find the transaction that can be added to survivor.
                UpdateSurvivorTransactions(_verifiedTransactions);
                UpdateSurvivorTransactions(_unverifiedTransactions);
            }

            // check transactions in survivor, if the transaction valid
            // until block is less than current height+1, move it from survivor to eden
            foreach (var item in _survivorTransactions)
            {
                SurvivorPoolExpiringCheck(item);
            }
        }

        private void UpdateSurvivorTransactions(Dictionary<UInt256, PoolItem> transactionDictionary)
        {
            foreach (var (_, tx) in transactionDictionary)
            {
                // survivor only handle transactions that will expire in 10 blocks
                if (tx.Tx.ValidUntilBlock - _system.HeaderCache.Count > 10) continue;

                // survivor is full, update the existing survivor
                if (SurvivorCount >= _edenCapacity)
                {
                    // get lowest fee transaction in survivor
                    _survivorTransactions.Min.TryGetTarget(out var minItem);

                    // if the lowest fee is higher than tx, tx will not be added
                    // and tx will be deleted from the mempool
                    if (minItem != null && minItem.CompareTo(tx) > 0)
                    {
                        TryRemoveTransaction(tx.Tx);
                        continue;
                    }
                }

                // add the tx to the survivor
                _txRwLock.EnterWriteLock();
                _survivorTransactions.Add(new WeakReference<PoolItem>(tx));
                _txRwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Update the survivor pool to eden pool.
        /// </summary>
        /// <param name="poolItem">Transaction in the survivor pool.</param>
        /// <returns></returns>
        private bool SurvivorPoolExpiringCheck(WeakReference<PoolItem> poolItem)
        {
            poolItem.TryGetTarget(out var tx);
            if (tx == null) return false;

            // only transactions that will expire in next block will be moved to eden
            if (tx.Tx.ValidUntilBlock - _system.HeaderCache.Count != 1) return false;

            // remove the transaction from survivor pool if it is expiring.
            _txRwLock.EnterWriteLock();
            _survivorTransactions.Remove(poolItem);
            _txRwLock.ExitWriteLock();
            // Eden only handles transactions that has the highest fee
            // rest transactions that expire in the next block will be disposed directly.
            if (EdenCount > _edenCapacity)
            {
                _edenTransactions.Min.TryGetTarget(out var item);
                // transaction in the pool fee is higher,
                // then delete the survivor tx from pool.
                if (item.CompareTo(tx) > 0)
                {
                    TryRemoveTransaction(tx.Tx);
                    return false;
                }
                // transaction in the pool fee is lower,
                // delete the low fee transaction from the pool,
                // remove the low fee transaction from eden pool.
                TryRemoveTransaction(item.Tx);
                _txRwLock.EnterWriteLock();
                _edenTransactions.Remove(_edenTransactions.Min);
                _txRwLock.ExitWriteLock();
            }
            // move the transaction to eden pool.
            _txRwLock.EnterWriteLock();
            _edenTransactions.Add(poolItem);
            _txRwLock.ExitWriteLock();
            return true;
        }


        /// <summary>
        /// User gonna pay much more fees if the pool is almost full.
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool CapacityCheck(Transaction tx)
        {
            var capacityLimits = new[] { 0.95, 0.90, 0.85, 0.80, 0.75 };
            var feeThresholds = new uint[] { 9, 8, 7, 6, 5 };

            var txTotalFee = tx.NetworkFee + tx.SystemFee;

            for (int i = 0; i < capacityLimits.Length; i++)
            {
                if (!(Count > Capacity * capacityLimits[i])) continue;
                if (txTotalFee > feeThresholds[i] * 1_0000_0000) return true;
            }
            return false;
        }

        private void RemoveEmptyWeakReferences(SortedSet<WeakReference<PoolItem>> sortedSet)
        {
            var emptyWeakReference = sortedSet.Where(x => !x.TryGetTarget(out _)).ToList();
            _txRwLock.EnterWriteLock();
            foreach (var weakReference in emptyWeakReference)
            {
                sortedSet.Remove(weakReference);
            }
            _txRwLock.ExitWriteLock();
        }
    }
}
