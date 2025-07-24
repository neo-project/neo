// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ResourceMonitoring_Simple.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.Security;
using System;
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    [TestClass]
    public class UT_ResourceMonitoring_Simple : SecurityTestBase
    {
        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestPluginResourceMonitorSingleton()
        {
            var monitor1 = PluginResourceMonitor.Instance;
            var monitor2 = PluginResourceMonitor.Instance;
            
            Assert.IsNotNull(monitor1);
            Assert.AreSame(monitor1, monitor2);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceMonitoringBasic()
        {
            var monitor = PluginResourceMonitor.Instance;
            var policy = PluginSecurityPolicy.CreateDefault();
            
            // Start monitoring
            var tracker = monitor.StartMonitoring("TestPlugin", policy);
            Assert.IsNotNull(tracker);
            
            // Stop monitoring
            monitor.StopMonitoring("TestPlugin");
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceUsageCreation()
        {
            var usage = new ResourceUsage
            {
                MemoryUsed = 1024,
                CpuTimeUsed = 100,
                ThreadsCreated = 5,
                ExecutionTime = 500
            };
            
            Assert.AreEqual(1024, usage.MemoryUsed);
            Assert.AreEqual(100, usage.CpuTimeUsed);
            Assert.AreEqual(5, usage.ThreadsCreated);
            Assert.AreEqual(500, usage.ExecutionTime);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceUsageToString()
        {
            var usage = new ResourceUsage
            {
                MemoryUsed = 2 * 1024 * 1024, // 2MB
                CpuTimeUsed = 150,
                ThreadsCreated = 5,
                ExecutionTime = 1000
            };
            
            var str = usage.ToString();
            Assert.IsNotNull(str);
            // Basic check that ToString() works
            Assert.IsTrue(str.Length > 0);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestConcurrentMonitoring()
        {
            var monitor = PluginResourceMonitor.Instance;
            var policy = PluginSecurityPolicy.CreateDefault();
            
            // Test concurrent plugin monitoring
            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    var pluginName = $"ConcurrentPlugin{index}";
                    var tracker = monitor.StartMonitoring(pluginName, policy);
                    Assert.IsNotNull(tracker);
                    
                    // Do some work
                    System.Threading.Thread.Sleep(10);
                    
                    monitor.StopMonitoring(pluginName);
                });
            }
            
            await Task.WhenAll(tasks);
        }
    }
}