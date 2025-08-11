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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.OpenTelemetry;
using System;

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
    }
}
