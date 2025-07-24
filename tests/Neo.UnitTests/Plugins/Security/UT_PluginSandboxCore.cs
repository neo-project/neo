// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginSandboxCore.cs file belongs to the neo project and is free
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    /// <summary>
    /// Core unit tests for the Neo Plugin Sandbox system.
    /// Tests fundamental sandbox functionality, lifecycle, and execution.
    /// </summary>
    [TestClass]
    public class UT_PluginSandboxCore : SecurityTestBase
    {
        #region Sandbox Type Tests

        [TestMethod]
        [TestCategory("Core")]
        [DataRow(typeof(PassThroughSandbox), SandboxType.PassThrough)]
        [DataRow(typeof(AssemblyLoadContextSandbox), SandboxType.AssemblyLoadContext)]
        public async Task TestSandboxTypeIdentification(Type sandboxType, SandboxType expectedType)
        {
            // Arrange
            using var sandbox = (IPluginSandbox)Activator.CreateInstance(sandboxType);

            // Assert type before initialization
            Assert.IsNotNull(sandbox);
            Assert.AreEqual(expectedType, sandbox.Type);

            // Initialize and verify type remains consistent
            var policy = PluginSecurityPolicy.CreateDefault();
            await sandbox.InitializeAsync(policy);
            Assert.AreEqual(expectedType, sandbox.Type);
        }

        [TestMethod]
        [TestCategory("Core")]
        public void TestSandboxTypeEnumeration()
        {
            // Verify all sandbox types are defined
            var definedTypes = Enum.GetValues(typeof(SandboxType)).Cast<SandboxType>().ToList();

            Assert.IsTrue(definedTypes.Contains(SandboxType.PassThrough));
            Assert.IsTrue(definedTypes.Contains(SandboxType.AssemblyLoadContext));
            Assert.IsTrue(definedTypes.Contains(SandboxType.Process));
            Assert.IsTrue(definedTypes.Contains(SandboxType.Container));

            // Verify expected types exist
            Assert.IsTrue(definedTypes.Count >= 4);
        }

        #endregion

        #region Sandbox Lifecycle Tests

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestSandboxInitialization()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();

            // Act & Assert - Before initialization
            Assert.IsFalse(sandbox.IsActive);

            // Act - Initialize
            await sandbox.InitializeAsync(policy);

            // Assert - After initialization
            Assert.IsTrue(sandbox.IsActive);
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestSandboxInitializationWithNullPolicy()
        {
            // Arrange
            using var sandbox = new AssemblyLoadContextSandbox();

            // Act & Assert
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                async () => await sandbox.InitializeAsync(null));
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestSandboxLifecycleComplete()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            var sandbox = new AssemblyLoadContextSandbox();

            // Act & Assert - Initialization
            Assert.IsFalse(sandbox.IsActive);
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);

            // Act & Assert - Suspension
            sandbox.Suspend();
            // Note: PassThrough sandbox doesn't actually suspend

            // Act & Assert - Resume
            sandbox.Resume();
            Assert.IsTrue(sandbox.IsActive);

            // Act & Assert - Termination
            sandbox.Terminate();
            Assert.IsFalse(sandbox.IsActive);

            // Act & Assert - Disposal
            sandbox.Dispose();
            Assert.IsFalse(sandbox.IsActive);
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestSandboxDoubleInitialization()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();

            // Act - First initialization
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);

            // Act - Second initialization (should be idempotent or throw)
            // Different sandboxes may handle this differently
            try
            {
                await sandbox.InitializeAsync(policy);
                // If no exception, verify still active
                Assert.IsTrue(sandbox.IsActive);
            }
            catch (InvalidOperationException)
            {
                // Some sandboxes may throw on re-initialization
                // This is also acceptable behavior
            }
        }

        #endregion

        #region Execution Tests

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestSynchronousExecution()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Act
            var result = await sandbox.ExecuteAsync(() => "Test Result");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Test Result", result.Result);
            Assert.IsNull(result.Exception);
            Assert.IsNotNull(result.ResourceUsage);
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestAsynchronousExecution()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Act
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(50);
                return 42;
            });

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Result);
            Assert.IsNull(result.Exception);
            Assert.IsNotNull(result.ResourceUsage);
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 50);
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestExecutionWithComplexTypes()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            var testData = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 123,
                ["key3"] = new[] { 1, 2, 3 }
            };

            // Act
            var result = await sandbox.ExecuteAsync(() => testData);

            // Assert
            Assert.IsTrue(result.Success);
            var resultData = result.Result as Dictionary<string, object>;
            Assert.IsNotNull(resultData);
            Assert.AreEqual("value1", resultData["key1"]);
            Assert.AreEqual(123, resultData["key2"]);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, resultData["key3"] as int[]);
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestExecutionReturningNull()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Act
            var result = await sandbox.ExecuteAsync(() => null);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNull(result.Result);
            Assert.IsNull(result.Exception);
        }

        #endregion

        #region Exception Handling Tests

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestExceptionInSynchronousExecution()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            const string exceptionMessage = "Test exception";

            // Act
            var result = await sandbox.ExecuteAsync(() =>
            {
                throw new InvalidOperationException(exceptionMessage);
            });

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Result);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOfType(result.Exception, typeof(InvalidOperationException));
            Assert.AreEqual(exceptionMessage, result.Exception.Message);
            Assert.IsNotNull(result.ResourceUsage);
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestExceptionInAsynchronousExecution()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            const string exceptionMessage = "Async test exception";

            // Act
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(10);
                throw new ArgumentException(exceptionMessage);
            });

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsNull(result.Result);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
            Assert.AreEqual(exceptionMessage, result.Exception.Message);
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestNullFunctionExecution()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Act & Assert - Synchronous null function
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                async () => await sandbox.ExecuteAsync((Func<object>)null));

            // Act & Assert - Asynchronous null function
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                async () => await sandbox.ExecuteAsync((Func<Task<object>>)null));
        }

        #endregion

        #region Resource Usage Tests

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestResourceUsageTracking()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Act
            var result = await sandbox.ExecuteAsync(() =>
            {
                // Allocate some memory
                var data = new byte[1024 * 1024]; // 1MB

                // Do some work
                Thread.Sleep(50);

                return data.Length;
            });

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1024 * 1024, result.Result);
            Assert.IsNotNull(result.ResourceUsage);
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 50);
            Assert.IsTrue(result.ResourceUsage.MemoryUsed >= 0);
            Assert.IsTrue(result.ResourceUsage.ThreadsCreated >= 0);
        }

        [TestMethod]
        [TestCategory("Core")]
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
            Assert.IsTrue(usage.ThreadsCreated >= 1); // At least the current thread
            Assert.IsTrue(usage.ExecutionTime >= 0);
        }

        #endregion

        #region Concurrent Execution Tests

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestConcurrentExecutions()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            const int concurrentTasks = 10;
            var tasks = new Task<SandboxResult>[concurrentTasks];

            // Act - Launch concurrent executions
            for (int i = 0; i < concurrentTasks; i++)
            {
                var taskId = i;
                tasks[i] = sandbox.ExecuteAsync(() =>
                {
                    Thread.Sleep(10); // Simulate work
                    return $"Task {taskId} completed";
                });
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All should succeed
            for (int i = 0; i < concurrentTasks; i++)
            {
                Assert.IsTrue(results[i].Success);
                Assert.AreEqual($"Task {i} completed", results[i].Result);
                Assert.IsNull(results[i].Exception);
            }
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestMixedSyncAsyncConcurrentExecutions()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            var tasks = new List<Task<SandboxResult>>();

            // Act - Mix synchronous and asynchronous executions
            for (int i = 0; i < 5; i++)
            {
                var index = i;

                // Synchronous
                tasks.Add(sandbox.ExecuteAsync(() => $"Sync {index}"));

                // Asynchronous
                tasks.Add(sandbox.ExecuteAsync(async () =>
                {
                    await Task.Delay(5);
                    return $"Async {index}";
                }));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(10, results.Length);
            Assert.IsTrue(results.All(r => r.Success));
            Assert.IsTrue(results.All(r => r.Exception == null));
        }

        #endregion

        #region Disposal Tests

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestDisposedSandboxBehavior()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Act - Dispose the sandbox
            sandbox.Dispose();

            // Assert - IsActive should be false
            Assert.IsFalse(sandbox.IsActive);

            // Assert - Operations should throw
            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
                async () => await sandbox.ExecuteAsync(() => "Should fail"));

            // Act & Assert - Multiple dispose should not throw
            sandbox.Dispose();
        }

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestTerminateAndDispose()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Act - Terminate first
            sandbox.Terminate();
            Assert.IsFalse(sandbox.IsActive);

            // Act - Then dispose
            sandbox.Dispose();
            Assert.IsFalse(sandbox.IsActive);
        }

        #endregion

        #region Cross-Platform Tests

        [TestMethod]
        [TestCategory("Core")]
        public async Task TestCrossPlatformSandbox()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();

            // Act
            await sandbox.InitializeAsync(policy);

            // Assert - Should initialize successfully on any platform
            Assert.IsTrue(sandbox.IsActive);

            // Act - Execute
            var result = await sandbox.ExecuteAsync(() =>
            {
                return Environment.OSVersion.Platform.ToString();
            });

            // Assert
            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Result);

            // The result should be a valid platform
            var platformString = result.Result.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(platformString));
        }

        #endregion
    }
}
