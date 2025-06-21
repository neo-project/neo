// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ThreadSafeStateManager.cs file belongs to the neo project and is free
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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    [TestClass]
    public class UT_ThreadSafeStateManager : SecurityTestBase
    {
        [TestMethod]
        [Timeout(5000)] // 5 second timeout
        public void TestThreadSafeStateManagerSingleton()
        {
            var instance1 = ServiceLocator.ThreadSafeStateManager;
            var instance2 = ServiceLocator.ThreadSafeStateManager;

            Assert.IsNotNull(instance1);
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void TestGlobalSecurityConfiguration()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;

            // Test initial state
            Assert.IsFalse(manager.IsSecurityEnabled);
            Assert.AreEqual(SecurityMode.Default, manager.CurrentSecurityMode);

            // Test setting configuration
            manager.SetGlobalSecurityConfiguration(true, SecurityMode.Strict);

            Assert.IsTrue(manager.IsSecurityEnabled);
            Assert.AreEqual(SecurityMode.Strict, manager.CurrentSecurityMode);

            // Test changing configuration
            manager.SetGlobalSecurityConfiguration(false, SecurityMode.Development);

            Assert.IsFalse(manager.IsSecurityEnabled);
            Assert.AreEqual(SecurityMode.Development, manager.CurrentSecurityMode);
        }

        [TestMethod]
        public void TestPluginStateManagement()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;
            const string pluginName = "TestPlugin";

            // Get initial state
            var state = manager.GetPluginState(pluginName);
            Assert.IsNotNull(state);
            Assert.AreEqual(pluginName, state.PluginName);
            Assert.AreEqual(PluginStatus.Unknown, state.Status);

            // Update state
            var updateSuccess = manager.UpdatePluginState(pluginName, s =>
            {
                s.Status = PluginStatus.Running;
                s.AdditionalData["TestKey"] = "TestValue";
            });

            Assert.IsTrue(updateSuccess);

            // Verify state was updated
            var updatedState = manager.GetPluginState(pluginName);
            Assert.AreEqual(PluginStatus.Running, updatedState.Status);
            Assert.AreEqual("TestValue", updatedState.AdditionalData["TestKey"]);

            // Remove state
            manager.RemovePluginState(pluginName);

            // Verify state was removed (new state should be created)
            var newState = manager.GetPluginState(pluginName);
            Assert.AreEqual(PluginStatus.Unknown, newState.Status);
            Assert.AreEqual(0, newState.AdditionalData.Count);
        }

        [TestMethod]
        public void TestConditionalPluginStateUpdate()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;
            const string pluginName = "ConditionalTest";

            // Set initial state
            manager.UpdatePluginState(pluginName, s => s.Status = PluginStatus.Loading);

            // Test successful conditional update
            var success = manager.UpdatePluginStateConditionally(
                pluginName,
                s => s.Status == PluginStatus.Loading,
                s => s.Status = PluginStatus.Running
            );

            Assert.IsTrue(success);
            Assert.AreEqual(PluginStatus.Running, manager.GetPluginState(pluginName).Status);

            // Test failed conditional update
            var failed = manager.UpdatePluginStateConditionally(
                pluginName,
                s => s.Status == PluginStatus.Loading, // Should be false since status is Running
                s => s.Status = PluginStatus.Error
            );

            Assert.IsFalse(failed);
            Assert.AreEqual(PluginStatus.Running, manager.GetPluginState(pluginName).Status); // Should remain Running
        }

        [TestMethod]
        public void TestSecurityContextManagement()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;
            const string pluginName = "SecurityTest";

            // Get security context
            var context = manager.GetSecurityContext(pluginName);
            Assert.IsNotNull(context);
            Assert.AreEqual(pluginName, context.PluginName);
            Assert.IsTrue(context.CreatedAt > DateTime.MinValue);
            Assert.IsTrue(context.LastAccessed > DateTime.MinValue);

            // Verify context caching
            var context2 = manager.GetSecurityContext(pluginName);
            Assert.AreSame(context, context2);

            // Test permission caching
            context.PermissionCache[PluginPermissions.ReadOnly] = true;
            Assert.IsTrue(context.PermissionCache[PluginPermissions.ReadOnly]);

            // Test security data
            context.SecurityData["TestData"] = "TestValue";
            Assert.AreEqual("TestValue", context.SecurityData["TestData"]);
        }

        [TestMethod]
        public async Task TestOperationCoordination()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;
            var results = new ConcurrentBag<int>();

            // Execute multiple coordinated operations
            var tasks = new Task<int>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = manager.ExecuteWithCoordination(async () =>
                {
                    await Task.Delay(10); // Simulate work
                    results.Add(index);
                    return index;
                });
            }

            int[] taskResults = await Task.WhenAll(tasks);

            // Verify all operations completed
            Assert.AreEqual(10, results.Count);
            Assert.AreEqual(10, taskResults.Length);

            // Verify all indices are present
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(results.Contains(i));
                Assert.IsTrue(taskResults.Contains(i));
            }
        }

        [TestMethod]
        public async Task TestExclusiveAccess()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;
            var operationOrder = new ConcurrentQueue<string>();

            // Start a regular coordinated operation
            var coordinatedTask = manager.ExecuteWithCoordination(async () =>
            {
                await Task.Delay(100);
                operationOrder.Enqueue("Coordinated");
                return "coordinated";
            });

            // Wait a bit to ensure the coordinated operation starts
            await Task.Delay(20);

            // Start an exclusive operation
            Task<string> exclusiveTask = manager.ExecuteWithExclusiveAccess(async () =>
            {
                operationOrder.Enqueue("Exclusive");
                await Task.Delay(50);
                return "exclusive";
            });

            // Wait for both to complete
            var results = await Task.WhenAll(coordinatedTask, exclusiveTask);

            Assert.AreEqual(2, operationOrder.Count);
            Assert.IsTrue(results.Contains("coordinated"));
            Assert.IsTrue(results.Contains("exclusive"));
        }

        [TestMethod]
        public async Task TestConcurrentStateUpdates()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;
            const string pluginName = "ConcurrentTest";
            const int threadCount = 20;
            const int operationsPerThread = 50;

            var tasks = new Task[threadCount];
            var updateCounts = new ConcurrentDictionary<int, int>();

            for (int i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        manager.UpdatePluginState($"{pluginName}_{threadIndex}", state =>
                        {
                            state.Status = (PluginStatus)(j % 5); // Cycle through statuses
                            state.AdditionalData[$"Operation_{j}"] = DateTime.UtcNow.Ticks;
                        });

                        updateCounts.AddOrUpdate(threadIndex, 1, (k, v) => v + 1);
                    }
                });
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Verify all updates completed
            Assert.AreEqual(threadCount, updateCounts.Count);
            foreach (var count in updateCounts.Values)
            {
                Assert.AreEqual(operationsPerThread, count);
            }

            // Verify plugin states exist
            var allStates = manager.GetAllPluginStates();
            Assert.AreEqual(threadCount, allStates.Count);
        }

        [TestMethod]
        public void TestStatistics()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;

            // Clear any existing state
            manager.InvalidateAllPluginStates();

            // Create some plugin states
            manager.UpdatePluginState("Plugin1", s => s.Status = PluginStatus.Running);
            manager.UpdatePluginState("Plugin2", s => s.Status = PluginStatus.Suspended);
            manager.GetSecurityContext("Plugin1");
            manager.GetSecurityContext("Plugin3");

            var stats = manager.GetStatistics();

            Assert.IsNotNull(stats);
            Assert.AreEqual(2, stats.PluginStateCount);
            Assert.AreEqual(2, stats.SecurityContextCount);
            Assert.IsTrue(stats.CurrentOperationCount >= 0);
        }

        [TestMethod]
        public void TestInvalidInputHandling()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;

            // Test null/empty plugin names
            Assert.ThrowsExactly<ArgumentNullException>(() => manager.GetPluginState(null));
            Assert.ThrowsExactly<ArgumentNullException>(() => manager.GetPluginState(""));

            Assert.ThrowsExactly<ArgumentNullException>(() => manager.GetSecurityContext(null));
            Assert.ThrowsExactly<ArgumentNullException>(() => manager.GetSecurityContext(""));

            // Test null update actions
            Assert.IsFalse(manager.UpdatePluginState("TestPlugin", null));
            Assert.IsFalse(manager.UpdatePluginStateConditionally("TestPlugin", null, s => { }));
            Assert.IsFalse(manager.UpdatePluginStateConditionally("TestPlugin", s => true, null));
        }

        [TestMethod]
        public async Task TestOperationTimeout()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;

            // Test timeout with very short timeout
            await Assert.ThrowsExactlyAsync<TimeoutException>(async () =>
            {
                await manager.ExecuteWithCoordination(async () =>
                {
                    await Task.Delay(200); // 200ms
                    return "result";
                }, TimeSpan.FromMilliseconds(100)); // 100ms timeout
            });
        }

        [TestMethod]
        public void TestPluginStateInvalidation()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;

            // Create some plugin states
            manager.UpdatePluginState("Plugin1", s => s.Status = PluginStatus.Running);
            manager.UpdatePluginState("Plugin2", s => s.Status = PluginStatus.Suspended);
            manager.GetSecurityContext("Plugin1");

            var statsBefore = manager.GetStatistics();
            Assert.IsTrue(statsBefore.PluginStateCount > 0);
            Assert.IsTrue(statsBefore.SecurityContextCount > 0);

            // Invalidate all states
            manager.InvalidateAllPluginStates();

            var statsAfter = manager.GetStatistics();
            Assert.AreEqual(0, statsAfter.PluginStateCount);
            Assert.AreEqual(0, statsAfter.SecurityContextCount);
        }

        [TestMethod]
        public void TestThreadSafetyOfSecurityConfiguration()
        {
            var manager = ServiceLocator.ThreadSafeStateManager;
            const int threadCount = 10;

            var tasks = new Task[threadCount];
            var results = new ConcurrentBag<(bool enabled, SecurityMode mode)>();

            for (int i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                tasks[i] = Task.Run(() =>
                {
                    // Each thread sets a different configuration
                    var enabled = threadIndex % 2 == 0;
                    var mode = (SecurityMode)(threadIndex % 4);

                    manager.SetGlobalSecurityConfiguration(enabled, mode);

                    // Read back the configuration
                    results.Add((manager.IsSecurityEnabled, manager.CurrentSecurityMode));
                });
            }

            Task.WaitAll(tasks);

            // Verify all operations completed without exceptions
            Assert.AreEqual(threadCount, results.Count);

            // Final state should be consistent
            var finalEnabled = manager.IsSecurityEnabled;
            var finalMode = manager.CurrentSecurityMode;

            // Just verify the values are valid (final state depends on timing)
            Assert.IsTrue(finalMode >= SecurityMode.Default && finalMode <= SecurityMode.Development);
        }
    }
}
