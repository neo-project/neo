// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Neo.Ledger
{
    /// <summary>
    /// Used to cache verified transactions before being written into the block.
    /// </summary>
    public partial class MemoryPool
    {
        /// <summary>
        /// Store the unverified transactions currently in the pool.
        ///
        /// Transactions in this data structure were valid in some prior block, but may no longer be valid.
        /// The top ones that could make it into the next block get verified and moved into the verified data structures
        /// (_unsortedTransactions, and _sortedTransactions) after each block.
        /// </summary>
        private readonly Dictionary<UInt256, PoolItem> _unverifiedTransactions = new();
        private readonly SortedSet<PoolItem> _unverifiedSortedTransactions = new();

        internal int UnverifiedSortedTxCount => _unverifiedSortedTransactions.Count;

        /// <summary>
        /// Total count of unverified transactions in the pool.
        /// </summary>
        public int UnVerifiedCount => _unverifiedTransactions.Count;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryRemoveUnVerified(UInt256 hash, out PoolItem item)
        {
            if (!_unverifiedTransactions.TryGetValue(hash, out item))
                return false;

            _unverifiedTransactions.Remove(hash);
            _unverifiedSortedTransactions.Remove(item);
            return true;
        }
    }
}
