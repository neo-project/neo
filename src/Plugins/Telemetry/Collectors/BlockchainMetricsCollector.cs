// Copyright (C) 2015-2025 The Neo Project.
//
// BlockchainMetricsCollector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IEventHandlers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.Telemetry.Metrics;
using Neo.SmartContract.Native;
using System.Diagnostics;

namespace Neo.Plugins.Telemetry.Collectors
{
    /// <summary>
    /// Collects blockchain-related metrics including block height, sync status, and block processing times.
    /// </summary>
    public sealed class BlockchainMetricsCollector : ICommittedHandler, IDisposable
    {
        private readonly NeoSystem _system;
        private readonly string _nodeId;
        private readonly string _network;
        private readonly Stopwatch _blockPersistStopwatch = new();
        private DateTime _lastBlockTime = DateTime.UtcNow;
        private bool _disposed;

        public BlockchainMetricsCollector(NeoSystem system, string nodeId, string network)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _nodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            _network = network ?? throw new ArgumentNullException(nameof(network));

            // Subscribe to blockchain events
            Blockchain.Committing += OnCommitting;
            Blockchain.Committed += Blockchain_Committed_Handler;

            // Initialize metrics
            InitializeMetrics();
        }

        private void InitializeMetrics()
        {
            try
            {
                // Initialize with safe defaults - actual values will be set on first block commit
                // or during periodic collection once the store is ready
                MetricsDefinitions.BlockHeight.WithLabels(_nodeId, _network).Set(0);
                MetricsDefinitions.HeaderHeight.WithLabels(_nodeId, _network).Set(0);
                MetricsDefinitions.SyncStatus.WithLabels(_nodeId, _network).Set(1); // Assume synced initially
                MetricsDefinitions.BlocksBehind.WithLabels(_nodeId, _network).Set(0);

                // Try to get actual current height if store is ready
                TryUpdateCurrentHeight();
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(BlockchainMetricsCollector), LogLevel.Warning,
                    $"Failed to initialize blockchain metrics: {ex.Message}");
            }
        }

        private void TryUpdateCurrentHeight()
        {
            try
            {
                var currentHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
                MetricsDefinitions.BlockHeight.WithLabels(_nodeId, _network).Set(currentHeight);
                MetricsDefinitions.HeaderHeight.WithLabels(_nodeId, _network).Set(currentHeight);
            }
            catch
            {
                // Store not ready yet, will be updated on first block commit or periodic collection
            }
        }

        private void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
        {
            _blockPersistStopwatch.Restart();
        }

        public void Blockchain_Committed_Handler(NeoSystem system, Block block)
        {
            if (_disposed) return;

            try
            {
                _blockPersistStopwatch.Stop();
                var persistDuration = _blockPersistStopwatch.ElapsedMilliseconds;

                // Update block height
                MetricsDefinitions.BlockHeight.WithLabels(_nodeId, _network).Set(block.Index);

                // Update header height
                var headerHeight = _system.HeaderCache.Last?.Index ?? block.Index;
                MetricsDefinitions.HeaderHeight.WithLabels(_nodeId, _network).Set(headerHeight);

                // Update blocks persisted counter
                MetricsDefinitions.BlocksPersisted.WithLabels(_nodeId, _network).Inc();

                // Record block persist duration
                MetricsDefinitions.BlockPersistDuration.WithLabels(_nodeId, _network).Observe(persistDuration);

                // Update transaction count for this block
                MetricsDefinitions.BlockTransactionCount.WithLabels(_nodeId, _network).Set(block.Transactions.Length);

                // Update total transactions processed
                MetricsDefinitions.TransactionsProcessed.WithLabels(_nodeId, _network).Inc(block.Transactions.Length);

                // Update sync status
                UpdateSyncStatus(block.Index, headerHeight);

                // Update time since last block
                var timeSinceLastBlock = (DateTime.UtcNow - _lastBlockTime).TotalSeconds;
                MetricsDefinitions.TimeSinceLastBlock.WithLabels(_nodeId, _network).Set(timeSinceLastBlock);
                _lastBlockTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(BlockchainMetricsCollector), LogLevel.Warning,
                    $"Error updating blockchain metrics: {ex.Message}");
            }
        }

        private void UpdateSyncStatus(uint blockHeight, uint headerHeight)
        {
            var blocksBehind = headerHeight - blockHeight;
            MetricsDefinitions.BlocksBehind.WithLabels(_nodeId, _network).Set(blocksBehind);

            // Consider synced if within 2 blocks of header height
            var isSynced = blocksBehind <= 2;
            MetricsDefinitions.SyncStatus.WithLabels(_nodeId, _network).Set(isSynced ? 1 : 0);
        }

        /// <summary>
        /// Collects current blockchain state metrics (called periodically).
        /// </summary>
        public void CollectCurrentState()
        {
            if (_disposed) return;

            try
            {
                var currentHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
                var headerHeight = _system.HeaderCache.Last?.Index ?? currentHeight;

                MetricsDefinitions.BlockHeight.WithLabels(_nodeId, _network).Set(currentHeight);
                MetricsDefinitions.HeaderHeight.WithLabels(_nodeId, _network).Set(headerHeight);
                UpdateSyncStatus(currentHeight, headerHeight);

                // Update time since last block
                var timeSinceLastBlock = (DateTime.UtcNow - _lastBlockTime).TotalSeconds;
                MetricsDefinitions.TimeSinceLastBlock.WithLabels(_nodeId, _network).Set(timeSinceLastBlock);
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(BlockchainMetricsCollector), LogLevel.Debug,
                    $"Error collecting blockchain state: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Blockchain.Committing -= OnCommitting;
            Blockchain.Committed -= Blockchain_Committed_Handler;
        }
    }
}
