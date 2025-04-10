// Copyright (C) 2015-2025 The Neo Project.
// 
// PrometheusService.Helpers.cs file belongs to the neo project and is free
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
using System.Threading;

namespace Neo.Monitoring
{
    // Interface for the block processing timer to allow setting details
    public interface IBlockProcessingTimer : IDisposable
    {
        /// <summary>
        /// Sets the details for the block being processed. Call this before the timer is disposed.
        /// </summary>
        /// <param name="transactionCount">Number of transactions in the block.</param>
        /// <param name="sizeBytes">Size of the block in bytes.</param>
        /// <param name="gasGenerated">Total GAS generated in the block (10^-8 units).</param>
        /// <param name="systemFee">Total system fee collected (10^-8 units).</param>
        /// <param name="networkFee">Total network fee collected (10^-8 units).</param>
        void SetBlockDetails(int transactionCount, long sizeBytes, long gasGenerated, long systemFee, long networkFee);
    }

    // Implementation of the block processing timer
    internal sealed class BlockProcessingTimerImpl : IBlockProcessingTimer
    {
        private readonly IDisposable _histogramTimer;
        private readonly Gauge _txGauge;
        private readonly Gauge _sizeGauge;
        private readonly Gauge _gasGeneratedGauge;
        private readonly Gauge _systemFeeGauge;
        private readonly Gauge _networkFeeGauge;
        private bool _disposed = false;
        private int _txCount = 0;
        private long _sizeBytes = 0;
        private long _gasGenerated = 0;
        private long _systemFee = 0;
        private long _networkFee = 0;

        public BlockProcessingTimerImpl(Histogram histogram,
                                      Gauge txGauge, Gauge sizeGauge,
                                      Gauge gasGeneratedGauge, Gauge systemFeeGauge, Gauge networkFeeGauge)
        {
            _histogramTimer = histogram.NewTimer();
            _txGauge = txGauge;
            _sizeGauge = sizeGauge;
            _gasGeneratedGauge = gasGeneratedGauge;
            _systemFeeGauge = systemFeeGauge;
            _networkFeeGauge = networkFeeGauge;
        }

        public void SetBlockDetails(int transactionCount, long sizeBytes, long gasGenerated, long systemFee, long networkFee)
        {
            if (_disposed) return;
            _txCount = transactionCount;
            _sizeBytes = sizeBytes;
            _gasGenerated = gasGenerated;
            _systemFee = systemFee;
            _networkFee = networkFee;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _txGauge.Set(_txCount);
            _sizeGauge.Set(_sizeBytes);
            _gasGeneratedGauge.Set(_gasGenerated);
            _systemFeeGauge.Set(_systemFee);
            _networkFeeGauge.Set(_networkFee);
            _histogramTimer.Dispose();
            _disposed = true;
        }
    }

    // Null implementation for when Prometheus is disabled
    internal sealed class NullBlockProcessingTimer : IBlockProcessingTimer
    {
        public static readonly NullBlockProcessingTimer Instance = new NullBlockProcessingTimer();
        private NullBlockProcessingTimer() { }
        public void SetBlockDetails(int transactionCount, long sizeBytes, long gasGenerated, long systemFee, long networkFee) { /* No-op */ }
        public void Dispose() { /* No-op */ }
    }

    // Helper class for cleaner disposal pattern when using timers/using blocks
    internal sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new NullDisposable();
        private NullDisposable() { } // Prevent external instantiation
        public void Dispose() { }
    }

    /// <summary>
    /// Configuration settings for the Prometheus service.
    /// </summary>
    public class PrometheusSettings
    {
        public bool Enabled { get; set; } = false; // Disabled by default
        public string Host { get; set; } = "127.0.0.1"; // Default to loopback
        public int Port { get; set; } = 9100; // Default Prometheus port often used

        /// <summary>
        /// Initializes a new instance of the <see cref="PrometheusSettings"/> class.
        /// Default values: Enabled = false, Host = 127.0.0.1, Port = 9100
        /// </summary>
        public PrometheusSettings()
        {
            // Default configuration values are set by property initializers
        }
    }

    // Helper for lazy initialization of metrics without capturing 'this'
    internal static class NonCapturingLazyInitializer
    {
        private static readonly LazyThreadSafetyMode Mode = LazyThreadSafetyMode.ExecutionAndPublication;

        public static Lazy<Counter> CreateCounter(string name, string help, params string[] labelNames)
        {
            return new Lazy<Counter>(() => Metrics.CreateCounter(name, help, new CounterConfiguration
            {
                LabelNames = labelNames ?? Array.Empty<string>() // Ensure not null
            }), Mode);
        }

        public static Lazy<Gauge> CreateGauge(string name, string help, params string[] labelNames)
        {
            return new Lazy<Gauge>(() => Metrics.CreateGauge(name, help, new GaugeConfiguration
            {
                LabelNames = labelNames ?? Array.Empty<string>() // Ensure not null
            }), Mode);
        }

        public static Lazy<Histogram> CreateHistogram(string name, string help, HistogramConfiguration? configuration = null)
        {
            configuration ??= new HistogramConfiguration();
            // Ensure LabelNames is not null if provided within configuration
            if (configuration.LabelNames == null) configuration.LabelNames = Array.Empty<string>();

            return new Lazy<Histogram>(() => Metrics.CreateHistogram(name, help, configuration), Mode);
        }
    }
}
