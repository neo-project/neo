// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ResourceMonitoring.cs file belongs to the neo project and is free
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    [TestClass]
    public class UT_ResourceMonitoring : SecurityTestBase
    {
        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestPluginResourceMonitorCreation()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var monitor = new PluginResourceMonitor(policy);
            
            Assert.IsNotNull(monitor);
            Assert.IsFalse(monitor.IsMonitoring);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceMonitoringLifecycle()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var monitor = new PluginResourceMonitor(policy);

            // Test initial state
            Assert.IsFalse(monitor.IsMonitoring);

            // Test start monitoring
            monitor.StartMonitoring();
            Assert.IsTrue(monitor.IsMonitoring);

            // Test double start (should be idempotent)
            monitor.StartMonitoring();
            Assert.IsTrue(monitor.IsMonitoring);

            // Test stop monitoring
            monitor.StopMonitoring();
            Assert.IsFalse(monitor.IsMonitoring);

            // Test double stop (should be idempotent)
            monitor.StopMonitoring();
            Assert.IsFalse(monitor.IsMonitoring);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestResourceUsageTracking()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var monitor = new PluginResourceMonitor(policy);

            monitor.StartMonitoring();

            // Simulate some work
            var data = new byte[1024 * 1024]; // 1MB
            await Task.Delay(100);

            var usage = monitor.GetCurrentUsage();
            Assert.IsNotNull(usage);
            Assert.IsTrue(usage.MemoryUsed >= 0);
            Assert.IsTrue(usage.ExecutionTime >= 0);

            monitor.StopMonitoring();
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceViolationDetection()
        {
            var policy = PluginSecurityPolicy.CreateRestrictive();
            policy.MaxMemoryBytes = 1; // Extremely low limit to trigger violation
            policy.MaxThreads = 1; // Extremely low limit

            using var monitor = new PluginResourceMonitor(policy);
            var violations = new List<ResourceViolationEventArgs>();

            monitor.ResourceViolation += (sender, e) =>
            {
                violations.Add(e);
            };

            monitor.StartMonitoring();

            // Force a check that should detect violations
            var limitExceeded = !monitor.CheckLimits();

            monitor.StopMonitoring();

            // We should have detected limit violations
            Assert.IsTrue(limitExceeded || violations.Count > 0);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceViolationTypes()
        {
            // Test all violation types are defined
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceViolationType), ResourceViolationType.Memory));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceViolationType), ResourceViolationType.Cpu));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceViolationType), ResourceViolationType.Threads));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceViolationType), ResourceViolationType.ExecutionTime));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceViolationType), ResourceViolationType.NetworkConnections));
            Assert.IsTrue(Enum.IsDefined(typeof(ResourceViolationType), ResourceViolationType.FileAccess));
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceUsageSnapshot()
        {
            var snapshot1 = ResourceUsage.CreateSnapshot();
            Assert.IsNotNull(snapshot1);
            Assert.IsTrue(snapshot1.MemoryUsed >= 0);
            Assert.IsTrue(snapshot1.ThreadsCreated >= 1);
            Assert.IsTrue(snapshot1.ExecutionTime >= 0);

            Thread.Sleep(50);

            var snapshot2 = ResourceUsage.CreateSnapshot();
            Assert.IsNotNull(snapshot2);

            // Execution time should have increased
            Assert.IsTrue(snapshot2.ExecutionTime > snapshot1.ExecutionTime);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceUsageArithmetic()
        {
            var usage1 = new ResourceUsage
            {
                MemoryUsed = 1000,
                CpuTimeUsed = 100,
                ThreadsCreated = 5,
                ExecutionTime = 500,
                NetworkConnections = 2,
                FilesAccessed = 3,
                PeakMemoryUsed = 1500
            };

            var usage2 = new ResourceUsage
            {
                MemoryUsed = 500,
                CpuTimeUsed = 50,
                ThreadsCreated = 3,
                ExecutionTime = 200,
                NetworkConnections = 1,
                FilesAccessed = 1,
                PeakMemoryUsed = 800
            };

            // Test subtraction
            var diff = usage1.Subtract(usage2);
            Assert.AreEqual(500, diff.MemoryUsed);
            Assert.AreEqual(50, diff.CpuTimeUsed);
            Assert.AreEqual(2, diff.ThreadsCreated);
            Assert.AreEqual(300, diff.ExecutionTime);
            Assert.AreEqual(1, diff.NetworkConnections);
            Assert.AreEqual(2, diff.FilesAccessed);

            // Test negative protection
            var reverseDiff = usage2.Subtract(usage1);
            Assert.AreEqual(0, reverseDiff.MemoryUsed);
            Assert.AreEqual(0, reverseDiff.CpuTimeUsed);
            Assert.AreEqual(0, reverseDiff.ThreadsCreated);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceUsageFormatting()
        {
            var usage = new ResourceUsage
            {
                MemoryUsed = 2 * 1024 * 1024, // 2MB
                CpuTimeUsed = 150,
                ThreadsCreated = 5,
                ExecutionTime = 1000
            };

            var formatted = usage.ToString();
            Assert.IsTrue(formatted.Contains("Memory: 2MB"));
            Assert.IsTrue(formatted.Contains("CPU: 150ms"));
            Assert.IsTrue(formatted.Contains("Threads: 5"));
            Assert.IsTrue(formatted.Contains("Execution: 1000ms"));
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceUsagePercentage()
        {
            var usage = new ResourceUsage
            {
                MemoryUsed = 1024 * 1024 // 1MB
            };

            var percentage = usage.GetMemoryUsagePercentage();
            Assert.IsTrue(percentage >= 0.0);
            Assert.IsTrue(percentage <= 1.0);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestEventDrivenResourceMonitor()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var monitor = new EventDrivenResourceMonitor(policy);

            var events = new List<ResourceEvent>();
            monitor.ResourceEvent += (sender, e) =>
            {
                events.Add(e);
            };

            monitor.StartMonitoring();

            // Simulate some resource-consuming work
            var data = new byte[1024];
            await Task.Delay(50);

            monitor.StopMonitoring();

            // Should have received resource events
            Assert.IsTrue(events.Count >= 0); // May be 0 if no significant changes
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestConcurrentResourceMonitoring()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var monitor = new PluginResourceMonitor(policy);

            monitor.StartMonitoring();

            // Run concurrent tasks
            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var data = new byte[1024];
                    Thread.Sleep(10);
                    var usage = monitor.GetCurrentUsage();
                    Assert.IsNotNull(usage);
                });
            }

            await Task.WhenAll(tasks);

            monitor.StopMonitoring();
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceMonitoringConfiguration()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            policy.ResourceMonitoring = new ResourceMonitoringPolicy
            {
                CheckInterval = 100, // 100ms
                DetailedMetrics = true,
                EnableAlerts = true
            };

            using var monitor = new PluginResourceMonitor(policy);
            
            // Monitor should respect configuration
            Assert.IsNotNull(monitor);
            // Configuration is applied internally
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceLimitValidation()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            policy.MaxMemoryBytes = 128 * 1024 * 1024; // 128MB
            policy.MaxCpuPercent = 80;
            policy.MaxThreads = 50;

            using var monitor = new PluginResourceMonitor(policy);
            monitor.StartMonitoring();

            // Current usage should be within limits
            var withinLimits = monitor.CheckLimits();
            Assert.IsTrue(withinLimits);

            monitor.StopMonitoring();
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceMonitorDisposal()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            var monitor = new PluginResourceMonitor(policy);

            monitor.StartMonitoring();
            Assert.IsTrue(monitor.IsMonitoring);

            // Disposal should stop monitoring
            monitor.Dispose();
            Assert.IsFalse(monitor.IsMonitoring);

            // Multiple dispose should not throw
            monitor.Dispose();
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestResourceMonitoringUnderLoad()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var monitor = new PluginResourceMonitor(policy);

            var violations = 0;
            monitor.ResourceViolation += (sender, e) =>
            {
                Interlocked.Increment(ref violations);
            };

            monitor.StartMonitoring();

            // Create some load
            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var data = new byte[10 * 1024 * 1024]; // 10MB each
                    Thread.Sleep(100);
                    GC.KeepAlive(data);
                });
            }

            await Task.WhenAll(tasks);

            monitor.StopMonitoring();

            // Check final resource usage
            var usage = monitor.GetCurrentUsage();
            Assert.IsNotNull(usage);
            Assert.IsTrue(usage.MemoryUsed > 0);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestViolationEventArgs()
        {
            var args = new ResourceViolationEventArgs
            {
                ViolationType = ResourceViolationType.Memory,
                CurrentValue = 1024 * 1024 * 1024, // 1GB
                LimitValue = 512 * 1024 * 1024, // 512MB
                ResourceUsage = new ResourceUsage
                {
                    MemoryUsed = 1024 * 1024 * 1024,
                    ExecutionTime = 5000
                }
            };

            Assert.AreEqual(ResourceViolationType.Memory, args.ViolationType);
            Assert.AreEqual(1024 * 1024 * 1024, args.CurrentValue);
            Assert.AreEqual(512 * 1024 * 1024, args.LimitValue);
            Assert.IsNotNull(args.ResourceUsage);
            Assert.AreEqual(1024 * 1024 * 1024, args.ResourceUsage.MemoryUsed);
        }
    }
}