// Copyright (C) 2015-2025 The Neo Project.
//
// MetricsCollector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.SmartContract.Native;
using System;
using System.Threading;

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Collects metrics from Neo core classes without modifying them.
    /// This class reads publicly exposed information only.
    /// </summary>
    internal class MetricsCollector : IDisposable
    {
        private readonly NeoSystem _neoSystem;
        private readonly Timer _collectionTimer;
        private readonly object _lock = new object();

        // Metrics snapshots
        public NetworkMetrics LastNetworkMetrics { get; private set; }
        public MemPoolMetrics LastMemPoolMetrics { get; private set; }
        public BlockchainMetrics LastBlockchainMetrics { get; private set; }

        // Events for metrics updates
        public event Action<NetworkMetrics>? NetworkMetricsUpdated;
        public event Action<MemPoolMetrics>? MemPoolMetricsUpdated;
        public event Action<BlockchainMetrics>? BlockchainMetricsUpdated;

        public MetricsCollector(NeoSystem neoSystem, TimeSpan collectionInterval)
        {
            _neoSystem = neoSystem ?? throw new ArgumentNullException(nameof(neoSystem));
            
            // Initialize with empty metrics
            LastNetworkMetrics = new NetworkMetrics();
            LastMemPoolMetrics = new MemPoolMetrics();
            LastBlockchainMetrics = new BlockchainMetrics();

            // Start periodic collection
            _collectionTimer = new Timer(
                CollectMetrics,
                null,
                TimeSpan.Zero,
                collectionInterval);
        }

        private void CollectMetrics(object? state)
        {
            try
            {
                CollectNetworkMetrics();
                CollectMemPoolMetrics();
                CollectBlockchainMetrics();
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                ConsoleHelper.Error($"Error collecting metrics: {ex.Message}");
            }
        }

        private void CollectNetworkMetrics()
        {
            if (_neoSystem?.LocalNode is LocalNode localNode)
            {
                var metrics = new NetworkMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    ConnectedPeers = localNode.ConnectedCount,
                    UnconnectedPeers = localNode.UnconnectedCount
                };

                lock (_lock)
                {
                    LastNetworkMetrics = metrics;
                }

                NetworkMetricsUpdated?.Invoke(metrics);
            }
        }

        private void CollectMemPoolMetrics()
        {
            var memPool = _neoSystem?.MemPool;
            if (memPool != null)
            {
                var metrics = new MemPoolMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    Count = memPool.Count,
                    VerifiedCount = memPool.VerifiedCount,
                    UnverifiedCount = memPool.UnVerifiedCount,
                    Capacity = memPool.Capacity
                };

                // Calculate derived metrics
                metrics.CapacityRatio = metrics.Capacity > 0 
                    ? (double)metrics.Count / metrics.Capacity 
                    : 0;

                // Estimate memory usage (approximate based on average tx size)
                // This is an estimation since we can't access internal transaction data
                const int AverageTxSize = 250; // bytes, approximate
                metrics.EstimatedMemoryBytes = metrics.Count * AverageTxSize;

                lock (_lock)
                {
                    LastMemPoolMetrics = metrics;
                }

                MemPoolMetricsUpdated?.Invoke(metrics);
            }
        }

        private void CollectBlockchainMetrics()
        {
            try
            {
                using var snapshot = _neoSystem?.GetSnapshotCache();
                if (snapshot != null)
                {
                    var metrics = new BlockchainMetrics
                    {
                        Timestamp = DateTime.UtcNow,
                        CurrentHeight = NativeContract.Ledger.CurrentIndex(snapshot)
                    };

                    // Check if syncing (simplified check)
                    var headerHeight = _neoSystem?.HeaderCache?.Count > 0 
                        ? _neoSystem.HeaderCache.Last?.Index ?? metrics.CurrentHeight 
                        : metrics.CurrentHeight;
                    
                    metrics.IsSyncing = headerHeight - metrics.CurrentHeight > 10;
                    metrics.NetworkId = (int)(_neoSystem?.Settings.Network ?? 0);

                    lock (_lock)
                    {
                        LastBlockchainMetrics = metrics;
                    }

                    BlockchainMetricsUpdated?.Invoke(metrics);
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.Warning($"Error collecting blockchain metrics: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _collectionTimer?.Dispose();
        }
    }

    /// <summary>
    /// Network metrics snapshot
    /// </summary>
    public class NetworkMetrics
    {
        public DateTime Timestamp { get; set; }
        public int ConnectedPeers { get; set; }
        public int UnconnectedPeers { get; set; }
    }

    /// <summary>
    /// Memory pool metrics snapshot
    /// </summary>
    public class MemPoolMetrics
    {
        public DateTime Timestamp { get; set; }
        public int Count { get; set; }
        public int VerifiedCount { get; set; }
        public int UnverifiedCount { get; set; }
        public int Capacity { get; set; }
        public double CapacityRatio { get; set; }
        public long EstimatedMemoryBytes { get; set; }
    }

    /// <summary>
    /// Blockchain metrics snapshot
    /// </summary>
    public class BlockchainMetrics
    {
        public DateTime Timestamp { get; set; }
        public uint CurrentHeight { get; set; }
        public bool IsSyncing { get; set; }
        public int NetworkId { get; set; }
    }
}