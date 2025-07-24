// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginSecurity.cs file belongs to the neo project and is free
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
    public class UT_PluginSecurity
    {
        [TestInitialize]
        public void SetUp()
        {
            // Set test environment variable
            Environment.SetEnvironmentVariable("DOTNET_TEST_MODE", "true");
        }

        [TestCleanup]
        public void TearDown()
        {
            // Clean up
            Environment.SetEnvironmentVariable("DOTNET_TEST_MODE", null);
        }

        [TestMethod]
        public void TestSecurityPolicyCreation()
        {
            var defaultPolicy = PluginSecurityPolicy.CreateDefault();
            Assert.IsNotNull(defaultPolicy);
            Assert.AreEqual(PluginPermissions.ReadOnly, defaultPolicy.DefaultPermissions);

            var restrictivePolicy = PluginSecurityPolicy.CreateRestrictive();
            Assert.IsNotNull(restrictivePolicy);
            Assert.AreEqual(PluginPermissions.ReadOnly, restrictivePolicy.MaxPermissions);

            var permissivePolicy = PluginSecurityPolicy.CreatePermissive();
            Assert.IsNotNull(permissivePolicy);
            Assert.AreEqual(PluginPermissions.ServicePlugin, permissivePolicy.DefaultPermissions);
        }

        [TestMethod]
        public void TestPluginPermissions()
        {
            var readOnly = PluginPermissions.ReadOnly;
            var networkAccess = PluginPermissions.NetworkAccess;
            var storageAccess = PluginPermissions.StorageAccess;

            Assert.IsTrue((readOnly & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((networkAccess & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
            Assert.IsTrue((storageAccess & PluginPermissions.StorageAccess) == PluginPermissions.StorageAccess);

            var combined = readOnly | networkAccess;
            Assert.IsTrue((combined & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((combined & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
        }

        [TestMethod]
        public void TestSecurityConstants()
        {
            Assert.IsTrue(SecurityConstants.Memory.DefaultMaxMemoryBytes > 0);
            Assert.IsTrue(SecurityConstants.Performance.DefaultMaxCpuPercent > 0);
            Assert.IsTrue(SecurityConstants.Threading.DefaultMaxThreads > 0);
            Assert.IsTrue(SecurityConstants.Timeouts.DefaultExecutionTimeoutSeconds > 0);
        }

        [TestMethod]
        public void TestResourceUsage()
        {
            var usage = new ResourceUsage
            {
                MemoryUsed = 1024 * 1024,
                CpuTimeUsed = 2500,
                ThreadsCreated = 5,
                ExecutionTime = 1000
            };

            Assert.AreEqual(1024 * 1024, usage.MemoryUsed);
            Assert.AreEqual(2500, usage.CpuTimeUsed);
            Assert.AreEqual(5, usage.ThreadsCreated);
            Assert.AreEqual(1000, usage.ExecutionTime);
        }

        [TestMethod]
        public void TestSandboxTypes()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.PassThrough));
            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.AssemblyLoadContext));
            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.Process));
            Assert.IsTrue(Enum.IsDefined(typeof(SandboxType), SandboxType.Container));
        }

        [TestMethod]
        public void TestViolationActions()
        {
            Assert.IsTrue(Enum.IsDefined(typeof(ViolationAction), ViolationAction.Log));
            Assert.IsTrue(Enum.IsDefined(typeof(ViolationAction), ViolationAction.Suspend));
            Assert.IsTrue(Enum.IsDefined(typeof(ViolationAction), ViolationAction.Terminate));
        }

        [TestMethod]
        public void TestServiceLocatorInTestMode()
        {
            // Enable test mode - this should not cause circular dependencies
            ServiceLocator.EnableTestMode();

            // Access test implementations - these should be lightweight mock objects
            var permissionCache = ServiceLocator.PermissionCacheManager;
            var stateManager = ServiceLocator.ThreadSafeStateManager;

            Assert.IsNotNull(permissionCache);
            Assert.IsNotNull(stateManager);

            // Reset for cleanup
            ServiceLocator.Reset();
        }

        [TestMethod]
        public void TestPluginSecurityManagerBasic()
        {
            // Enable test mode first to avoid circular dependencies
            ServiceLocator.EnableTestMode();

            try
            {
                var manager = PluginSecurityManager.Instance;
                Assert.IsNotNull(manager);

                // Test policy operations - these should work with test mocks
                var policy = manager.GetPolicyForPlugin("TestPlugin");
                Assert.IsNotNull(policy);

                manager.SetPolicyForPlugin("TestPlugin", PluginSecurityPolicy.CreateDefault());

                // Permission validation should work with test implementations
                var hasPermission = manager.ValidatePermission("TestPlugin", PluginPermissions.ReadOnly);
                Assert.IsTrue(hasPermission); // Should be true in test mode

                // Cleanup
                ServiceLocator.Reset();
            }
            catch (Exception ex)
            {
                Assert.Fail($"PluginSecurityManager operations should work in test mode: {ex.Message}");
            }
        }
    }
}
