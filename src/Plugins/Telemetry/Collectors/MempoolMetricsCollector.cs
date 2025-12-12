// Copyright (C) 2015-2025 The Neo Project.
//
// MempoolMetricsCollector.cs file belongs to the neo project and is free
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
using Neo.Plugins.Telemetry.Metrics;

namespace Neo.Plugins.Telemetry.Collectors
{
    /// <summary>
    /// Collects memory pool metrics including transaction counts and pool utilization.
    /// </summary>
    public sealed class MempoolMetricsCollector : ITransactionAddedHandler, ITransactionRemovedHandler, IDisposable
    {
        private readonly NeoSystem _system;
        private readonly string _nodeId;
        private readonly string _network;
        private volatile bool _disposed;

        public MempoolMetricsCollector(NeoSystem system, string nodeId, string network)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _nodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            _network = network ?? throw new ArgumentNullException(nameof(network));

            // Subscribe to mempool events
            _system.MemPool.TransactionAdded += MemoryPool_TransactionAdded_Handler;
            _system.MemPool.TransactionRemoved += MemoryPool_TransactionRemoved_Handler;

            // Initialize capacity metric
            MetricsDefinitions.MempoolCapacity.WithLabels(_nodeId, _network).Set(_system.MemPool.Capacity);
        }

        public void MemoryPool_TransactionAdded_Handler(object? sender, Transaction tx)
        {
            if (_disposed) return;

            try
            {
                MetricsDefinitions.MempoolTransactionsAdded.WithLabels(_nodeId, _network).Inc();
                UpdateMempoolMetrics();
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(MempoolMetricsCollector), LogLevel.Debug,
                    $"Error handling transaction added: {ex.Message}");
            }
        }

        public void MemoryPool_TransactionRemoved_Handler(object? sender, TransactionRemovedEventArgs e)
        {
            if (_disposed) return;

            try
            {
                var reason = e.Reason.ToString();
                MetricsDefinitions.MempoolTransactionsRemoved.WithLabels(_nodeId, _network, reason).Inc(e.Transactions.Count);
                UpdateMempoolMetrics();
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(MempoolMetricsCollector), LogLevel.Debug,
                    $"Error handling transaction removed: {ex.Message}");
            }
        }

        private void UpdateMempoolMetrics()
        {
            try
            {
                var memPool = _system.MemPool;

                // Update pool size
                var totalCount = memPool.Count;
                MetricsDefinitions.MempoolSize.WithLabels(_nodeId, _network).Set(totalCount);

                // Update verified count
                var verifiedCount = memPool.VerifiedCount;
                MetricsDefinitions.MempoolVerifiedCount.WithLabels(_nodeId, _network).Set(verifiedCount);

                // Update unverified count
                var unverifiedCount = memPool.UnVerifiedCount;
                MetricsDefinitions.MempoolUnverifiedCount.WithLabels(_nodeId, _network).Set(unverifiedCount);

                // Update utilization ratio
                var capacity = memPool.Capacity;
                var utilization = capacity > 0 ? (double)totalCount / capacity : 0;
                MetricsDefinitions.MempoolUtilization.WithLabels(_nodeId, _network).Set(utilization);
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(MempoolMetricsCollector), LogLevel.Debug,
                    $"Error updating mempool metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Collects current mempool state metrics (called periodically).
        /// </summary>
        public void CollectCurrentState()
        {
            if (_disposed) return;
            UpdateMempoolMetrics();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _system.MemPool.TransactionAdded -= MemoryPool_TransactionAdded_Handler;
            _system.MemPool.TransactionRemoved -= MemoryPool_TransactionRemoved_Handler;
        }
    }
}
