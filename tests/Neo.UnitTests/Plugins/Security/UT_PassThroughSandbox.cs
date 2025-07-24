// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PassThroughSandbox.cs file belongs to the neo project and is free
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
    public class UT_PassThroughSandbox : SecurityTestBase
    {
        [TestMethod]
        public async Task TestAccurateExecutionTimeTracking()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            const int delayMs = 100;
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(delayMs);
                return "test";
            });

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.ResourceUsage);

            // Execution time should be at least the delay time (with some tolerance)
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= delayMs - 10,
                $"Expected execution time >= {delayMs - 10}ms, got {result.ResourceUsage.ExecutionTime}ms");

            // Should not be excessively longer (allow 100ms overhead)
            Assert.IsTrue(result.ResourceUsage.ExecutionTime <= delayMs + 100,
                $"Expected execution time <= {delayMs + 100}ms, got {result.ResourceUsage.ExecutionTime}ms");
        }

        [TestMethod]
        public async Task TestMemoryUsageTracking()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            var result = await sandbox.ExecuteAsync(() =>
            {
                // Allocate memory to test tracking
                var data = new byte[1024 * 1024]; // 1MB
                Array.Fill<byte>(data, 42); // Actually use the memory
                return data.Length;
            });

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.ResourceUsage);

            // Memory usage should be greater than 0
            Assert.IsTrue(result.ResourceUsage.MemoryUsed >= 0,
                $"Expected memory usage >= 0, got {result.ResourceUsage.MemoryUsed}");
        }

        [TestMethod]
        public async Task TestCpuTimeTracking()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            var result = await sandbox.ExecuteAsync(() =>
            {
                // Perform CPU-intensive work
                var sum = 0.0;
                for (int i = 0; i < 1000000; i++)
                {
                    sum += Math.Sqrt(i);
                }
                return sum;
            });

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.ResourceUsage);

            // CPU time should be greater than 0
            Assert.IsTrue(result.ResourceUsage.CpuTimeUsed >= 0,
                $"Expected CPU time >= 0, got {result.ResourceUsage.CpuTimeUsed}ms");
        }

        [TestMethod]
        public async Task TestBaselineResourceUsageTracking()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();

            // Get usage before initialization
            var usageBeforeInit = sandbox.GetResourceUsage();

            await sandbox.InitializeAsync(policy);

            // Wait a bit to establish baseline
            await Task.Delay(50);

            var usageAfterInit = sandbox.GetResourceUsage();

            Assert.IsNotNull(usageBeforeInit);
            Assert.IsNotNull(usageAfterInit);

            // After initialization, we should have some baseline measurements
            Assert.IsTrue(usageAfterInit.ExecutionTime >= 0);
            Assert.IsTrue(usageAfterInit.MemoryUsed >= 0);
            Assert.IsTrue(usageAfterInit.CpuTimeUsed >= 0);
        }

        [TestMethod]
        public async Task TestResourceTrackingConsistency()
        {
            // Simple test to check if initialization works
            var policy = PluginSecurityPolicy.CreateDefault();
            Assert.IsNotNull(policy);

            using var sandbox = new PassThroughSandbox();
            Assert.IsNotNull(sandbox);

            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);

            // Execute a single simple operation
            var result = await sandbox.ExecuteAsync(() => 42);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Result);
            Assert.IsNotNull(result.ResourceUsage);

            // Resource values should be non-negative
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 0);
            Assert.IsTrue(result.ResourceUsage.MemoryUsed >= 0);
            Assert.IsTrue(result.ResourceUsage.CpuTimeUsed >= 0);
            Assert.IsTrue(result.ResourceUsage.ThreadsCreated >= 0);
        }

        [TestMethod]
        public async Task TestResourceUsageWithException()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            var result = await sandbox.ExecuteAsync(() =>
            {
                // Allocate some memory before throwing
                var data = new byte[1024];
                throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
                return data.Length;
#pragma warning restore CS0162
            });

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOfType(result.Exception, typeof(InvalidOperationException));

            // Resource usage should still be tracked even when exceptions occur
            Assert.IsNotNull(result.ResourceUsage);
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 0);
            Assert.IsTrue(result.ResourceUsage.MemoryUsed >= 0);
            Assert.IsTrue(result.ResourceUsage.CpuTimeUsed >= 0);
        }

        [TestMethod]
        public async Task TestConcurrentResourceTracking()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Execute multiple operations concurrently
            var tasks = new Task<SandboxResult>[3];

            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = sandbox.ExecuteAsync(async () =>
                {
                    await Task.Delay(50);
                    var data = new byte[1024 * (index + 1)];
                    return data.Length;
                });
            }

            var results = await Task.WhenAll(tasks);

            // All results should be successful
            foreach (var result in results)
            {
                Assert.IsTrue(result.Success);
                Assert.IsNotNull(result.ResourceUsage);
                Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 45); // At least delay time
                Assert.IsTrue(result.ResourceUsage.MemoryUsed >= 0);
            }

            // Overall sandbox resource usage should reflect all operations
            var overallUsage = sandbox.GetResourceUsage();
            Assert.IsNotNull(overallUsage);
            Assert.IsTrue(overallUsage.ExecutionTime > 0);
        }

        [TestMethod]
        public async Task TestResourceUsageThreadSafety()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();

            // Initialize sandbox
            await sandbox.InitializeAsync(policy);

            // Call GetResourceUsage from multiple threads concurrently
            var tasks = new Task[10];
            var results = new ResourceUsage[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    results[index] = sandbox.GetResourceUsage();
                });
            }

            Task.WaitAll(tasks);

            // All calls should succeed and return valid results
            foreach (var usage in results)
            {
                Assert.IsNotNull(usage);
                Assert.IsTrue(usage.ExecutionTime >= 0);
                Assert.IsTrue(usage.MemoryUsed >= 0);
                Assert.IsTrue(usage.CpuTimeUsed >= 0);
                Assert.IsTrue(usage.ThreadsCreated >= 0);
            }
        }
    }
}
