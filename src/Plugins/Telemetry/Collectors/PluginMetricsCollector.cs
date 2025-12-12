// Copyright (C) 2015-2025 The Neo Project.
//
// PluginMetricsCollector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P;
using Neo.Plugins.Telemetry.Metrics;

namespace Neo.Plugins.Telemetry.Collectors
{
    /// <summary>
    /// Collects plugin and node information metrics.
    /// </summary>
    public sealed class PluginMetricsCollector : IDisposable
    {
        private readonly NeoSystem _system;
        private readonly string _nodeId;
        private readonly string _network;
        private volatile bool _disposed;

        public PluginMetricsCollector(NeoSystem system, string nodeId, string network)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _nodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            _network = network ?? throw new ArgumentNullException(nameof(network));

            // Set node info metric
            var version = _system.GetType().Assembly.GetName().Version?.ToString() ?? "unknown";
            var protocolVersion = LocalNode.ProtocolVersion.ToString();
            MetricsDefinitions.NodeInfo.WithLabels(_nodeId, _network, version, protocolVersion).Set(1);
        }

        /// <summary>
        /// Collects current plugin state metrics.
        /// </summary>
        public void CollectCurrentState()
        {
            if (_disposed) return;

            try
            {
                // Count loaded plugins
                var loadedPlugins = Plugin.Plugins.Count;
                MetricsDefinitions.PluginsLoaded.WithLabels(_nodeId, _network).Set(loadedPlugins);

                // Update individual plugin status (all loaded plugins are considered running)
                foreach (var plugin in Plugin.Plugins)
                {
                    // Note: IsStopped is internal, so we assume all loaded plugins are running
                    MetricsDefinitions.PluginStatus.WithLabels(_nodeId, _network, plugin.Name).Set(1);
                }
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(PluginMetricsCollector), LogLevel.Debug,
                    $"Error collecting plugin metrics: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
