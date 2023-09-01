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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Ledger;

/// <summary>
/// Used to cache verified transactions before being written into the block.
/// </summary>
public partial class MemoryPool
{
    /// <summary>
    /// Store all verified unsorted transactions currently in the pool.
    /// </summary>
    private readonly Dictionary<UInt256, PoolItem> _unsortedVerifiedTransactions = new();
    /// <summary>
    /// Stores the verified sorted transactions currently in the pool.
    /// </summary>
    private readonly SortedSet<PoolItem> _sortedVerifiedTransactions = new();

    // Internal methods to aid in unit testing
    internal int SortedTxCount => _sortedVerifiedTransactions.Count;

    /// <summary>
    /// Total count of verified transactions in the pool.
    /// </summary>
    public int VerifiedCount => _unsortedVerifiedTransactions.Count; // read of 32 bit type is atomic (no lock)

    /// <summary>
    /// Store all verified unsorted transactions' senders' fee currently in the memory pool.
    /// </summary>
    private TransactionVerificationContext VerificationContext = new();

    /// <summary>
    /// Gets the verified transactions in the <see cref="MemoryPool"/>.
    /// </summary>
    /// <returns>The verified transactions.</returns>
    public IEnumerable<Transaction> GetVerifiedTransactions()
    {
        _txRwLock.EnterReadLock();
        try
        {
            return _unsortedVerifiedTransactions.Select(p => p.Value.Tx).ToArray();
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
            return _sortedVerifiedTransactions.Reverse().Select(p => p.Tx).ToArray();
        }
        finally
        {
            _txRwLock.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryRemoveVerified(UInt256 hash, out PoolItem item)
    {
        if (!_unsortedVerifiedTransactions.TryGetValue(hash, out item))
            return false;

        _unsortedVerifiedTransactions.Remove(hash);
        _sortedVerifiedTransactions.Remove(item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InvalidateVerifiedTransactions()
    {
        foreach (PoolItem item in _sortedVerifiedTransactions)
        {
            if (_unverifiedTransactions.TryAdd(item.Tx.Hash, item))
                _unverifiedSortedTransactions.Add(item);
        }

        // Clear the verified transactions now, since they all must be reverified.
        _unsortedVerifiedTransactions.Clear();
        VerificationContext = new TransactionVerificationContext();
        _sortedVerifiedTransactions.Clear();
    }
}
