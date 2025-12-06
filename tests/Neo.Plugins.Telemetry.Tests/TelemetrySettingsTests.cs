// Copyright (C) 2015-2025 The Neo Project.
//
// TelemetrySettingsTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Plugins;
using Neo.Plugins.Telemetry;
using Xunit;

namespace Neo.Plugins.TelemetryPlugin.Tests
{
    public class TelemetrySettingsTests
    {
        [Fact]
        public void Default_ShouldNotBeNull()
        {
            // Assert
            Assert.NotNull(TelemetrySettings.Default);
        }

        [Fact]
        public void Default_ShouldHaveValidPrometheusPort()
        {
            // Assert
            Assert.True(TelemetrySettings.Default.PrometheusPort > 0);
            Assert.True(TelemetrySettings.Default.PrometheusPort < 65536);
        }

        [Fact]
        public void Default_ShouldHaveValidPrometheusHost()
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(TelemetrySettings.Default.PrometheusHost));
        }

        [Fact]
        public void Default_ShouldHaveValidPrometheusPath()
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(TelemetrySettings.Default.PrometheusPath));
            Assert.StartsWith("/", TelemetrySettings.Default.PrometheusPath);
        }

        [Fact]
        public void Default_HealthPort_ShouldBeNullOrValid()
        {
            // Assert
            if (TelemetrySettings.Default.HealthPort.HasValue)
            {
                Assert.InRange(TelemetrySettings.Default.HealthPort.Value, 1, 65535);
            }
        }

        [Fact]
        public void Default_ShouldHaveValidSystemMetricsInterval()
        {
            // Assert
            Assert.True(TelemetrySettings.Default.SystemMetricsIntervalMs > 0);
        }

        [Fact]
        public void Default_ShouldHaveValidNodeId()
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(TelemetrySettings.Default.NodeId));
        }

        [Fact]
        public void Default_ShouldHaveValidNetworkName()
        {
            // Assert
            Assert.False(string.IsNullOrEmpty(TelemetrySettings.Default.NetworkName));
        }

        [Fact]
        public void Default_ShouldHaveValidExceptionPolicy()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(UnhandledExceptionPolicy), TelemetrySettings.Default.ExceptionPolicy));
        }

        [Fact]
        public void Load_WithValidConfiguration_ShouldUpdateSettings()
        {
            // Arrange - create a configuration with nested PluginConfiguration section
            var configData = new Dictionary<string, string?>
            {
                ["PluginConfiguration:PrometheusPort"] = "9999",
                ["PluginConfiguration:PrometheusHost"] = "0.0.0.0",
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            TelemetrySettings.Load(configuration.GetSection("PluginConfiguration"));

            // Assert
            Assert.Equal(9999, TelemetrySettings.Default.PrometheusPort);
            Assert.Equal("0.0.0.0", TelemetrySettings.Default.PrometheusHost);
        }

        [Fact]
        public void ExceptionPolicy_ShouldBeAccessible()
        {
            // Assert - IPluginSettings interface requirement
            IPluginSettings settings = TelemetrySettings.Default;
            Assert.True(Enum.IsDefined(typeof(UnhandledExceptionPolicy), settings.ExceptionPolicy));
        }
    }
}
