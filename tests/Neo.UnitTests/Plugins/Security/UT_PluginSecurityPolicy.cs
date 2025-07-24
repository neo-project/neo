// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginSecurityPolicy.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.Plugins.Security
{
    /// <summary>
    /// Unit tests for PluginSecurityPolicy.
    /// Tests policy creation, validation, and configuration.
    /// </summary>
    [TestClass]
    public class UT_PluginSecurityPolicy : SecurityTestBase
    {
        #region Policy Creation Tests

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestDefaultPolicyCreation()
        {
            // Act
            var policy = PluginSecurityPolicy.CreateDefault();

            // Assert
            Assert.IsNotNull(policy);
            Assert.AreEqual(PluginPermissions.ReadOnly, policy.DefaultPermissions);
            Assert.AreEqual(PluginPermissions.NetworkPlugin, policy.MaxPermissions);
            Assert.IsTrue(policy.StrictMode);
            Assert.IsTrue(policy.RequireSignedPlugins);
            Assert.IsNotNull(policy.FileSystemAccess);
            Assert.IsNotNull(policy.NetworkAccess);
            Assert.IsNotNull(policy.ResourceMonitoring);
        }

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestRestrictivePolicyCreation()
        {
            // Act
            var policy = PluginSecurityPolicy.CreateRestrictive();

            // Assert
            Assert.IsNotNull(policy);
            Assert.AreEqual(PluginPermissions.ReadOnly, policy.DefaultPermissions);
            Assert.AreEqual(PluginPermissions.ReadOnly, policy.MaxPermissions);
            Assert.IsTrue(policy.StrictMode);
            Assert.IsTrue(policy.RequireSignedPlugins);
            Assert.AreEqual(ViolationAction.Terminate, policy.ViolationAction);
            Assert.AreEqual(SandboxType.Process, policy.SandboxType);
        }

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestPermissivePolicyCreation()
        {
            // Act
            var policy = PluginSecurityPolicy.CreatePermissive();

            // Assert
            Assert.IsNotNull(policy);
            Assert.AreEqual(PluginPermissions.ServicePlugin, policy.DefaultPermissions);
            Assert.AreEqual(PluginPermissions.AdminPlugin, policy.MaxPermissions);
            Assert.IsFalse(policy.StrictMode);
            Assert.IsFalse(policy.RequireSignedPlugins);
            Assert.AreEqual(ViolationAction.Log, policy.ViolationAction);
        }

        #endregion

        #region Resource Limits Tests

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestResourceLimitsConfiguration()
        {
            // Arrange
            var policy = new PluginSecurityPolicy
            {
                MaxMemoryBytes = 512 * 1024 * 1024, // 512 MB
                MaxCpuPercent = 75.0,
                MaxThreads = 20,
                MaxExecutionTimeSeconds = 30
            };

            // Assert
            Assert.AreEqual(512 * 1024 * 1024, policy.MaxMemoryBytes);
            Assert.AreEqual(75.0, policy.MaxCpuPercent);
            Assert.AreEqual(20, policy.MaxThreads);
            Assert.AreEqual(30, policy.MaxExecutionTimeSeconds);
        }

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestResourceLimitsValidation()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();

            // Assert - Default limits should be reasonable
            Assert.IsTrue(policy.MaxMemoryBytes > 0);
            Assert.IsTrue(policy.MaxCpuPercent > 0 && policy.MaxCpuPercent <= 100);
            Assert.IsTrue(policy.MaxThreads > 0);
            Assert.IsTrue(policy.MaxExecutionTimeSeconds > 0);
        }

        #endregion

        #region Permission Validation Tests

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestPermissionValidation()
        {
            // Arrange
            var policy = new PluginSecurityPolicy
            {
                DefaultPermissions = PluginPermissions.ReadOnly | PluginPermissions.NetworkAccess,
                MaxPermissions = PluginPermissions.ServicePlugin
            };

            // Act & Assert - Within limits
            Assert.IsTrue(ValidatePermission(policy, PluginPermissions.ReadOnly));
            Assert.IsTrue(ValidatePermission(policy, PluginPermissions.NetworkAccess));
            Assert.IsTrue(ValidatePermission(policy, PluginPermissions.StorageAccess)); // Within MaxPermissions

            // Act & Assert - Beyond limits
            Assert.IsFalse(ValidatePermission(policy, PluginPermissions.FileSystemAccess));
            Assert.IsFalse(ValidatePermission(policy, PluginPermissions.ProcessAccess));
            Assert.IsFalse(ValidatePermission(policy, PluginPermissions.AdminAccess));
        }

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestPolicyConsistency()
        {
            // Arrange
            var policy = new PluginSecurityPolicy
            {
                DefaultPermissions = PluginPermissions.AdminPlugin,
                MaxPermissions = PluginPermissions.NetworkPlugin
            };

            // Assert - Validate should detect inconsistency
            var isValid = ValidatePolicyConsistency(policy);
            Assert.IsFalse(isValid);

            // Act - Fix inconsistency
            policy.DefaultPermissions = PluginPermissions.NetworkPlugin;

            // Assert - Should now be valid
            isValid = ValidatePolicyConsistency(policy);
            Assert.IsTrue(isValid);
        }

        #endregion

        #region Network Policy Tests

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestNetworkAccessPolicy()
        {
            // Arrange
            var policy = new PluginSecurityPolicy();
            policy.NetworkAccess.AllowedEndpoints.Add("api.neo.org");
            policy.NetworkAccess.BlockedEndpoints.Add("malicious.com");
            policy.NetworkAccess.AllowedPorts.Add(8080);
            policy.NetworkAccess.RequireSSL = true;

            // Assert
            Assert.IsTrue(policy.NetworkAccess.AllowedEndpoints.Contains("api.neo.org"));
            Assert.IsTrue(policy.NetworkAccess.BlockedEndpoints.Contains("malicious.com"));
            Assert.IsTrue(policy.NetworkAccess.AllowedPorts.Contains(8080));
            Assert.IsTrue(policy.NetworkAccess.RequireSSL);
        }

        #endregion

        #region File System Policy Tests

        [TestMethod]
        [TestCategory("SecurityPolicy")]
        public void TestFileSystemAccessPolicy()
        {
            // Arrange
            var policy = new PluginSecurityPolicy();
            policy.FileSystemAccess.AllowedPaths.Add("/home/user/data");
            policy.FileSystemAccess.RestrictedPaths.Add("/etc");
            policy.FileSystemAccess.MaxFileSize = 10 * 1024 * 1024; // 10 MB
            policy.FileSystemAccess.MaxTotalFiles = 100;

            // Assert
            Assert.IsTrue(policy.FileSystemAccess.AllowedPaths.Contains("/home/user/data"));
            Assert.IsTrue(policy.FileSystemAccess.RestrictedPaths.Contains("/etc"));
            Assert.AreEqual(10 * 1024 * 1024, policy.FileSystemAccess.MaxFileSize);
            Assert.AreEqual(100, policy.FileSystemAccess.MaxTotalFiles);
        }

        #endregion

        #region Helper Methods

        private bool ValidatePermission(PluginSecurityPolicy policy, PluginPermissions permission)
        {
            return (permission & policy.MaxPermissions) == permission;
        }

        private bool ValidatePolicyConsistency(PluginSecurityPolicy policy)
        {
            // DefaultPermissions should not exceed MaxPermissions
            return (policy.DefaultPermissions & policy.MaxPermissions) == policy.DefaultPermissions;
        }

        #endregion
    }
}
