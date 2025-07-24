// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginResourceMonitoring.cs file belongs to the neo project and is free
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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    /// <summary>
    /// Unit tests for plugin resource monitoring system.
    /// Tests resource usage tracking and reporting.
    /// </summary>
    [TestClass]
    public class UT_PluginResourceMonitoring : SecurityTestBase
    {
        #region Resource Usage Tracking Tests

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestBasicResourceUsageTracking()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Act
            var result = await sandbox.ExecuteAsync(() =>
            {
                // Allocate some memory
                var data = new byte[1024 * 100]; // 100KB

                // Do some computation
                var sum = 0;
                for (int i = 0; i < 10000; i++)
                {
                    sum += i;
                }

                return sum;
            });

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.ResourceUsage);
            Assert.IsTrue(result.ResourceUsage.MemoryUsed >= 0);
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 0);
            Assert.IsTrue(result.ResourceUsage.ThreadsCreated >= 0);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestExecutionTimeTracking()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            const int sleepTime = 10;

            // Act
            var result = await sandbox.ExecuteAsync(() =>
            {
                Thread.Sleep(sleepTime);
                return "completed";
            });

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual("completed", result.Result);

            // Execution time should be at least as long as sleep time (with tolerance)
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= sleepTime - 5);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestAsyncExecutionTimeTracking()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            const int delayTime = 10;

            // Act
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(delayTime);
                return 42;
            });

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Result);
            // Execution time might be slightly less than delay time due to measurement precision
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= delayTime - 5);
        }

        #endregion

        #region Resource Usage Reporting Tests

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestGetResourceUsage()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Act
            var usage = sandbox.GetResourceUsage();

            // Assert
            Assert.IsNotNull(usage);
            Assert.IsTrue(usage.MemoryUsed >= 0);
            Assert.IsTrue(usage.ThreadsCreated >= 0);
            Assert.IsTrue(usage.ExecutionTime >= 0);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestResourceUsageWithException()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Act - Execute code that throws an exception
            var result = await sandbox.ExecuteAsync(() =>
            {
                var data = new byte[1024 * 100];
                Thread.Sleep(5);
                throw new InvalidOperationException("Test exception");
            });

            // Assert - Resource usage should still be tracked even when exception occurs
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Exception);
            Assert.IsNotNull(result.ResourceUsage);
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 1);
            Assert.IsTrue(result.ResourceUsage.MemoryUsed >= 0);
        }

        #endregion

        #region Resource Policy Tests

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceMonitoringPolicy()
        {
            // Arrange
            var policy = new ResourceMonitoringPolicy
            {
                EnableMonitoring = true,
                CheckInterval = 1000,
                MemoryWarningThreshold = 0.75,
                CpuWarningThreshold = 0.8
            };

            // Assert
            Assert.IsTrue(policy.EnableMonitoring);
            Assert.AreEqual(1000, policy.CheckInterval);
            Assert.AreEqual(0.75, policy.MemoryWarningThreshold);
            Assert.AreEqual(0.8, policy.CpuWarningThreshold);
        }

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public void TestResourceLimitsValidation()
        {
            // Arrange
            var restrictivePolicy = PluginSecurityPolicy.CreateRestrictive();
            var permissivePolicy = PluginSecurityPolicy.CreatePermissive();

            // Assert - Restrictive policy has lower limits
            Assert.IsTrue(restrictivePolicy.MaxMemoryBytes < permissivePolicy.MaxMemoryBytes);
            Assert.IsTrue(restrictivePolicy.MaxCpuPercent < permissivePolicy.MaxCpuPercent);
            Assert.IsTrue(restrictivePolicy.MaxThreads < permissivePolicy.MaxThreads);
            Assert.IsTrue(restrictivePolicy.MaxExecutionTimeSeconds < permissivePolicy.MaxExecutionTimeSeconds);
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        [TestCategory("ResourceMonitoring")]
        public async Task TestResourceMonitoringPerformanceOverhead()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            const int iterations = 3;
            var totalTime = 0L;

            // Act - Run multiple small operations to measure overhead
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();

                var result = await sandbox.ExecuteAsync(() =>
                {
                    return i * 2;
                });

                stopwatch.Stop();
                totalTime += stopwatch.ElapsedMilliseconds;

                Assert.IsTrue(result.Success);
                Assert.AreEqual(i * 2, result.Result);
            }

            // Assert - Average overhead should be reasonable
            var averageTime = totalTime / (double)iterations;
            Assert.IsTrue(averageTime < 100, $"Average execution time {averageTime}ms is too high");
        }

        #endregion
    }
}
