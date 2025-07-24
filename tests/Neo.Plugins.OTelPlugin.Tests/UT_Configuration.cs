// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Configuration.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Neo.Plugins.OTelPlugin.Tests
{
    [TestClass]
    public class UT_Configuration
    {
        private string _tempConfigPath = null!;

        [TestInitialize]
        public void Setup()
        {
            _tempConfigPath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempConfigPath))
                File.Delete(_tempConfigPath);
        }

        [TestMethod]
        public void TestDefaultConfiguration()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true,
                ["ServiceName"] = "neo-node",
                ["ServiceVersion"] = "3.8.1"
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(_tempConfigPath)
                .Build();

            var pluginConfig = configuration.GetSection("PluginConfiguration");

            Assert.IsTrue(pluginConfig.GetValue<bool>("Enabled"));
            Assert.AreEqual("neo-node", pluginConfig.GetValue<string>("ServiceName"));
            Assert.AreEqual("3.8.1", pluginConfig.GetValue<string>("ServiceVersion"));
        }

        [TestMethod]
        public void TestMetricsConfiguration()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true,
                ["Metrics"] = new Dictionary<string, object>
                {
                    ["Enabled"] = true,
                    ["PrometheusExporter"] = new Dictionary<string, object>
                    {
                        ["Enabled"] = true,
                        ["Port"] = 9090,
                        ["Path"] = "/metrics"
                    },
                    ["ConsoleExporter"] = new Dictionary<string, object>
                    {
                        ["Enabled"] = false
                    }
                }
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(_tempConfigPath)
                .Build();

            var metricsConfig = configuration.GetSection("PluginConfiguration:Metrics");

            Assert.IsTrue(metricsConfig.GetValue<bool>("Enabled"));

            var prometheusConfig = metricsConfig.GetSection("PrometheusExporter");
            Assert.IsTrue(prometheusConfig.GetValue<bool>("Enabled"));
            Assert.AreEqual(9090, prometheusConfig.GetValue<int>("Port"));
            Assert.AreEqual("/metrics", prometheusConfig.GetValue<string>("Path"));
        }

        [TestMethod]
        public void TestOtlpExporterConfiguration()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true,
                ["OtlpExporter"] = new Dictionary<string, object>
                {
                    ["Enabled"] = true,
                    ["Endpoint"] = "http://localhost:4317",
                    ["Protocol"] = "grpc",
                    ["Timeout"] = 10000,
                    ["Headers"] = "api-key=test-key",
                    ["ExportMetrics"] = true,
                    ["ExportTraces"] = true,
                    ["ExportLogs"] = true
                }
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(_tempConfigPath)
                .Build();

            var otlpConfig = configuration.GetSection("PluginConfiguration:OtlpExporter");

            Assert.IsTrue(otlpConfig.GetValue<bool>("Enabled"));
            Assert.AreEqual("http://localhost:4317", otlpConfig.GetValue<string>("Endpoint"));
            Assert.AreEqual("grpc", otlpConfig.GetValue<string>("Protocol"));
            Assert.AreEqual(10000, otlpConfig.GetValue<int>("Timeout"));
            Assert.AreEqual("api-key=test-key", otlpConfig.GetValue<string>("Headers"));
            Assert.IsTrue(otlpConfig.GetValue<bool>("ExportMetrics"));
            Assert.IsTrue(otlpConfig.GetValue<bool>("ExportTraces"));
            Assert.IsTrue(otlpConfig.GetValue<bool>("ExportLogs"));
        }

        [TestMethod]
        public void TestResourceAttributes()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true,
                ["ResourceAttributes"] = new Dictionary<string, object>
                {
                    ["deployment.environment"] = "production",
                    ["service.namespace"] = "blockchain",
                    ["node.type"] = "full"
                }
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(_tempConfigPath)
                .Build();

            var resourceAttributes = configuration.GetSection("PluginConfiguration:ResourceAttributes");

            Assert.AreEqual("production", resourceAttributes["deployment.environment"]);
            Assert.AreEqual("blockchain", resourceAttributes["service.namespace"]);
            Assert.AreEqual("full", resourceAttributes["node.type"]);
        }

        [TestMethod]
        public void TestDisabledConfiguration()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = false
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            var configuration = new ConfigurationBuilder()
                .AddJsonFile(_tempConfigPath)
                .Build();

            var pluginConfig = configuration.GetSection("PluginConfiguration");

            Assert.IsFalse(pluginConfig.GetValue<bool>("Enabled"));
        }
    }
}
