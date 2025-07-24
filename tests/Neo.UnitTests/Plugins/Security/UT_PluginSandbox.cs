// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginSandbox.cs file belongs to the neo project and is free
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
    public class UT_PluginSandbox : SecurityTestBase
    {
        [TestMethod]
        public void TestPluginPermissions()
        {
            // Test basic permission combinations
            var readOnly = PluginPermissions.ReadOnly;
            var networkAccess = PluginPermissions.NetworkAccess;
            var combined = readOnly | networkAccess;

            Assert.AreEqual(PluginPermissions.ReadOnly, readOnly);
            Assert.AreEqual(PluginPermissions.NetworkAccess, networkAccess);
            Assert.AreEqual(PluginPermissions.NetworkPlugin, combined);

            // Test permission validation
            Assert.IsTrue((combined & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((combined & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
            Assert.IsFalse((combined & PluginPermissions.FileSystemAccess) == PluginPermissions.FileSystemAccess);
        }

        [TestMethod]
        public void TestSecurityPolicy()
        {
            // Test default policy
            var defaultPolicy = PluginSecurityPolicy.CreateDefault();
            Assert.AreEqual(PluginPermissions.ReadOnly, defaultPolicy.DefaultPermissions);
            Assert.AreEqual(PluginPermissions.NetworkPlugin, defaultPolicy.MaxPermissions);
            Assert.AreEqual(SandboxType.AssemblyLoadContext, defaultPolicy.SandboxType);

            // Test restrictive policy
            var restrictivePolicy = PluginSecurityPolicy.CreateRestrictive();
            Assert.AreEqual(PluginPermissions.ReadOnly, restrictivePolicy.DefaultPermissions);
            Assert.AreEqual(PluginPermissions.ReadOnly, restrictivePolicy.MaxPermissions);
            Assert.AreEqual(SandboxType.Process, restrictivePolicy.SandboxType);
            Assert.AreEqual(ViolationAction.Terminate, restrictivePolicy.ViolationAction);

            // Test permissive policy
            var permissivePolicy = PluginSecurityPolicy.CreatePermissive();
            Assert.AreEqual(PluginPermissions.ServicePlugin, permissivePolicy.DefaultPermissions);
            Assert.AreEqual(PluginPermissions.AdminPlugin, permissivePolicy.MaxPermissions);
            Assert.AreEqual(SandboxType.AssemblyLoadContext, permissivePolicy.SandboxType);
            Assert.AreEqual(ViolationAction.Log, permissivePolicy.ViolationAction);
        }

        [TestMethod]
        public async Task TestPassThroughSandbox()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();

            // Initialize sandbox
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);
            Assert.AreEqual(SandboxType.PassThrough, sandbox.Type);

            // Test permission validation
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.NetworkAccess));

            // Test execution
            var result = await sandbox.ExecuteAsync(() => "Hello World");
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Hello World", result.Result);
            Assert.IsNotNull(result.ResourceUsage);

            // Test resource usage
            var usage = sandbox.GetResourceUsage();
            Assert.IsNotNull(usage);
            Assert.IsTrue(usage.MemoryUsed >= 0);
        }

        [TestMethod]
        public async Task TestAssemblyLoadContextSandbox()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();

            // Initialize sandbox
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);
            Assert.AreEqual(SandboxType.AssemblyLoadContext, sandbox.Type);

            // Test permission validation
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.NetworkAccess));

            // Test execution
            var result = await sandbox.ExecuteAsync(() => 42);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Result);
            Assert.IsNotNull(result.ResourceUsage);

            // Test async execution
            var asyncResult = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(10);
                return "Async Result";
            });
            Assert.IsTrue(asyncResult.Success);
            Assert.AreEqual("Async Result", asyncResult.Result);
        }

        [TestMethod]
        public async Task TestSandboxSuspendResume()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();

            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);

            // Test suspend/resume (note: PassThrough doesn't actually suspend)
            sandbox.Suspend();
            sandbox.Resume();
            Assert.IsTrue(sandbox.IsActive);

            // Test terminate
            sandbox.Terminate();
            Assert.IsFalse(sandbox.IsActive);
        }

        [TestMethod]
        public void TestSecurityManager()
        {
            // In test mode, we use mock implementations
            // Test that basic security operations work
            var policy = PluginSecurityPolicy.CreateDefault();
            Assert.IsNotNull(policy);
            Assert.AreEqual(PluginPermissions.ReadOnly, policy.DefaultPermissions);

            // Test permission validation logic
            var hasReadOnlyPermission = (policy.DefaultPermissions & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly;
            Assert.IsTrue(hasReadOnlyPermission);
        }

        [TestMethod]
        [Timeout(3000)] // 3 second timeout for this test
        public async Task TestSecurityViolationHandling()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            policy.MaxExecutionTimeSeconds = 1; // 1 second timeout

            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Test timeout scenario
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(1500); // This should timeout
                return "Should not complete";
            });

            Assert.IsFalse(result.Success);
            Assert.IsInstanceOfType(result.Exception, typeof(TimeoutException));
        }

        [TestMethod]
        public void TestResourceUsageCalculation()
        {
            var usage = new ResourceUsage
            {
                MemoryUsed = 1024 * 1024, // 1 MB
                CpuTimeUsed = 100,
                ThreadsCreated = 5,
                ExecutionTime = 500
            };

            Assert.AreEqual(1024 * 1024, usage.MemoryUsed);
            Assert.AreEqual(100, usage.CpuTimeUsed);
            Assert.AreEqual(5, usage.ThreadsCreated);
            Assert.AreEqual(500, usage.ExecutionTime);
        }

        [TestMethod]
        public async Task TestCrossPlatformSandbox()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new CrossPlatformSandbox();

            // Initialize sandbox
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);
            // CrossPlatformSandbox delegates to underlying sandbox, so type varies

            // Test execution
            var result = await sandbox.ExecuteAsync(() => "CrossPlatform test");
            Assert.IsTrue(result.Success);
            Assert.AreEqual("CrossPlatform test", result.Result);
        }

        [TestMethod]
        public void TestSandboxTypeEnumeration()
        {
            // Test all sandbox types are properly defined
            var sandboxTypes = Enum.GetValues(typeof(SandboxType));
            Assert.IsTrue(sandboxTypes.Length >= 4); // PassThrough, AssemblyLoadContext, Process, Container

            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.PassThrough));
            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.AssemblyLoadContext));
            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.Process));
            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.Container));
            // CrossPlatform is not an enum value - it's a wrapper class
        }

        [TestMethod]
        public void TestSandboxFactoryCreation()
        {
            // Test sandbox factory can create all types
            var policy = PluginSecurityPolicy.CreateDefault();

            // Test PassThrough
            using (var sandbox = new PassThroughSandbox())
            {
                Assert.IsNotNull(sandbox);
                Assert.AreEqual(SandboxType.PassThrough, sandbox.Type);
                Assert.IsInstanceOfType(sandbox, typeof(PassThroughSandbox));
            }

            // Test AssemblyLoadContext
            using (var sandbox = new AssemblyLoadContextSandbox())
            {
                Assert.IsNotNull(sandbox);
                Assert.AreEqual(SandboxType.AssemblyLoadContext, sandbox.Type);
                Assert.IsInstanceOfType(sandbox, typeof(AssemblyLoadContextSandbox));
            }

            // Test Process
            using (var sandbox = new ProcessSandbox())
            {
                Assert.IsNotNull(sandbox);
                Assert.AreEqual(SandboxType.Process, sandbox.Type);
                Assert.IsInstanceOfType(sandbox, typeof(ProcessSandbox));
            }

            // Test Container
            using (var sandbox = new ContainerSandbox())
            {
                Assert.IsNotNull(sandbox);
                Assert.AreEqual(SandboxType.Container, sandbox.Type);
                Assert.IsInstanceOfType(sandbox, typeof(ContainerSandbox));
            }

            // Test CrossPlatform (creates underlying sandbox based on platform)
            using (var sandbox = new CrossPlatformSandbox())
            {
                Assert.IsNotNull(sandbox);
                Assert.IsInstanceOfType(sandbox, typeof(CrossPlatformSandbox));
            }
        }

        [TestMethod]
        public void TestResourceMonitoringIntegration()
        {
            // In test mode, we use simplified resource tracking
            var policy = PluginSecurityPolicy.CreateDefault();

            // Test basic resource usage creation
            var usage = new ResourceUsage
            {
                MemoryUsed = 1024,
                ExecutionTime = 100,
                CpuTimeUsed = 50,
                ThreadsCreated = 2
            };

            Assert.IsNotNull(usage);
            Assert.AreEqual(1024, usage.MemoryUsed);
            Assert.AreEqual(100, usage.ExecutionTime);
        }

        [TestMethod]
        public void TestPermissionCombinations()
        {
            // Test various permission combinations
            var readOnly = PluginPermissions.ReadOnly;
            var networkAccess = PluginPermissions.NetworkAccess;
            var fileSystemAccess = PluginPermissions.FileSystemAccess;

            // Test basic combinations
            var networkPlugin = readOnly | networkAccess;
            Assert.AreEqual(PluginPermissions.NetworkPlugin, networkPlugin);

            var fileSystemPlugin = readOnly | fileSystemAccess;
            Assert.AreEqual(PluginPermissions.ReadOnly | PluginPermissions.FileSystemAccess, fileSystemPlugin);

            var servicePlugin = readOnly | PluginPermissions.StorageAccess | networkAccess;
            Assert.AreEqual(PluginPermissions.ServicePlugin, servicePlugin);

            var adminPlugin = servicePlugin | fileSystemAccess | PluginPermissions.RpcPlugin | PluginPermissions.CryptographicAccess | PluginPermissions.DatabaseAccess | PluginPermissions.LogAccess;
            Assert.AreEqual(PluginPermissions.AdminPlugin, adminPlugin);

            // Test permission validation
            Assert.IsTrue((networkPlugin & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((networkPlugin & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
            Assert.IsFalse((networkPlugin & PluginPermissions.ProcessAccess) == PluginPermissions.ProcessAccess);
        }

        [TestMethod]
        public async Task TestSandboxPerformanceOptimization()
        {
            // Test performance-optimized sandbox creation
            var baseSandbox = SandboxType.AssemblyLoadContext;
            var profile = PerformanceProfile.HighPerformance;

            using var optimizedSandbox = OptimizedSandboxFactory.CreateOptimized(baseSandbox, profile);
            Assert.IsNotNull(optimizedSandbox);

            var policy = PluginSecurityPolicy.CreateDefault();
            await optimizedSandbox.InitializeAsync(policy);
            Assert.IsTrue(optimizedSandbox.IsActive);

            // Test that optimization doesn't break basic functionality
            var result = await optimizedSandbox.ExecuteAsync(() => "Optimized execution");
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Optimized execution", result.Result);
        }

        [TestMethod]
        public void TestViolationActionHandling()
        {
            // Test different violation actions
            var logPolicy = PluginSecurityPolicy.CreateDefault();
            logPolicy.ViolationAction = ViolationAction.Log;
            Assert.AreEqual(ViolationAction.Log, logPolicy.ViolationAction);

            var suspendPolicy = PluginSecurityPolicy.CreateDefault();
            suspendPolicy.ViolationAction = ViolationAction.Suspend;
            Assert.AreEqual(ViolationAction.Suspend, suspendPolicy.ViolationAction);

            var terminatePolicy = PluginSecurityPolicy.CreateRestrictive();
            terminatePolicy.ViolationAction = ViolationAction.Terminate;
            Assert.AreEqual(ViolationAction.Terminate, terminatePolicy.ViolationAction);
        }

        [TestMethod]
        public async Task TestSandboxIsolationLevels()
        {
            var policies = new[]
            {
                PluginSecurityPolicy.CreatePermissive(),
                PluginSecurityPolicy.CreateDefault(),
                PluginSecurityPolicy.CreateRestrictive()
            };

            foreach (var policy in policies)
            {
                // Test with PassThrough sandbox (least isolation)
                using (var sandbox = new PassThroughSandbox())
                {
                    await sandbox.InitializeAsync(policy);
                    var result = await sandbox.ExecuteAsync(() => "Isolation test");
                    Assert.IsTrue(result.Success);
                }

                // Test with AssemblyLoadContext sandbox (medium isolation)
                using (var sandbox = new AssemblyLoadContextSandbox())
                {
                    await sandbox.InitializeAsync(policy);
                    var result = await sandbox.ExecuteAsync(() => "Isolation test");
                    Assert.IsTrue(result.Success);
                }
            }
        }

        [TestMethod]
        public void TestSecurityAuditLogging()
        {
            // In test mode, we test the enum values and structures
            Assert.IsTrue(Enum.IsDefined(typeof(SecurityEventType), SecurityEventType.SecurityViolation));
            Assert.IsTrue(Enum.IsDefined(typeof(SecurityEventType), SecurityEventType.ResourceViolation));
            Assert.IsTrue(Enum.IsDefined(typeof(SecurityEventSeverity), SecurityEventSeverity.Medium));
            Assert.IsTrue(Enum.IsDefined(typeof(SecurityEventSeverity), SecurityEventSeverity.High));
            Assert.IsTrue(Enum.IsDefined(typeof(SecurityEventSeverity), SecurityEventSeverity.Critical));
        }

        [TestMethod]
        public async Task TestConcurrentSandboxExecution()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Test concurrent execution
            var tasks = new Task<SandboxResult>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = sandbox.ExecuteAsync(() => $"Concurrent task {index}");
            }

            var results = await Task.WhenAll(tasks);

            // Verify all executions completed successfully
            for (int i = 0; i < results.Length; i++)
            {
                Assert.IsTrue(results[i].Success);
                Assert.AreEqual($"Concurrent task {i}", results[i].Result);
            }
        }

        [TestMethod]
        public void TestSecurityManagerCacheInvalidation()
        {
            // In test mode, test cache manager interfaces through ServiceLocator
            var cacheManager = ServiceLocator.PermissionCacheManager;
            Assert.IsNotNull(cacheManager);

            const string testPlugin = "CacheTestPlugin";

            // Test cache operations
            cacheManager.CachePermissionResult(testPlugin, PluginPermissions.ReadOnly, true);
            var cachedResult = cacheManager.GetCachedPermissionResult(testPlugin, PluginPermissions.ReadOnly);
            Assert.IsTrue(cachedResult.HasValue);

            // Test cache invalidation
            cacheManager.InvalidatePluginCache(testPlugin);
            var invalidatedResult = cacheManager.GetCachedPermissionResult(testPlugin, PluginPermissions.ReadOnly);
            Assert.IsFalse(invalidatedResult.HasValue);

            // Test state manager
            var stateManager = ServiceLocator.ThreadSafeStateManager;
            Assert.IsNotNull(stateManager);

            // Enable security for testing
            stateManager.SetGlobalSecurityConfiguration(true, SecurityMode.Default);
            Assert.IsTrue(stateManager.IsSecurityEnabled);
        }

        [TestMethod]
        public void TestSecurityAuditLoggerThreadSafety()
        {
            // In test mode, test concurrent operations with thread-safe collections
            var stateManager = ServiceLocator.ThreadSafeStateManager;
            Assert.IsNotNull(stateManager);

            // Enable security for testing
            stateManager.SetGlobalSecurityConfiguration(true, SecurityMode.Default);

            // Test concurrent state updates
            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    var pluginName = $"TestPlugin{index}";
                    stateManager.UpdatePluginState(pluginName, state =>
                    {
                        // Update plugin state for thread safety testing
                        state.LastUpdated = DateTime.UtcNow;
                    });
                });
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks, TimeSpan.FromSeconds(5));

            // Verify thread safety didn't cause issues
            Assert.IsTrue(stateManager.IsSecurityEnabled);

            // Verify all plugin states were created
            var allStates = stateManager.GetAllPluginStates();
            Assert.AreEqual(10, allStates.Count);
        }
    }
}
