// Copyright (C) 2015-2025 The Neo Project.
//
// TelemetryPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Telemetry.Collectors;
using Prometheus;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Neo.Plugins.Telemetry
{
    /// <summary>
    /// Neo N3 Telemetry Plugin - Provides comprehensive metrics collection and Prometheus export
    /// for monitoring Neo full node health, performance, and operational status.
    /// </summary>
    public class TelemetryPlugin : Plugin
    {
        public override string Name => "TelemetryPlugin";
        public override string Description => "Telemetry and metrics collection for Neo N3 full node";
        // Note: ExceptionPolicy is protected internal in base class, accessible only within Neo assembly
        // We use the default policy from settings loaded during Configure()

        private NeoSystem? _system;
        private MetricServer? _metricServer;
        private Timer? _collectionTimer;

        // Collectors
        private BlockchainMetricsCollector? _blockchainCollector;
        private NetworkMetricsCollector? _networkCollector;
        private MempoolMetricsCollector? _mempoolCollector;
        private SystemMetricsCollector? _systemCollector;
        private PluginMetricsCollector? _pluginCollector;

        private bool _isRunning;

        public TelemetryPlugin()
        {
            Log($"TelemetryPlugin initialized", LogLevel.Info);
        }

        protected override void Configure()
        {
            TelemetrySettings.Load(GetConfiguration());
            Log($"Configuration loaded - Enabled: {TelemetrySettings.Default.Enabled}, " +
                $"Port: {TelemetrySettings.Default.PrometheusPort}", LogLevel.Info);
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (!TelemetrySettings.Default.Enabled)
            {
                Log("Telemetry plugin is disabled in configuration", LogLevel.Info);
                return;
            }

            _system = system;

            try
            {
                // Determine network name from protocol settings
                var networkName = DetermineNetworkName(system);
                var nodeId = TelemetrySettings.Default.NodeId;

                Log($"Starting telemetry collection for node '{nodeId}' on network '{networkName}'", LogLevel.Info);

                // Initialize collectors based on configuration
                InitializeCollectors(system, nodeId, networkName);

                // Start Prometheus metric server
                StartMetricServer();

                // Start periodic collection timer
                StartCollectionTimer();

                _isRunning = true;
                Log($"Telemetry plugin started successfully. Metrics available at " +
                    $"http://{TelemetrySettings.Default.PrometheusHost}:{TelemetrySettings.Default.PrometheusPort}" +
                    $"{TelemetrySettings.Default.PrometheusPath}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Failed to start telemetry plugin: {ex.Message}", LogLevel.Error);
                StopTelemetry();

                // Re-throw to let the base Plugin class handle according to its ExceptionPolicy
                throw;
            }
        }

        private static string DetermineNetworkName(NeoSystem system)
        {
            // Try to determine network from magic number
            var magic = system.Settings.Network;
            return magic switch
            {
                860833102u => "mainnet",   // Neo N3 MainNet magic
                894710606u => "testnet",   // Neo N3 TestNet magic
                _ => TelemetrySettings.Default.NetworkName
            };
        }

        private void InitializeCollectors(NeoSystem system, string nodeId, string networkName)
        {
            var settings = TelemetrySettings.Default;

            if (settings.CollectBlockchainMetrics)
            {
                _blockchainCollector = new BlockchainMetricsCollector(system, nodeId, networkName);
                Log("Blockchain metrics collector initialized", LogLevel.Debug);
            }

            if (settings.CollectNetworkMetrics)
            {
                _networkCollector = new NetworkMetricsCollector(system, nodeId, networkName);
                Log("Network metrics collector initialized", LogLevel.Debug);
            }

            if (settings.CollectMempoolMetrics)
            {
                _mempoolCollector = new MempoolMetricsCollector(system, nodeId, networkName);
                Log("Mempool metrics collector initialized", LogLevel.Debug);
            }

            if (settings.CollectSystemMetrics)
            {
                _systemCollector = new SystemMetricsCollector(nodeId, networkName);
                Log("System metrics collector initialized", LogLevel.Debug);
            }

            // Always collect plugin metrics
            _pluginCollector = new PluginMetricsCollector(system, nodeId, networkName);
            Log("Plugin metrics collector initialized", LogLevel.Debug);
        }

        private void StartMetricServer()
        {
            var settings = TelemetrySettings.Default;

            try
            {
                _metricServer = new MetricServer(
                    hostname: settings.PrometheusHost,
                    port: settings.PrometheusPort,
                    url: settings.PrometheusPath);

                _metricServer.Start();
                Log($"Prometheus metric server started on port {settings.PrometheusPort}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Failed to start Prometheus metric server: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        private void StartCollectionTimer()
        {
            var intervalMs = TelemetrySettings.Default.SystemMetricsIntervalMs;

            _collectionTimer = new Timer(intervalMs);
            _collectionTimer.Elapsed += OnCollectionTimerElapsed;
            _collectionTimer.AutoReset = true;
            _collectionTimer.Start();

            Log($"Metrics collection timer started with interval {intervalMs}ms", LogLevel.Debug);
        }

        private void OnCollectionTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                // Collect current state from all collectors
                _blockchainCollector?.CollectCurrentState();
                _networkCollector?.CollectCurrentState();
                _mempoolCollector?.CollectCurrentState();
                _systemCollector?.CollectCurrentState();
                _pluginCollector?.CollectCurrentState();
            }
            catch (Exception ex)
            {
                Log($"Error during periodic metrics collection: {ex.Message}", LogLevel.Warning);
            }
        }

        public override void Dispose()
        {
            if (!_isRunning)
            {
                base.Dispose();
                return;
            }

            _isRunning = false;
            Log("Shutting down telemetry plugin...", LogLevel.Info);

            StopTelemetry();
            base.Dispose();
        }

        private void StopTelemetry()
        {
            try
            {
                // Stop collection timer
                if (_collectionTimer != null)
                {
                    _collectionTimer.Stop();
                    _collectionTimer.Elapsed -= OnCollectionTimerElapsed;
                    _collectionTimer.Dispose();
                    _collectionTimer = null;
                }

                // Stop metric server
                if (_metricServer != null)
                {
                    _metricServer.Stop();
                    _metricServer = null;
                }

                // Dispose collectors
                _blockchainCollector?.Dispose();
                _networkCollector?.Dispose();
                _mempoolCollector?.Dispose();
                _systemCollector?.Dispose();
                _pluginCollector?.Dispose();

                _blockchainCollector = null;
                _networkCollector = null;
                _mempoolCollector = null;
                _systemCollector = null;
                _pluginCollector = null;

                Log("Telemetry plugin shut down successfully", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Error during telemetry plugin shutdown: {ex.Message}", LogLevel.Warning);
            }
        }
    }
}
