// Copyright (C) 2015-2025 The Neo Project.
//
// SystemMetricsCollectorTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Telemetry.Collectors;
using Xunit;

namespace Neo.Plugins.TelemetryPlugin.Tests
{
    public class SystemMetricsCollectorTests
    {
        [Fact]
        public void Constructor_ShouldNotThrow()
        {
            // Arrange & Act
            var collector = new SystemMetricsCollector("test-node", "testnet");

            // Assert
            Assert.NotNull(collector);

            // Cleanup
            collector.Dispose();
        }

        [Fact]
        public void CollectCurrentState_ShouldNotThrow()
        {
            // Arrange
            var collector = new SystemMetricsCollector("test-node", "testnet");

            // Act & Assert - should not throw
            collector.CollectCurrentState();

            // Cleanup
            collector.Dispose();
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            // Arrange
            var collector = new SystemMetricsCollector("test-node", "testnet");

            // Act & Assert - multiple dispose calls should not throw
            collector.Dispose();
            collector.Dispose();
        }

        [Fact]
        public void CollectCurrentState_AfterDispose_ShouldNotThrow()
        {
            // Arrange
            var collector = new SystemMetricsCollector("test-node", "testnet");
            collector.Dispose();

            // Act & Assert - should not throw even after dispose
            collector.CollectCurrentState();
        }
    }
}
