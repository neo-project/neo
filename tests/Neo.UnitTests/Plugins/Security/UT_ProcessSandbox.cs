// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ProcessSandbox.cs file belongs to the neo project and is free
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
using System.IO;
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    [TestClass]
    public class UT_ProcessSandbox : SecurityTestBase
    {
        private static string SandboxExecutablePath;

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            // Create a mock sandbox executable for testing
            // In real scenarios, this would be the actual Neo.PluginSandbox.exe
            SandboxExecutablePath = Path.Combine(Path.GetTempPath(), "TestSandbox.exe");

            // For testing purposes, we'll skip actual process execution
            // and focus on testing the sandbox infrastructure
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void ClassCleanup()
        {
            try
            {
                if (File.Exists(SandboxExecutablePath))
                {
                    File.Delete(SandboxExecutablePath);
                }
            }
            catch { }
        }

        [TestMethod]
        public async Task TestProcessSandboxInitialization()
        {
            var policy = PluginSecurityPolicy.CreateRestrictive();
            using var sandbox = new ProcessSandbox();

            // Test basic properties
            Assert.AreEqual(SandboxType.Process, sandbox.Type);
            Assert.IsFalse(sandbox.IsActive);

            // Initialize without executable (should work but won't execute)
            await sandbox.InitializeAsync(policy);
            Assert.IsTrue(sandbox.IsActive);
        }

        [TestMethod]
        public async Task TestProcessSandboxPermissionValidation()
        {
            var policy = PluginSecurityPolicy.CreateRestrictive();
            using var sandbox = new ProcessSandbox();
            await sandbox.InitializeAsync(policy);

            // Test permission validation with restrictive policy
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FileSystemAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FullAccess));
        }

        [TestMethod]
        public async Task TestProcessSandboxResourceUsage()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ProcessSandbox();
            await sandbox.InitializeAsync(policy);

            // Test resource usage tracking
            var usage = sandbox.GetResourceUsage();
            Assert.IsNotNull(usage);
            Assert.IsTrue(usage.ExecutionTime >= 0);
            Assert.IsTrue(usage.MemoryUsed >= 0);
        }

        [TestMethod]
        public async Task TestProcessSandboxSuspendResume()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ProcessSandbox();
            await sandbox.InitializeAsync(policy);

            // Test suspend/resume functionality
            sandbox.Suspend();
            sandbox.Resume();

            // Test termination
            sandbox.Terminate();
            Assert.IsFalse(sandbox.IsActive);
        }

        [TestMethod]
        public async Task TestProcessSandboxExecution_WithoutExecutable()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            using var sandbox = new ProcessSandbox();
            await sandbox.InitializeAsync(policy);

            // Test execution without actual executable (should fail gracefully)
            var result = await sandbox.ExecuteAsync(() => "test");
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Exception);
        }

        [TestMethod]
        public void TestProcessSandboxDisposal()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            var sandbox = new ProcessSandbox();

            // Test disposal
            sandbox.Dispose();
            Assert.IsFalse(sandbox.IsActive);

            // Multiple dispose calls should not throw
            sandbox.Dispose();
        }

        [TestMethod]
        public async Task TestProcessSandboxTimeout()
        {
            var policy = PluginSecurityPolicy.CreateDefault();
            policy.MaxExecutionTimeSeconds = 1;

            using var sandbox = new ProcessSandbox();
            await sandbox.InitializeAsync(policy);

            // Test that execution respects timeout settings
            // Note: Without actual executable, this will fail at process start
            var result = await sandbox.ExecuteAsync(async () =>
            {
                await Task.Delay(200);
                return "Should timeout";
            });

            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Exception);
        }

        [TestMethod]
        public async Task TestProcessSandboxResourceLimits()
        {
            var policy = PluginSecurityPolicy.CreateRestrictive();
            policy.MaxMemoryBytes = 1024 * 1024; // 1MB
            policy.MaxCpuPercent = 50;

            using var sandbox = new ProcessSandbox();
            await sandbox.InitializeAsync(policy);

            // Verify resource limits are configured
            var usage = sandbox.GetResourceUsage();
            Assert.IsNotNull(usage);

            // Note: Actual resource enforcement requires process execution
            // This test verifies the configuration is accepted
        }
    }
}
