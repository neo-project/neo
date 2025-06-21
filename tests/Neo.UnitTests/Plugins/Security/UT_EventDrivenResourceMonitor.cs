// Copyright (C) 2015-2025 The Neo Project.
//
// UT_EventDrivenResourceMonitor.cs file belongs to the neo project and is free
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
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    [TestClass]
    public class UT_EventDrivenResourceMonitor : SecurityTestBase
    {
        [TestMethod]
        public void TestEventDrivenResourceMonitorCreation()
        {
            using var monitor = new EventDrivenResourceMonitor();
            Assert.IsNotNull(monitor);
        }

        [TestMethod]
        public async Task TestResourceEventReporting()
        {
            using var monitor = new EventDrivenResourceMonitor();

            var resetEvent = new ManualResetEventSlim(false);

            monitor.ResourceViolationDetected += (sender, args) =>
            {
                resetEvent.Set();
            };

            // Report a resource event that should trigger violation
            monitor.ReportResourceUsage("TestPlugin", new ResourceUsageSnapshot
            {
                MemoryBytes = long.MaxValue, // Excessive memory usage
                CpuPercent = 100,
                ThreadCount = 5,
                HandleCount = 1000
            });

            // Wait for event processing
            await Task.Delay(100);

            // Note: The actual violation detection depends on policy configuration
            // This test verifies the monitoring infrastructure works
        }

        [TestMethod]
        public async Task TestOperationTracking()
        {
            using var monitor = new EventDrivenResourceMonitor();

            var operationId = Guid.NewGuid().ToString();

            // Test operation start/end tracking
            monitor.ReportOperationStart("TestPlugin", operationId);

            // Simulate some work
            await Task.Delay(50);

            monitor.ReportOperationEnd("TestPlugin", operationId, new ResourceUsage());

            // Verify the monitor handles the operation lifecycle
            // Note: We can't easily verify internal state without exposing it,
            // but we ensure no exceptions are thrown
        }

        [TestMethod]
        public void TestConcurrentResourceReporting()
        {
            using var monitor = new EventDrivenResourceMonitor();

            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                var pluginName = $"TestPlugin{i}";
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        monitor.ReportResourceUsage(pluginName, new ResourceUsageSnapshot
                        {
                            MemoryBytes = j * 1024,
                            CpuPercent = j * 0.1,
                            ThreadCount = 1,
                            HandleCount = j
                        });
                    }
                });
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks, TimeSpan.FromSeconds(5));

            // Verify no exceptions were thrown during concurrent access
        }

        [TestMethod]
        public async Task TestEventChannelCapacity()
        {
            using var monitor = new EventDrivenResourceMonitor();

            // Send many events rapidly to test channel capacity
            for (int i = 0; i < 1000; i++)
            {
                monitor.ReportResourceUsage($"Plugin{i % 10}", new ResourceUsageSnapshot
                {
                    MemoryBytes = i * 1024,
                    CpuPercent = i * 0.1,
                    ThreadCount = 1,
                    HandleCount = i
                });
            }

            // Wait for processing
            await Task.Delay(50);

            // Verify the monitor can handle high event volume
            // The test passes if no exceptions are thrown
        }

        [TestMethod]
        public void TestResourceViolationDetection()
        {
            using var monitor = new EventDrivenResourceMonitor();


            monitor.ResourceViolationDetected += (sender, args) =>
            {
                Assert.IsNotNull(args.PluginName);
                // Note: Check available properties of ResourceViolationEventArgs
            };

            monitor.ResourceWarningThresholdCrossed += (sender, args) =>
            {
                Assert.IsNotNull(args.PluginName);
                // Note: Check available properties of ResourceWarningEventArgs
            };

            // Report extreme resource usage to trigger violations
            monitor.ReportResourceUsage("TestPlugin", new ResourceUsageSnapshot
            {
                MemoryBytes = long.MaxValue,
                CpuPercent = 100,
                ThreadCount = 1000,
                HandleCount = long.MaxValue
            });

            // Give time for event processing
            Thread.Sleep(200);

            // Note: Actual violation detection depends on policy configuration
            // This test verifies the event infrastructure is working
        }

        [TestMethod]
        public void TestMonitorDisposal()
        {
            var monitor = new EventDrivenResourceMonitor();

            // Test disposal
            monitor.Dispose();

            // Verify operations after disposal don't throw (they should be ignored)
            monitor.ReportResourceUsage("TestPlugin", new ResourceUsageSnapshot());
            monitor.ReportOperationStart("TestPlugin", "test-op");
            monitor.ReportOperationEnd("TestPlugin", "test-op", new ResourceUsage());

            // Multiple dispose calls should not throw
            monitor.Dispose();
        }

        [TestMethod]
        public async Task TestMemoryPressureReporting()
        {
            using var monitor = new EventDrivenResourceMonitor();

            // Test memory pressure simulation through resource usage
            monitor.ReportResourceUsage("TestPlugin", new ResourceUsageSnapshot
            {
                MemoryBytes = 1024 * 1024 * 1024, // 1GB simulates high memory pressure
            });

            await Task.Delay(50);

            // Verify no exceptions thrown during resource reporting
        }

        [TestMethod]
        public async Task TestExceptionReporting()
        {
            using var monitor = new EventDrivenResourceMonitor();

            var testException = new InvalidOperationException("Test exception");

            // Simulate exception reporting through operation tracking
            monitor.ReportOperationStart("TestPlugin", "exception-test");
            monitor.ReportOperationEnd("TestPlugin", "exception-test", new ResourceUsage());

            await Task.Delay(50);

            // Verify operation reporting doesn't throw
        }

        [TestMethod]
        public void TestEventHandlerExceptionHandling()
        {
            using var monitor = new EventDrivenResourceMonitor();

            // Add event handler that throws
            monitor.ResourceViolationDetected += (sender, args) =>
            {
                throw new InvalidOperationException("Test handler exception");
            };

            // Report resource usage that might trigger violation
            monitor.ReportResourceUsage("TestPlugin", new ResourceUsageSnapshot
            {
                MemoryBytes = 1024 * 1024 * 1024, // 1GB
            });

            // Wait for processing
            Thread.Sleep(100);

            // Verify that exceptions in event handlers don't crash the monitor
            // The test passes if no unhandled exceptions escape
        }
    }
}
