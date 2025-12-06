// Copyright (C) 2015-2025 The Neo Project.
//
// MetricsDefinitionsTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Telemetry.Metrics;
using Xunit;

namespace Neo.Plugins.TelemetryPlugin.Tests
{
    public class MetricsDefinitionsTests
    {
        [Fact]
        public void BlockchainMetrics_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(MetricsDefinitions.BlockHeight);
            Assert.NotNull(MetricsDefinitions.HeaderHeight);
            Assert.NotNull(MetricsDefinitions.BlocksPersisted);
            Assert.NotNull(MetricsDefinitions.BlockPersistDuration);
            Assert.NotNull(MetricsDefinitions.BlockTransactionCount);
            Assert.NotNull(MetricsDefinitions.TransactionsProcessed);
            Assert.NotNull(MetricsDefinitions.SyncStatus);
            Assert.NotNull(MetricsDefinitions.BlocksBehind);
            Assert.NotNull(MetricsDefinitions.TimeSinceLastBlock);
        }

        [Fact]
        public void NetworkMetrics_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(MetricsDefinitions.ConnectedPeers);
            Assert.NotNull(MetricsDefinitions.UnconnectedPeers);
            Assert.NotNull(MetricsDefinitions.PeerConnectionsTotal);
            Assert.NotNull(MetricsDefinitions.PeerDisconnectionsTotal);
            Assert.NotNull(MetricsDefinitions.MessagesReceived);
            Assert.NotNull(MetricsDefinitions.MessagesSent);
            Assert.NotNull(MetricsDefinitions.BytesReceived);
            Assert.NotNull(MetricsDefinitions.BytesSent);
        }

        [Fact]
        public void MempoolMetrics_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(MetricsDefinitions.MempoolSize);
            Assert.NotNull(MetricsDefinitions.MempoolVerifiedCount);
            Assert.NotNull(MetricsDefinitions.MempoolUnverifiedCount);
            Assert.NotNull(MetricsDefinitions.MempoolCapacity);
            Assert.NotNull(MetricsDefinitions.MempoolUtilization);
            Assert.NotNull(MetricsDefinitions.MempoolTransactionsAdded);
            Assert.NotNull(MetricsDefinitions.MempoolTransactionsRemoved);
        }

        [Fact]
        public void SystemMetrics_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(MetricsDefinitions.CpuUsage);
            Assert.NotNull(MetricsDefinitions.MemoryUsageBytes);
            Assert.NotNull(MetricsDefinitions.GcCollectionCount);
            Assert.NotNull(MetricsDefinitions.ThreadPoolWorkerThreads);
            Assert.NotNull(MetricsDefinitions.ThreadPoolCompletionPortThreads);
            Assert.NotNull(MetricsDefinitions.ProcessUptime);
        }

        [Fact]
        public void PluginMetrics_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(MetricsDefinitions.PluginsLoaded);
            Assert.NotNull(MetricsDefinitions.PluginStatus);
        }

        [Fact]
        public void NodeInfoMetrics_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(MetricsDefinitions.NodeInfo);
            Assert.NotNull(MetricsDefinitions.NodeStartTime);
        }

        [Fact]
        public void BlockHeight_ShouldAcceptLabels()
        {
            // Act
            var labeled = MetricsDefinitions.BlockHeight.WithLabels("test-node", "testnet");

            // Assert
            Assert.NotNull(labeled);
        }

        [Fact]
        public void BlocksPersisted_ShouldAcceptLabels()
        {
            // Act
            var labeled = MetricsDefinitions.BlocksPersisted.WithLabels("test-node", "testnet");

            // Assert
            Assert.NotNull(labeled);
        }

        [Fact]
        public void MessagesReceived_ShouldAcceptLabelsWithMessageType()
        {
            // Act
            var labeled = MetricsDefinitions.MessagesReceived.WithLabels("test-node", "testnet", "GetBlocks");

            // Assert
            Assert.NotNull(labeled);
        }
    }
}
