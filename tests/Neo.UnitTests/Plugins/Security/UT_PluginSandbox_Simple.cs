// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginSandbox_Simple.cs file belongs to the neo project and is free
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
    /// <summary>
    /// Simplified unit tests that work with the actual implementation
    /// </summary>
    [TestClass]
    public class UT_PluginSandbox_Simple : SecurityTestBase
    {
        [TestMethod]
        [TestCategory("Core")]
        public async Task TestBasicSandboxFunctionality()
        {
            // Test PassThrough sandbox
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);
            Assert.AreEqual(SandboxType.PassThrough, sandbox.Type);
            
            // Test execution
            var result = await sandbox.ExecuteAsync(() => "Hello World");
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Hello World", result.Result);
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public void TestPermissionFlags()
        {
            // Test basic permission operations
            var permissions = PluginPermissions.ReadOnly;
            
            // Add NetworkAccess
            permissions = permissions | PluginPermissions.NetworkAccess;
            Assert.AreEqual(PluginPermissions.NetworkPlugin, permissions);
            
            // Check individual permissions
            Assert.IsTrue((permissions & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((permissions & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
            Assert.IsFalse((permissions & PluginPermissions.FileSystemAccess) == PluginPermissions.FileSystemAccess);
        }

        [TestMethod]
        [TestCategory("Policy")]
        public void TestSecurityPolicyBasics()
        {
            // Test policy creation
            var defaultPolicy = PluginSecurityPolicy.CreateDefault();
            Assert.IsNotNull(defaultPolicy);
            Assert.AreEqual(PluginPermissions.ReadOnly, defaultPolicy.DefaultPermissions);
            
            var restrictivePolicy = PluginSecurityPolicy.CreateRestrictive();
            Assert.IsNotNull(restrictivePolicy);
            Assert.AreEqual(ViolationAction.Terminate, restrictivePolicy.ViolationAction);
            
            var permissivePolicy = PluginSecurityPolicy.CreatePermissive();
            Assert.IsNotNull(permissivePolicy);
            Assert.AreEqual(ViolationAction.Log, permissivePolicy.ViolationAction);
        }

        [TestMethod]
        [TestCategory("Resources")]
        public async Task TestResourceUsageInExecution()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);
            
            var result = await sandbox.ExecuteAsync(() => 
            {
                var data = new byte[1024];
                return data.Length;
            });
            
            Assert.IsTrue(result.Success);
            Assert.AreEqual(1024, result.Result);
            Assert.IsNotNull(result.ResourceUsage);
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 0);
        }

        [TestMethod]
        [TestCategory("ErrorHandling")]
        public async Task TestExceptionHandling()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);
            
            var result = await sandbox.ExecuteAsync(() => 
            {
                throw new InvalidOperationException("Test exception");
            });
            
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOfType(result.Exception, typeof(InvalidOperationException));
            Assert.AreEqual("Test exception", result.Exception.Message);
        }

        [TestMethod]
        [TestCategory("Concurrency")]
        public async Task TestConcurrentSandboxOperations()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);
            
            // Run multiple operations concurrently
            var tasks = new Task<SandboxResult>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = sandbox.ExecuteAsync(() => $"Task {index}");
            }
            
            var results = await Task.WhenAll(tasks);
            
            // Verify all succeeded
            for (int i = 0; i < results.Length; i++)
            {
                Assert.IsTrue(results[i].Success);
                Assert.AreEqual($"Task {i}", results[i].Result);
            }
        }
    }
}