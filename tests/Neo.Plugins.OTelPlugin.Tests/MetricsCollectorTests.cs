// Copyright (C) 2015-2025 The Neo Project.
//
// MetricsCollectorTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Plugins.OpenTelemetry;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.OTelPlugin.Tests
{
    [TestClass]
    public class MetricsCollectorTests
    {
        private Mock<NeoSystem> _mockNeoSystem;
        private Mock<MemoryPool> _mockMemPool;
        private Mock<LocalNode> _mockLocalNode;
        private MetricsCollector _collector;

        [TestInitialize]
        public void Setup()
        {
            _mockNeoSystem = new Mock<NeoSystem>();
            _mockMemPool = new Mock<MemoryPool>();
            _mockLocalNode = new Mock<LocalNode>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _collector?.Dispose();
        }

        [TestMethod]
        public void Constructor_NullNeoSystem_ThrowsArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new MetricsCollector(null, TimeSpan.FromSeconds(5)));
        }

        [TestMethod]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromSeconds(5));

            Assert.IsNotNull(_collector.LastNetworkMetrics);
            Assert.IsNotNull(_collector.LastMemPoolMetrics);
            Assert.IsNotNull(_collector.LastBlockchainMetrics);
        }

        [TestMethod]
        public async Task NetworkMetrics_UpdatedPeriodically()
        {
            // Arrange
            _mockNeoSystem.Setup(x => x.LocalNode).Returns(_mockLocalNode.Object);
            _mockLocalNode.Setup(x => x.ConnectedCount).Returns(5);
            _mockLocalNode.Setup(x => x.UnconnectedCount).Returns(10);

            NetworkMetrics? capturedMetrics = null;
            var resetEvent = new ManualResetEventSlim(false);

            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromMilliseconds(100));
            _collector.NetworkMetricsUpdated += metrics =>
            {
                capturedMetrics = metrics;
                resetEvent.Set();
            };

            // Act
            var signaled = resetEvent.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.IsTrue(signaled, "NetworkMetricsUpdated event was not raised");
            Assert.IsNotNull(capturedMetrics);
            Assert.AreEqual(5, capturedMetrics.ConnectedPeers);
            Assert.AreEqual(10, capturedMetrics.UnconnectedPeers);
        }

        [TestMethod]
        public async Task MemPoolMetrics_CalculatesCapacityRatioCorrectly()
        {
            // Arrange
            _mockNeoSystem.Setup(x => x.MemPool).Returns(_mockMemPool.Object);
            _mockMemPool.Setup(x => x.Count).Returns(250);
            _mockMemPool.Setup(x => x.VerifiedCount).Returns(200);
            _mockMemPool.Setup(x => x.UnVerifiedCount).Returns(50);
            _mockMemPool.Setup(x => x.Capacity).Returns(500);

            MemPoolMetrics? capturedMetrics = null;
            var resetEvent = new ManualResetEventSlim(false);

            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromMilliseconds(100));
            _collector.MemPoolMetricsUpdated += metrics =>
            {
                capturedMetrics = metrics;
                resetEvent.Set();
            };

            // Act
            var signaled = resetEvent.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.IsTrue(signaled, "MemPoolMetricsUpdated event was not raised");
            Assert.IsNotNull(capturedMetrics);
            Assert.AreEqual(250, capturedMetrics.Count);
            Assert.AreEqual(200, capturedMetrics.VerifiedCount);
            Assert.AreEqual(50, capturedMetrics.UnverifiedCount);
            Assert.AreEqual(500, capturedMetrics.Capacity);
            Assert.AreEqual(0.5, capturedMetrics.CapacityRatio, 0.01);
        }

        [TestMethod]
        public void MemPoolMetrics_EstimatesMemoryUsage()
        {
            // Arrange
            _mockNeoSystem.Setup(x => x.MemPool).Returns(_mockMemPool.Object);
            _mockMemPool.Setup(x => x.Count).Returns(100);

            MemPoolMetrics? capturedMetrics = null;
            var resetEvent = new ManualResetEventSlim(false);

            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromMilliseconds(100));
            _collector.MemPoolMetricsUpdated += metrics =>
            {
                capturedMetrics = metrics;
                resetEvent.Set();
            };

            // Act
            var signaled = resetEvent.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.IsTrue(signaled);
            Assert.IsNotNull(capturedMetrics);
            // Average tx size is 250 bytes
            Assert.AreEqual(25000, capturedMetrics.EstimatedMemoryBytes);
        }

        [TestMethod]
        public void Dispose_StopsCollection()
        {
            // Arrange
            var updateCount = 0;
            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromMilliseconds(50));
            _collector.NetworkMetricsUpdated += _ => Interlocked.Increment(ref updateCount);

            // Act
            Thread.Sleep(150); // Should get ~3 updates
            var countBeforeDispose = updateCount;
            _collector.Dispose();
            Thread.Sleep(150); // Should get no more updates

            // Assert
            Assert.IsTrue(countBeforeDispose > 0, "Should have received updates before dispose");
            Assert.AreEqual(countBeforeDispose, updateCount, "Should not receive updates after dispose");
        }

        [TestMethod]
        public void CollectionContinuesAfterException()
        {
            // Arrange
            _mockNeoSystem.Setup(x => x.LocalNode)
                .Throws(new Exception("Test exception"))
                .Callback(() =>
                {
                    // After first exception, return valid data
                    _mockNeoSystem.Setup(x => x.LocalNode).Returns(_mockLocalNode.Object);
                });

            var updateCount = 0;
            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromMilliseconds(50));
            _collector.NetworkMetricsUpdated += _ => Interlocked.Increment(ref updateCount);

            // Act
            Thread.Sleep(200);

            // Assert
            Assert.IsTrue(updateCount > 0, "Collection should continue after exception");
        }

        [TestMethod]
        public void EmptyCapacity_HandlesGracefully()
        {
            // Arrange
            _mockNeoSystem.Setup(x => x.MemPool).Returns(_mockMemPool.Object);
            _mockMemPool.Setup(x => x.Count).Returns(0);
            _mockMemPool.Setup(x => x.Capacity).Returns(0);

            MemPoolMetrics? capturedMetrics = null;
            var resetEvent = new ManualResetEventSlim(false);

            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromMilliseconds(100));
            _collector.MemPoolMetricsUpdated += metrics =>
            {
                capturedMetrics = metrics;
                resetEvent.Set();
            };

            // Act
            var signaled = resetEvent.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.IsTrue(signaled);
            Assert.IsNotNull(capturedMetrics);
            Assert.AreEqual(0, capturedMetrics.CapacityRatio);
        }

        [TestMethod]
        public void NullLocalNode_HandlesGracefully()
        {
            // Arrange
            _mockNeoSystem.Setup(x => x.LocalNode).Returns((LocalNode)null);

            NetworkMetrics? capturedMetrics = null;
            var resetEvent = new ManualResetEventSlim(false);

            _collector = new MetricsCollector(_mockNeoSystem.Object, TimeSpan.FromMilliseconds(100));
            _collector.NetworkMetricsUpdated += metrics =>
            {
                capturedMetrics = metrics;
                resetEvent.Set();
            };

            // Act - Should not crash
            Thread.Sleep(200);

            // Assert - May or may not have updated, but should not crash
            Assert.IsNotNull(_collector.LastNetworkMetrics);
        }
    }
}
