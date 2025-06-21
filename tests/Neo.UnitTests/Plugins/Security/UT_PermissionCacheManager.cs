// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PermissionCacheManager.cs file belongs to the neo project and is free
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
    public class UT_PermissionCacheManager : SecurityTestBase
    {
        [TestMethod]
        public void TestPermissionCacheManagerSingleton()
        {
            var instance1 = ServiceLocator.PermissionCacheManager;
            var instance2 = ServiceLocator.PermissionCacheManager;

            Assert.IsNotNull(instance1);
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void TestPermissionCaching()
        {
            var manager = ServiceLocator.PermissionCacheManager;
            const string pluginName = "TestPlugin";
            const PluginPermissions permission = PluginPermissions.ReadOnly;
            const bool expectedResult = true;

            // Initially should return null (not cached)
            var result = manager.GetCachedPermissionResult(pluginName, permission);
            Assert.IsNull(result);

            // Cache the result
            manager.CachePermissionResult(pluginName, permission, expectedResult);

            // Should now return the cached result
            result = manager.GetCachedPermissionResult(pluginName, permission);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResult, result.Value);
        }

        [TestMethod]
        public void TestPolicyCaching()
        {
            var manager = ServiceLocator.PermissionCacheManager;
            const string pluginName = "TestPolicy";
            var policy = PluginSecurityPolicy.CreateDefault();

            // Initially should return null (not cached)
            var cachedPolicy = manager.GetCachedPolicy(pluginName);
            Assert.IsNull(cachedPolicy);

            // Cache the policy
            manager.CachePolicy(pluginName, policy);

            // Should now return the cached policy
            cachedPolicy = manager.GetCachedPolicy(pluginName);
            Assert.IsNotNull(cachedPolicy);
            Assert.AreEqual(policy.DefaultPermissions, cachedPolicy.DefaultPermissions);
            Assert.AreEqual(policy.MaxPermissions, cachedPolicy.MaxPermissions);
        }

        [TestMethod]
        public void TestPluginCacheInvalidation()
        {
            var manager = ServiceLocator.PermissionCacheManager;
            const string pluginName = "InvalidationTest";
            const PluginPermissions permission1 = PluginPermissions.ReadOnly;
            const PluginPermissions permission2 = PluginPermissions.NetworkAccess;
            var policy = PluginSecurityPolicy.CreateDefault();

            // Cache permission results and policy
            manager.CachePermissionResult(pluginName, permission1, true);
            manager.CachePermissionResult(pluginName, permission2, false);
            manager.CachePolicy(pluginName, policy);

            // Verify cached
            Assert.IsNotNull(manager.GetCachedPermissionResult(pluginName, permission1));
            Assert.IsNotNull(manager.GetCachedPermissionResult(pluginName, permission2));
            Assert.IsNotNull(manager.GetCachedPolicy(pluginName));

            // Invalidate plugin cache
            manager.InvalidatePluginCache(pluginName);

            // Should all be null after invalidation
            Assert.IsNull(manager.GetCachedPermissionResult(pluginName, permission1));
            Assert.IsNull(manager.GetCachedPermissionResult(pluginName, permission2));
            Assert.IsNull(manager.GetCachedPolicy(pluginName));
        }

        [TestMethod]
        public void TestPermissionTypeInvalidation()
        {
            var manager = ServiceLocator.PermissionCacheManager;
            const string plugin1 = "Plugin1";
            const string plugin2 = "Plugin2";
            const PluginPermissions networkPermission = PluginPermissions.NetworkAccess;
            const PluginPermissions readOnlyPermission = PluginPermissions.ReadOnly;

            // Cache different permissions for different plugins
            manager.CachePermissionResult(plugin1, networkPermission, true);
            manager.CachePermissionResult(plugin1, readOnlyPermission, true);
            manager.CachePermissionResult(plugin2, networkPermission, false);
            manager.CachePermissionResult(plugin2, readOnlyPermission, true);

            // Verify all cached
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin1, networkPermission));
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin1, readOnlyPermission));
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin2, networkPermission));
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin2, readOnlyPermission));

            // Invalidate network permission across all plugins
            manager.InvalidatePermissionCache(networkPermission);

            // Network permissions should be invalidated, others should remain
            Assert.IsNull(manager.GetCachedPermissionResult(plugin1, networkPermission));
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin1, readOnlyPermission));
            Assert.IsNull(manager.GetCachedPermissionResult(plugin2, networkPermission));
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin2, readOnlyPermission));
        }

        [TestMethod]
        public void TestAllCacheInvalidation()
        {
            var manager = ServiceLocator.PermissionCacheManager;
            const string plugin1 = "ClearTest1";
            const string plugin2 = "ClearTest2";
            var policy1 = PluginSecurityPolicy.CreateDefault();
            var policy2 = PluginSecurityPolicy.CreateRestrictive();

            // Cache multiple entries
            manager.CachePermissionResult(plugin1, PluginPermissions.ReadOnly, true);
            manager.CachePermissionResult(plugin2, PluginPermissions.NetworkAccess, false);
            manager.CachePolicy(plugin1, policy1);
            manager.CachePolicy(plugin2, policy2);

            // Verify cached
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin1, PluginPermissions.ReadOnly));
            Assert.IsNotNull(manager.GetCachedPermissionResult(plugin2, PluginPermissions.NetworkAccess));
            Assert.IsNotNull(manager.GetCachedPolicy(plugin1));
            Assert.IsNotNull(manager.GetCachedPolicy(plugin2));

            // Invalidate all caches
            manager.InvalidateAllCaches();

            // All should be null after invalidation
            Assert.IsNull(manager.GetCachedPermissionResult(plugin1, PluginPermissions.ReadOnly));
            Assert.IsNull(manager.GetCachedPermissionResult(plugin2, PluginPermissions.NetworkAccess));
            Assert.IsNull(manager.GetCachedPolicy(plugin1));
            Assert.IsNull(manager.GetCachedPolicy(plugin2));
        }

        [TestMethod]
        public void TestCacheStatistics()
        {
            var manager = ServiceLocator.PermissionCacheManager;

            // Clear cache first
            manager.InvalidateAllCaches();

            var initialStats = manager.GetCacheStatistics();
            Assert.AreEqual(0, initialStats.PermissionCacheSize);
            Assert.AreEqual(0, initialStats.PolicyCacheSize);

            // Add some entries
            manager.CachePermissionResult("Plugin1", PluginPermissions.ReadOnly, true);
            manager.CachePermissionResult("Plugin2", PluginPermissions.NetworkAccess, false);
            manager.CachePolicy("Plugin1", PluginSecurityPolicy.CreateDefault());

            var updatedStats = manager.GetCacheStatistics();
            Assert.AreEqual(2, updatedStats.PermissionCacheSize);
            Assert.AreEqual(1, updatedStats.PolicyCacheSize);
            Assert.IsTrue(updatedStats.MaxCacheSize > 0);
        }

        [TestMethod]
        public void TestConcurrentCacheOperations()
        {
            var manager = ServiceLocator.PermissionCacheManager;
            const int threadCount = 10;
            const int operationsPerThread = 100;
            var tasks = new Task[threadCount];

            // Clear cache first
            manager.InvalidateAllCaches();

            for (int i = 0; i < threadCount; i++)
            {
                var threadIndex = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        var pluginName = $"Plugin{threadIndex}";
                        var permission = (PluginPermissions)(1 << (j % 10));

                        // Perform cache operations
                        manager.CachePermissionResult(pluginName, permission, j % 2 == 0);
                        var result = manager.GetCachedPermissionResult(pluginName, permission);

                        if (j % 20 == 0)
                        {
                            // Occasionally invalidate plugin cache
                            manager.InvalidatePluginCache(pluginName);
                        }
                    }
                });
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks, TimeSpan.FromSeconds(30));

            // Verify no exceptions were thrown
            foreach (var task in tasks)
            {
                Assert.IsTrue(task.IsCompletedSuccessfully);
            }
        }

        [TestMethod]
        public async Task TestCacheExpiration()
        {
            var manager = ServiceLocator.PermissionCacheManager;
            const string pluginName = "ExpirationTest";
            const PluginPermissions permission = PluginPermissions.ReadOnly;

            // Cache a result
            manager.CachePermissionResult(pluginName, permission, true);

            // Should be cached initially
            var result = manager.GetCachedPermissionResult(pluginName, permission);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value);

            // Note: We can't easily test expiration without waiting for the actual timeout
            // or having a way to manipulate the cache timeout for testing.
            // This test verifies that the caching mechanism works as expected.

            // Verify that the cache is still working after a short delay
            await Task.Delay(100);
            result = manager.GetCachedPermissionResult(pluginName, permission);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value);
        }

        [TestMethod]
        public void TestCacheWithNullOrEmptyPluginName()
        {
            var manager = ServiceLocator.PermissionCacheManager;

            // Test with null plugin name
            var result = manager.GetCachedPermissionResult(null, PluginPermissions.ReadOnly);
            Assert.IsNull(result);

            manager.CachePermissionResult(null, PluginPermissions.ReadOnly, true);
            result = manager.GetCachedPermissionResult(null, PluginPermissions.ReadOnly);
            Assert.IsNull(result); // Should not cache null plugin names

            // Test with empty plugin name
            result = manager.GetCachedPermissionResult("", PluginPermissions.ReadOnly);
            Assert.IsNull(result);

            manager.CachePermissionResult("", PluginPermissions.ReadOnly, true);
            result = manager.GetCachedPermissionResult("", PluginPermissions.ReadOnly);
            Assert.IsNull(result); // Should not cache empty plugin names
        }

        [TestMethod]
        public void TestPolicyCacheWithNullValues()
        {
            var manager = ServiceLocator.PermissionCacheManager;

            // Test with null plugin name
            var policy = manager.GetCachedPolicy(null);
            Assert.IsNull(policy);

            manager.CachePolicy(null, PluginSecurityPolicy.CreateDefault());
            policy = manager.GetCachedPolicy(null);
            Assert.IsNull(policy); // Should not cache null plugin names

            // Test with null policy
            manager.CachePolicy("TestPlugin", null);
            policy = manager.GetCachedPolicy("TestPlugin");
            Assert.IsNull(policy); // Should not cache null policies
        }

        [TestMethod]
        public void TestDisposedManagerBehavior()
        {
            // Note: We can't easily test the actual disposal behavior since we're using a singleton
            // This test verifies that the manager handles disposal-related scenarios gracefully
            var manager = ServiceLocator.PermissionCacheManager;

            // These operations should work normally
            manager.CachePermissionResult("DisposeTest", PluginPermissions.ReadOnly, true);
            var result = manager.GetCachedPermissionResult("DisposeTest", PluginPermissions.ReadOnly);
            Assert.IsNotNull(result);

            // Clear the cache entry
            manager.InvalidatePluginCache("DisposeTest");
            result = manager.GetCachedPermissionResult("DisposeTest", PluginPermissions.ReadOnly);
            Assert.IsNull(result);
        }
    }
}
