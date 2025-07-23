// Copyright (C) 2015-2025 The Neo Project.
//
// SimpleOpenTelemetryPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo;
using Neo.ConsoleService;
using Neo.Plugins;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System;
using System.Diagnostics.Metrics;

namespace Neo.Plugins.OpenTelemetry
{
    public class OpenTelemetryPlugin : Plugin
    {
        private MeterProvider? _meterProvider;
        private Meter? _meter;
        private Counter<long>? _requestCounter;
        private bool _enabled = false;

        public override string Name => "OpenTelemetry";
        public override string Description => "Provides observability for Neo blockchain node using OpenTelemetry";

        protected override void Configure()
        {
            var config = GetConfiguration();
            _enabled = config.GetValue("Enabled", true);
            
            if (!_enabled)
            {
                ConsoleHelper.Warning("OpenTelemetry plugin is disabled in configuration");
            }
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (!_enabled) return;

            // Create a simple meter
            _meter = new Meter("Neo.Blockchain", "1.0.0");
            
            // Create a simple counter
            _requestCounter = _meter.CreateCounter<long>("neo.requests", "requests", "Total number of requests");

            // Initialize OpenTelemetry
            _meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("neo-node", serviceVersion: "3.8.1"))
                .AddMeter("Neo.Blockchain")
                .AddConsoleExporter()
                .Build();

            ConsoleHelper.Info("OpenTelemetry plugin initialized");
            
            // Increment counter as a test
            _requestCounter?.Add(1);
        }

        public override void Dispose()
        {
            _meterProvider?.Dispose();
            _meter?.Dispose();
            base.Dispose();
        }

        [ConsoleCommand("telemetry status", Category = "OpenTelemetry", Description = "Show telemetry status")]
        private void ShowTelemetryStatus()
        {
            ConsoleHelper.Info($"OpenTelemetry Status:");
            ConsoleHelper.Info($"  Enabled: {_enabled}");
            ConsoleHelper.Info($"  Service: neo-node");
        }
    }
}