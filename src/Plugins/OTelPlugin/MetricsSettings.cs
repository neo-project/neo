// Copyright (C) 2015-2025 The Neo Project.
//
// MetricsSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;

namespace Neo.Plugins.OpenTelemetry
{
    public class MetricsSettings
    {
        public bool Enabled { get; init; }
        public int Interval { get; init; }
        public MetricsCategorySettings Categories { get; init; }
        public PrometheusExporterSettings PrometheusExporter { get; init; }
        public ConsoleExporterSettings ConsoleExporter { get; init; }

        public MetricsSettings(IConfigurationSection section)
        {
            Enabled = section.GetValue("Enabled", true);
            Interval = section.GetValue("Interval", OTelConstants.DefaultMetricsInterval);
            Categories = new MetricsCategorySettings(section.GetSection("Categories"));
            PrometheusExporter = new PrometheusExporterSettings(section.GetSection("PrometheusExporter"));
            ConsoleExporter = new ConsoleExporterSettings(section.GetSection("ConsoleExporter"));
        }
    }

    public class MetricsCategorySettings
    {
        public MetricsCategorySettings(IConfigurationSection section)
        {
            Blockchain = section.GetValue("Blockchain", true);
            Mempool = section.GetValue("Mempool", true);
            Network = section.GetValue("Network", true);
            System = section.GetValue("System", true);
            Consensus = section.GetValue("Consensus", true);
            State = section.GetValue("State", true);
            Vm = section.GetValue("Vm", true);
            Rpc = section.GetValue("Rpc", true);
        }

        public bool Blockchain { get; }
        public bool Mempool { get; }
        public bool Network { get; }
        public bool System { get; }
        public bool Consensus { get; }
        public bool State { get; }
        public bool Vm { get; }
        public bool Rpc { get; }
    }
}
