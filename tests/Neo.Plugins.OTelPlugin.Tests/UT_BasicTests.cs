// Copyright (C) 2015-2025 The Neo Project.
//
// UT_BasicTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins;
using Neo.Plugins.OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Neo.Plugins.OTelPlugin.Tests
{
    [TestClass]
    public class UT_BasicTests
    {
        [TestMethod]
        public void TestMetricNamesConstants()
        {
            // Test that metric name constants are defined and not null
            Assert.IsNotNull(MetricNames.BlocksProcessedTotal);
            Assert.IsNotNull(MetricNames.TransactionsProcessedTotal);
            Assert.IsNotNull(MetricNames.ProcessCpuUsage);
            Assert.IsNotNull(MetricNames.ProcessMemoryWorkingSet);
            Assert.IsNotNull(MetricNames.MempoolSize);
            Assert.IsNotNull(MetricNames.P2PConnectedPeers);

            // Verify specific values
            Assert.AreEqual("neo.blocks.processed_total", MetricNames.BlocksProcessedTotal);
            Assert.AreEqual("neo.mempool.size", MetricNames.MempoolSize);
            Assert.AreEqual("process.cpu.usage", MetricNames.ProcessCpuUsage);
        }

        [TestMethod]
        public void TestOTelConstantsValues()
        {
            // Test that constants are defined
            Assert.AreEqual("neo-node", OTelConstants.DefaultServiceName);
            Assert.AreEqual("http://localhost:4317", OTelConstants.DefaultEndpoint);
            Assert.AreEqual(9090, OTelConstants.DefaultPrometheusPort);
            Assert.AreEqual(10000, OTelConstants.DefaultTimeout);
        }

        [TestMethod]
        public void TestPluginCanBeInstantiated()
        {
            // Simply test that the plugin class exists and can be referenced
            var pluginType = typeof(OpenTelemetryPlugin);
            Assert.IsNotNull(pluginType);
            Assert.AreEqual("OpenTelemetryPlugin", pluginType.Name);
        }

        [TestMethod]
        public void InitializeMetrics_AllCategoriesDisabled_SkipsInstruments()
        {
            var plugin = new OpenTelemetryPlugin();
            try
            {
                SetPrivateField(plugin, "_settings", CreateSettingsWithEnabledCategories());
                InvokeInitializeMetrics(plugin);

                Assert.IsNull(GetPrivateField<object>(plugin, "_blocksProcessedCounter"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_mempoolSizeGauge"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_connectedPeersGauge"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_cpuUsageGauge"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_consensusMessagesSentCounter"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_stateRootHeightGauge"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_rpcRequestsCounter"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_vmCounterListener"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_traceProfileStore"));
            }
            finally
            {
                plugin.Dispose();
                Plugin.Plugins.Remove(plugin);
            }
        }

        [TestMethod]
        public void InitializeMetrics_BlockchainCategoryEnabled_CreatesBlockchainInstruments()
        {
            var plugin = new OpenTelemetryPlugin();
            try
            {
                SetPrivateField(plugin, "_settings", CreateSettingsWithEnabledCategories("Blockchain"));
                InvokeInitializeMetrics(plugin);

                Assert.IsNotNull(GetPrivateField<object>(plugin, "_blocksProcessedCounter"));
                Assert.IsNotNull(GetPrivateField<object>(plugin, "_blockProcessingTimeHistogram"));
                Assert.IsNotNull(GetPrivateField<object>(plugin, "_blockHeightGauge"));
                Assert.IsNotNull(GetPrivateField<object>(plugin, "_blockProcessingRateGauge"));

                Assert.IsNull(GetPrivateField<object>(plugin, "_mempoolSizeGauge"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_rpcRequestsCounter"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_vmCounterListener"));
            }
            finally
            {
                plugin.Dispose();
                Plugin.Plugins.Remove(plugin);
            }
        }

        [TestMethod]
        public void InitializeMetrics_RpcCategoryEnabled_CreatesRpcInstruments()
        {
            var plugin = new OpenTelemetryPlugin();
            try
            {
                SetPrivateField(plugin, "_settings", CreateSettingsWithEnabledCategories("Rpc"));
                InvokeInitializeMetrics(plugin);

                Assert.IsNotNull(GetPrivateField<object>(plugin, "_rpcRequestsCounter"));
                Assert.IsNotNull(GetPrivateField<object>(plugin, "_rpcRequestErrorCounter"));
                Assert.IsNotNull(GetPrivateField<object>(plugin, "_rpcRequestDurationHistogram"));
                Assert.IsNotNull(GetPrivateField<object>(plugin, "_rpcActiveRequestsGauge"));

                Assert.IsNull(GetPrivateField<object>(plugin, "_blocksProcessedCounter"));
                Assert.IsNull(GetPrivateField<object>(plugin, "_vmCounterListener"));
            }
            finally
            {
                plugin.Dispose();
                Plugin.Plugins.Remove(plugin);
            }
        }

        private static readonly string[] CategoryNames =
        [
            "Blockchain",
            "Mempool",
            "Network",
            "System",
            "Consensus",
            "State",
            "Vm",
            "Rpc"
        ];

        private static OTelSettings CreateSettingsWithEnabledCategories(params string[] enabledCategories)
        {
            var values = new Dictionary<string, string?>
            {
                ["PluginConfiguration:Enabled"] = "true",
                ["PluginConfiguration:ServiceName"] = "unit-test-node",
                ["PluginConfiguration:Metrics:Enabled"] = "true",
                ["PluginConfiguration:Metrics:Interval"] = "1000",
                ["PluginConfiguration:Metrics:PrometheusExporter:Enabled"] = "false",
                ["PluginConfiguration:Metrics:ConsoleExporter:Enabled"] = "false",
                ["PluginConfiguration:Traces:Enabled"] = "false",
                ["PluginConfiguration:Logs:Enabled"] = "false",
                ["PluginConfiguration:OtlpExporter:Enabled"] = "false"
            };

            var enabledSet = new HashSet<string>(enabledCategories ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var category in CategoryNames)
            {
                values[$"PluginConfiguration:Metrics:Categories:{category}"] =
                    enabledSet.Contains(category) ? bool.TrueString : bool.FalseString;
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            return new OTelSettings(configuration.GetSection("PluginConfiguration"));
        }

        private static void InvokeInitializeMetrics(OpenTelemetryPlugin plugin)
        {
            var method = typeof(OpenTelemetryPlugin).GetMethod("InitializeMetrics", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "InitializeMetrics method not found via reflection.");
            method.Invoke(plugin, null);
        }

        private static void SetPrivateField(object target, string fieldName, object? value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static T? GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            return (T?)field.GetValue(target);
        }
    }
}
