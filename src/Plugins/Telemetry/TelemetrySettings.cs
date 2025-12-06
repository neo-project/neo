// Copyright (C) 2015-2025 The Neo Project.
//
// TelemetrySettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Plugins;

namespace Neo.Plugins.Telemetry
{
    /// <summary>
    /// Configuration settings for the Telemetry plugin.
    /// </summary>
    public sealed class TelemetrySettings : IPluginSettings
    {
        /// <summary>
        /// Gets the default settings instance.
        /// </summary>
        public static TelemetrySettings Default { get; private set; } = new();

        /// <summary>
        /// Gets the exception handling policy for the plugin.
        /// </summary>
        public UnhandledExceptionPolicy ExceptionPolicy { get; private set; } = UnhandledExceptionPolicy.StopPlugin;

        /// <summary>
        /// Gets whether the telemetry plugin is enabled.
        /// </summary>
        public bool Enabled { get; private set; } = true;

        /// <summary>
        /// Gets the port for the Prometheus metrics endpoint.
        /// </summary>
        public int PrometheusPort { get; private set; } = 9100;

        /// <summary>
        /// Gets the host address for the Prometheus metrics endpoint.
        /// </summary>
        public string PrometheusHost { get; private set; } = "localhost";

        /// <summary>
        /// Gets the path for the Prometheus metrics endpoint.
        /// </summary>
        public string PrometheusPath { get; private set; } = "/metrics";

        /// <summary>
        /// Gets the port for the health endpoints. Defaults to Prometheus port when null.
        /// </summary>
        public int? HealthPort { get; private set; }

        /// <summary>
        /// Gets the interval in milliseconds for collecting system metrics.
        /// </summary>
        public int SystemMetricsIntervalMs { get; private set; } = 5000;

        /// <summary>
        /// Gets whether to collect blockchain metrics.
        /// </summary>
        public bool CollectBlockchainMetrics { get; private set; } = true;

        /// <summary>
        /// Gets whether to collect network metrics.
        /// </summary>
        public bool CollectNetworkMetrics { get; private set; } = true;

        /// <summary>
        /// Gets whether to collect mempool metrics.
        /// </summary>
        public bool CollectMempoolMetrics { get; private set; } = true;

        /// <summary>
        /// Gets whether to collect system resource metrics (CPU, Memory, etc.).
        /// </summary>
        public bool CollectSystemMetrics { get; private set; } = true;

        /// <summary>
        /// Gets the node identifier label for metrics.
        /// </summary>
        public string NodeId { get; private set; } = Environment.MachineName;

        /// <summary>
        /// Gets the network name label for metrics (e.g., "mainnet", "testnet").
        /// </summary>
        public string NetworkName { get; private set; } = "unknown";

        /// <summary>
        /// Loads settings from the configuration section.
        /// </summary>
        /// <param name="section">The configuration section to load from.</param>
        public static void Load(IConfigurationSection section)
        {
            Default = new TelemetrySettings
            {
                ExceptionPolicy = section.GetValue(nameof(ExceptionPolicy), UnhandledExceptionPolicy.StopPlugin),
                Enabled = section.GetValue(nameof(Enabled), true),
                PrometheusPort = section.GetValue(nameof(PrometheusPort), 9100),
                PrometheusHost = section.GetValue(nameof(PrometheusHost), "localhost") ?? "localhost",
                PrometheusPath = section.GetValue(nameof(PrometheusPath), "/metrics") ?? "/metrics",
                HealthPort = section.GetValue<int?>(nameof(HealthPort)),
                SystemMetricsIntervalMs = section.GetValue(nameof(SystemMetricsIntervalMs), 5000),
                CollectBlockchainMetrics = section.GetValue(nameof(CollectBlockchainMetrics), true),
                CollectNetworkMetrics = section.GetValue(nameof(CollectNetworkMetrics), true),
                CollectMempoolMetrics = section.GetValue(nameof(CollectMempoolMetrics), true),
                CollectSystemMetrics = section.GetValue(nameof(CollectSystemMetrics), true),
                NodeId = GetNonEmptyValue(section, nameof(NodeId), Environment.MachineName),
                NetworkName = GetNonEmptyValue(section, nameof(NetworkName), "unknown")
            };
        }

        private static string GetNonEmptyValue(IConfigurationSection section, string key, string defaultValue)
        {
            var value = section.GetValue<string>(key);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
    }
}
