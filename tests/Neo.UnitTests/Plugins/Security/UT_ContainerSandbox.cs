// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ContainerSandbox.cs file belongs to the neo project and is free
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
    public class UT_ContainerSandbox : SecurityTestBase
    {
        [TestMethod]
        public async Task TestContainerSandboxInitialization()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ContainerSandbox();

            // Test basic properties
            Assert.AreEqual(SandboxType.Container, sandbox.Type);
            Assert.IsFalse(sandbox.IsActive);

            // Initialize sandbox
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);
        }

        [TestMethod]
        public async Task TestContainerSandboxPermissionValidation()
        {
            var policy = PluginSecurityPolicy.CreateRestrictive();
            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test permission validation with restrictive policy
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FileSystemAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FullAccess));
        }

        [TestMethod]
        public async Task TestContainerSandboxExecution()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test simple execution
            var result = await sandbox.ExecuteAsync(() => "Container test");
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Container test", result.Result);
            Assert.IsNotNull(result.ResourceUsage);
        }

        [TestMethod]
        public async Task TestContainerSandboxAsyncExecution()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test async execution
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(50);
                return 42;
            });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Result);
            Assert.IsNotNull(result.ResourceUsage);
            Assert.IsTrue(result.ResourceUsage.ExecutionTime >= 0);
        }

        [TestMethod]
        [Timeout(10000)] // 10 second timeout for this test
        public async Task TestContainerSandboxTimeout()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            policy.MaxExecutionTimeSeconds = 2; // 2 second timeout

            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test timeout handling
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(3000); // Should timeout
                return "Should not complete";
            });

            Assert.IsFalse(result.Success);
            Assert.IsInstanceOfType(result.Exception, typeof(TimeoutException));
        }

        [TestMethod]
        public async Task TestContainerSandboxResourceUsage()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Execute something that uses resources
            await sandbox.ExecuteAsync(() =>
            {
                var data = new byte[1024]; // Allocate some memory
                return data.Length;
            });

            // Test resource usage tracking
            var usage = sandbox.GetResourceUsage();
            Assert.IsNotNull(usage);
            Assert.IsTrue(usage.ExecutionTime >= 0);
            Assert.IsTrue(usage.MemoryUsed >= 0);
        }

        [TestMethod]
        public async Task TestContainerSandboxSuspendResume()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test suspend/resume
            sandbox.Suspend();
            sandbox.Resume();
            Assert.IsTrue(sandbox.IsActive);

            // Test termination
            sandbox.Terminate();
            Assert.IsFalse(sandbox.IsActive);
        }

        [TestMethod]
        public async Task TestContainerSandboxSecurityHooks()
        {
            var policy = PluginSecurityPolicy.CreateRestrictive();
            policy.StrictMode = true;

            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test that security hooks are installed
            // Note: We can't easily test the actual hooks without triggering them,
            // but we can verify the sandbox is configured properly
            Assert.IsTrue(sandbox.IsActive);

            // Execute simple operation to ensure hooks don't interfere with normal execution
            var result = await sandbox.ExecuteAsync(() => "Security test");
            Assert.IsTrue(result.Success);
            Assert.AreEqual("Security test", result.Result);
        }

        [TestMethod]
        public async Task TestContainerSandboxExceptionHandling()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test exception handling
            var result = await sandbox.ExecuteAsync(() =>
            {
                throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
                return "Should not reach here";
#pragma warning restore CS0162
            });

            Assert.IsFalse(result.Success);
            Assert.IsInstanceOfType(result.Exception, typeof(InvalidOperationException));
            Assert.AreEqual("Test exception", result.Exception.Message);
        }

        [TestMethod]
        public void TestContainerSandboxDisposal()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            var sandbox = new ContainerSandbox();

            // Test disposal
            sandbox.Dispose();
            Assert.IsFalse(sandbox.IsActive);

            // Multiple dispose calls should not throw
            sandbox.Dispose();
        }

        [TestMethod]
        public async Task TestContainerSandboxResourceLimits()
        {
            var policy = PluginSecurityPolicy.CreateRestrictive();
            policy.MaxMemoryBytes = 10 * 1024 * 1024; // 10MB
            policy.MaxExecutionTimeSeconds = 5;

            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test that resource limits are applied
            var result = await sandbox.ExecuteAsync(() =>
            {
                // Allocate some memory but stay within limits
                var data = new byte[1024];
                return data.Length;
            });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1024, result.Result);

            // Verify resource usage is tracked
            var usage = sandbox.GetResourceUsage();
            Assert.IsNotNull(usage);
            Assert.IsTrue(usage.MemoryUsed >= 0);
        }

#if NET5_0_OR_GREATER
        [TestMethod]
        public async Task TestContainerSandboxAssemblyLoadContext()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ContainerSandbox();
            await sandbox.InitializeAsync(policy);

            // Test execution in isolated context
            var result = await sandbox.ExecuteAsync(() =>
            {
                // This should execute in the isolated AssemblyLoadContext
                return System.Reflection.Assembly.GetExecutingAssembly().FullName;
            });

            Assert.IsTrue(result.Success);
            Assert.IsNotNull(result.Result);
        }
#endif
    }
}
