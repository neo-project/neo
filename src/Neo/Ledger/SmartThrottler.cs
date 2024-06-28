// Copyright (C) 2015-2024 The Neo Project.
//
// SmartThrottler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Ledger;

/// <summary>
/// SmartThrottler: Protects Neo blockchain's memory pool from attacks and network congestion
/// </summary>
public class SmartThrottler
{
    private readonly MemoryPool _memoryPool;
    private readonly NeoSystem _system;
    private uint _maxTransactionsPerSecond;
    private int _transactionsThisSecond;
    private DateTime _lastResetTime;
    private long _averageFee;

    // Fields for network load estimation
    private readonly Queue<ulong> _recentBlockTimes = new();
    private const int BlockTimeWindowSize = 20; // Consider last 20 blocks
    private ulong _lastBlockTimestamp;
    private int _unconfirmedTxCount;

    /// <summary>
    /// Initializes a new instance of the SmartThrottler
    /// </summary>
    /// <param name="memoryPool">The memory pool this throttler is associated with</param>
    /// <param name="system">The Neo system</param>
    public SmartThrottler(MemoryPool memoryPool, NeoSystem system)
    {
        _memoryPool = memoryPool;
        _system = system;
        _maxTransactionsPerSecond = (uint)system.Settings.MemPoolSettings.MaxTransactionsPerSecond;
        _lastResetTime = TimeProvider.Current.UtcNow;
        _lastBlockTimestamp = TimeProvider.Current.UtcNow.ToTimestampMS();
        _averageFee = CalculateAverageFee(null);
    }

    /// <summary>
    /// Determines whether a new transaction should be accepted
    /// </summary>
    /// <param name="tx">The transaction to be evaluated</param>
    /// <returns>True if the transaction should be accepted, false otherwise</returns>
    public bool ShouldAcceptTransaction(Transaction tx)
    {
        var now = TimeProvider.Current.UtcNow;
        if (now - _lastResetTime >= TimeSpan.FromSeconds(1))
        {
            _transactionsThisSecond = 0;
            _lastResetTime = now;
            AdjustThrottling(null);
            _averageFee = CalculateAverageFee(null);
        }

        // Check if we've hit the tx limit and it's not high priority
        var b = !IsHighPriorityTransaction(tx);
        if ((_transactionsThisSecond >= _maxTransactionsPerSecond) && b)

            /* Unmerged change from project 'Neo(net8.0)'
            Before:
                            return false;

                        _transactionsThisSecond++;
                        return true;
            After:
                        return false;

                    _transactionsThisSecond++;
                    return true;
            */
            return false;

        _transactionsThisSecond++;
        return true;
    }

    /// <summary>
    /// Updates the network state after a new block is added
    /// </summary>
    /// <param name="block">The newly added block</param>
    public void UpdateNetworkState(Block block)
    {
        var currentTime = TimeProvider.Current.UtcNow.ToTimestampMS();
        var blockTime = currentTime - _lastBlockTimestamp;

        _recentBlockTimes.Enqueue(blockTime);
        if (_recentBlockTimes.Count > BlockTimeWindowSize)
            _recentBlockTimes.Dequeue();

        _lastBlockTimestamp = currentTime;
        _unconfirmedTxCount = _memoryPool.Count;
        _averageFee = CalculateAverageFee(block);

        AdjustThrottling(block);
    }

    /// <summary>
    /// Adjusts throttling parameters based on current network conditions
    /// </summary>
    private void AdjustThrottling(Block block)
    {
        var memoryPoolUtilization = (double)_memoryPool.Count / _system.Settings.MemoryPoolMaxTransactions;
        var networkLoad = EstimateNetworkLoad(block);

        _maxTransactionsPerSecond = CalculateOptimalTps(memoryPoolUtilization, networkLoad, block);
    }

    /// <summary>
    /// Estimates current network load
    /// </summary>
    /// <returns>An integer between 0 and 100 representing the estimated network load</returns>
    private int EstimateNetworkLoad(Block block)
    {
        var load = 0;

        // 1. Memory pool utilization (30% weight)
        var memPoolUtilization = (double)_memoryPool.Count / _system.Settings.MemoryPoolMaxTransactions;
        load += (int)(memPoolUtilization * 30);

        // 2. Recent block times (30% weight)
        if (_recentBlockTimes.Count > 0)
        {
            var avgBlockTime = _recentBlockTimes.Average(t => (double)t);
            var blockTimeRatio = avgBlockTime / _system.Settings.MillisecondsPerBlock;
            load += (int)(Math.Min(blockTimeRatio, 2) * 30); // Cap at 60 points
        }

        // 3. Current block transaction count or unconfirmed transaction growth rate (40% weight)
        if (block != null)
        {
            var blockTxRatio = (double)block.Transactions.Length / _system.Settings.MaxTransactionsPerBlock;
            load += (int)(Math.Min(blockTxRatio, 1) * 40);
        }
        else
        {
            var txGrowthRate = (double)_unconfirmedTxCount / _system.Settings.MaxTransactionsPerBlock;
            load += (int)(Math.Min(txGrowthRate, 1) * 40);
        }

        return Math.Min(load, 100); // Ensure load doesn't exceed 100
    }

    /// <summary>
    /// Calculates optimal transactions per second
    /// </summary>
    private uint CalculateOptimalTps(double memoryPoolUtilization, int networkLoad, Block block)
    {
        var baseTps = _system.Settings.MemPoolSettings.MaxTransactionsPerSecond;
        var utilizationFactor = 1 - memoryPoolUtilization;
        var loadFactor = 1 - (networkLoad / 100.0);

        // Consider current block's transaction count if available
        var blockFactor = 1.0;
        if (block != null)
        {
            blockFactor = Math.Max(0.5, (double)block.Transactions.Length / _system.Settings.MaxTransactionsPerBlock);
        }

        var optimalTps = (uint)(baseTps * utilizationFactor * loadFactor * blockFactor);
        return Math.Max(optimalTps, _system.Settings.MaxTransactionsPerBlock); // Ensure TPS isn't lower than max transactions per block
    }

    /// <summary>
    /// Determines if a transaction is high priority
    /// </summary>
    private bool IsHighPriorityTransaction(Transaction tx)
    {
        // High priority: fee > 3x average
        return tx.NetworkFee + tx.SystemFee > _averageFee * 3;
    }

    /// <summary>
    /// Calculates average fee of transactions in memory pool and new block (if provided)
    /// </summary>
    private long CalculateAverageFee(Block block)
    {
        var transactions = _memoryPool.GetSortedVerifiedTransactions().ToList();
        if (block != null)
        {
            transactions.AddRange(block.Transactions); // Include transactions from the new block
        }
        return transactions.Count != 0 ? (long)transactions.Average(tx => tx.NetworkFee + tx.SystemFee) : 0;
    }
}
