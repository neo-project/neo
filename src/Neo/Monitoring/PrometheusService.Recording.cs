// Copyright (C) 2015-2025 The Neo Project.
// 
// PrometheusService.Recording.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Prometheus;
using System;

namespace Neo.Monitoring
{
    public sealed partial class PrometheusService
    {
        #region Metric Recording Methods

        // --- Core Blockchain ---
        public void SetBlockchainBlockHeight(uint height)
        {
            if (!_enabled) return;
            BlockchainBlockHeight.Value.Set(height);
        }

        public void SetBlockchainSyncStatus(double status) // 0.0 to 1.0
        {
            if (!_enabled) return;
            BlockchainSyncStatus.Value.Set(status);
        }

        public void SetBlockchainChainTipLag(long lag)
        {
            if (!_enabled) return;
            BlockchainChainTipLag.Value.Set(lag);
        }

        // --- Node Performance ---
        // Note: NodeMemoryWorkingSetBytes and NodeCpuSecondsTotal are updated by UpdateProcessMetrics timer

        public void IncNodeApiRequests(string method, bool success)
        {
            if (!_enabled) return;
            NodeApiRequestsTotal.Value.WithLabels(method ?? "unknown", success ? "success" : "error").Inc();
        }

        public IDisposable MeasureNodeApiRequestDuration(string method)
        {
            return _enabled ? NodeApiRequestDurationSeconds.Value.WithLabels(method ?? "unknown").NewTimer() : NullDisposable.Instance;
        }

        // --- Network ---
        public void SetNetworkPeers(long count)
        {
            if (!_enabled) return;
            NetworkPeersCount.Value.Set(count);
        }

        public void IncNetworkP2PMessagesReceived(string type)
        {
            if (!_enabled) return;
            NetworkP2PMessagesReceivedTotal.Value.WithLabels(type ?? "unknown").Inc();
        }

        public void IncNetworkP2PMessagesSent(string type)
        {
            if (!_enabled) return;
            NetworkP2PMessagesSentTotal.Value.WithLabels(type ?? "unknown").Inc();
        }

        // --- Mempool ---
        public void SetMempoolSizeTransactions(long count)
        {
            if (!_enabled) return;
            MempoolSizeTransactions.Value.Set(count);
        }

        public void SetMempoolSizeBytes(long bytes)
        {
            if (!_enabled) return;
            MempoolSizeBytes.Value.Set(bytes);
        }

        public void IncMempoolTransactionsAdded()
        {
            if (!_enabled) return;
            MempoolTransactionsAddedTotal.Value.Inc();
        }

        public void IncMempoolTransactionsRejected(string reason)
        {
            if (!_enabled) return;
            MempoolTransactionsRejectedTotal.Value.WithLabels(reason ?? "unknown").Inc();
        }

        // --- Consensus ---
        public void SetConsensusHeight(uint height)
        {
            if (!_enabled) return;
            ConsensusCurrentHeight.Value.Set(height);
        }

        public void SetConsensusView(byte view)
        {
            if (!_enabled) return;
            ConsensusCurrentView.Value.Set(view);
        }

        public void IncConsensusMessagesReceived(string type)
        {
            if (!_enabled) return;
            ConsensusP2PMessagesReceivedTotal.Value.WithLabels(type ?? "unknown").Inc();
        }

        public IDisposable MeasureConsensusBlockGeneration()
        {
            return _enabled ? ConsensusBlockGenerationDurationSeconds.Value.NewTimer() : NullDisposable.Instance;
        }

        public void IncConsensusNewBlockPersisted()
        {
            if (!_enabled) return;
            ConsensusNewBlockPersistedTotal.Value.Inc();
        }

        // --- Validator ---
        public void SetValidatorActive(bool isActive)
        {
            if (!_enabled) return;
            ValidatorActive.Value.Set(isActive ? 1 : 0);
        }

        public void IncValidatorMissedBlocks()
        {
            if (!_enabled) return;
            ValidatorMissedBlocksTotal.Value.Inc();
        }

        // --- Execution & Block Processing ---
        public void RecordTransactionExecutionTime(double seconds)
        {
            if (!_enabled) return;
            TransactionExecutionDurationSeconds.Value.Observe(seconds);
        }

        // Use: using (var timer = PrometheusService.Instance.MeasureBlockProcessing()) { ... timer.SetBlockDetails(...); ... return result; }
        public IBlockProcessingTimer MeasureBlockProcessing()
        {
            // Returns a timer that also allows setting block-specific gauges upon disposal
            return _enabled ? new BlockProcessingTimerImpl(
                                BlockProcessingDurationSeconds.Value,
                                BlockProcessingTransactionsTotal.Value,
                                BlockProcessingSizeBytes.Value,
                                BlockGasGeneratedTotal.Value,
                                BlockSystemFeeTotal.Value,
                                BlockNetworkFeeTotal.Value)
                            : NullBlockProcessingTimer.Instance;
        }

        // --- Security ---
        public void IncFailedAuthentication(string service)
        {
            if (!_enabled) return;
            FailedAuthenticationAttemptsTotal.Value.WithLabels(service ?? "unknown").Inc();
        }

        public void IncInvalidP2PMessage(string reason)
        {
            if (!_enabled) return;
            InvalidP2PMessageCountTotal.Value.WithLabels(reason ?? "unknown").Inc();
        }

        public void IncUnexpectedShutdown(string reason)
        {
            if (!_enabled) return;
            UnexpectedShutdownsTotal.Value.WithLabels(reason ?? "unknown").Inc();
        }

        #endregion // Metric Recording Methods
    }
}
