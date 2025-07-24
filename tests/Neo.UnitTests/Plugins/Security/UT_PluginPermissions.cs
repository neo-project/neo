// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginPermissions.cs file belongs to the neo project and is free
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
using System.Threading.Tasks;

namespace Neo.UnitTests.Plugins.Security
{
    /// <summary>
    /// Comprehensive unit tests for the plugin permission system.
    /// Tests permission flags, combinations, validation, and enforcement.
    /// </summary>
    [TestClass]
    public class UT_PluginPermissions : SecurityTestBase
    {
        #region Permission Flag Tests

        [TestMethod]
        [TestCategory("Permissions")]
        public void TestIndividualPermissionFlags()
        {
            // Test each permission flag is a unique power of 2
            var permissions = new[]
            {
                PluginPermissions.ReadOnly,
                PluginPermissions.StorageAccess,
                PluginPermissions.NetworkAccess,
                PluginPermissions.FileSystemAccess,
                PluginPermissions.RpcPlugin,
                PluginPermissions.SystemAccess,
                PluginPermissions.CryptographicAccess,
                PluginPermissions.ProcessAccess,
                PluginPermissions.RegistryAccess,
                PluginPermissions.ServiceAccess,
                PluginPermissions.DatabaseAccess,
                PluginPermissions.ConsensusAccess,
                PluginPermissions.WalletAccess,
                PluginPermissions.OracleAccess,
                PluginPermissions.StateAccess,
                PluginPermissions.LogAccess,
                PluginPermissions.DebuggingAccess,
                PluginPermissions.HttpsOnly,
                PluginPermissions.AdminAccess,
                PluginPermissions.PluginLoaderAccess
            };

            // Verify each permission is a single bit
            foreach (var permission in permissions)
            {
                var value = (uint)permission;
                Assert.IsTrue(IsPowerOfTwo(value), $"{permission} should be a power of 2");
            }

            // Verify no duplicate values
            var uniqueValues = permissions.Select(p => (uint)p).Distinct().Count();
            Assert.AreEqual(permissions.Length, uniqueValues);
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public void TestPermissionCombinations()
        {
            // Test predefined permission combinations
            var networkPlugin = PluginPermissions.NetworkPlugin;
            var servicePlugin = PluginPermissions.ServicePlugin;
            var adminPlugin = PluginPermissions.AdminPlugin;

            // NetworkPlugin = ReadOnly | NetworkAccess
            Assert.IsTrue((networkPlugin & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((networkPlugin & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
            Assert.IsFalse((networkPlugin & PluginPermissions.StorageAccess) == PluginPermissions.StorageAccess);

            // ServicePlugin = ReadOnly | StorageAccess | NetworkAccess
            Assert.IsTrue((servicePlugin & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((servicePlugin & PluginPermissions.StorageAccess) == PluginPermissions.StorageAccess);
            Assert.IsTrue((servicePlugin & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);

            // AdminPlugin includes multiple permissions but not all
            Assert.IsTrue((adminPlugin & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);
            Assert.IsTrue((adminPlugin & PluginPermissions.FileSystemAccess) == PluginPermissions.FileSystemAccess);
            Assert.IsTrue((adminPlugin & PluginPermissions.RpcPlugin) == PluginPermissions.RpcPlugin);
            Assert.IsFalse((adminPlugin & PluginPermissions.ProcessAccess) == PluginPermissions.ProcessAccess);
            Assert.IsFalse((adminPlugin & PluginPermissions.FullAccess) == PluginPermissions.FullAccess);
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public void TestPermissionBitOperations()
        {
            var permissions = PluginPermissions.None;

            // Test adding permissions
            permissions = permissions | PluginPermissions.ReadOnly;
            Assert.IsTrue((permissions & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);

            permissions = permissions | PluginPermissions.NetworkAccess;
            Assert.IsTrue((permissions & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
            Assert.IsTrue((permissions & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);

            // Test removing permissions
            permissions = permissions & ~PluginPermissions.NetworkAccess;
            Assert.IsFalse((permissions & PluginPermissions.NetworkAccess) == PluginPermissions.NetworkAccess);
            Assert.IsTrue((permissions & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);

            // Test toggling permissions
            permissions = permissions ^ PluginPermissions.FileSystemAccess;
            Assert.IsTrue((permissions & PluginPermissions.FileSystemAccess) == PluginPermissions.FileSystemAccess);

            permissions = permissions ^ PluginPermissions.FileSystemAccess;
            Assert.IsFalse((permissions & PluginPermissions.FileSystemAccess) == PluginPermissions.FileSystemAccess);
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public void TestSpecialPermissionValues()
        {
            // Test None permission
            var none = PluginPermissions.None;
            Assert.AreEqual(0u, (uint)none);
            Assert.IsFalse((none & PluginPermissions.ReadOnly) == PluginPermissions.ReadOnly);

            // Test FullAccess permission
            var full = PluginPermissions.FullAccess;
            Assert.AreEqual(0xFFFFFFFF, (uint)full);

            // FullAccess should include all individual permissions
            var allPermissions = Enum.GetValues(typeof(PluginPermissions))
                .Cast<PluginPermissions>()
                .Where(p => p != PluginPermissions.None && p != PluginPermissions.FullAccess);

            foreach (var permission in allPermissions)
            {
                Assert.IsTrue((full & permission) == permission,
                    $"FullAccess should include {permission}");
            }
        }

        #endregion

        #region Permission Validation Tests

        [TestMethod]
        [TestCategory("Permissions")]
        public async Task TestSandboxPermissionValidation()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            policy.DefaultPermissions = PluginPermissions.ReadOnly;
            policy.MaxPermissions = PluginPermissions.NetworkPlugin;

            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Assert - Should have ReadOnly
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));

            // Assert - Should have NetworkAccess (within MaxPermissions)
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.NetworkAccess));

            // Assert - Should NOT have permissions beyond MaxPermissions
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FileSystemAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.ProcessAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.AdminAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FullAccess));
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public async Task TestRestrictivePermissionValidation()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateRestrictive();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Assert - Restrictive policy only allows ReadOnly
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.NetworkAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.StorageAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FileSystemAccess));
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public async Task TestPermissivePermissionValidation()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreatePermissive();
            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Assert - Permissive policy allows many permissions
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.NetworkAccess));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.StorageAccess));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.FileSystemAccess));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.DatabaseAccess));

            // But not the most dangerous ones
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.ProcessAccess));
            Assert.IsFalse(sandbox.ValidatePermission(PluginPermissions.FullAccess));
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public async Task TestPassThroughSandboxPermissions()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateRestrictive();
            using var sandbox = new PassThroughSandbox();
            await sandbox.InitializeAsync(policy);

            // Assert - PassThrough sandbox allows all permissions regardless of policy
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ReadOnly));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.NetworkAccess));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.ProcessAccess));
            Assert.IsTrue(sandbox.ValidatePermission(PluginPermissions.FullAccess));
        }

        #endregion

        #region Permission Enforcement Tests

        [TestMethod]
        [TestCategory("Permissions")]
        public async Task TestPermissionEnforcementInExecution()
        {
            // Arrange
            var policy = PluginSecurityPolicy.CreateDefault();
            policy.DefaultPermissions = PluginPermissions.ReadOnly;
            policy.MaxPermissions = PluginPermissions.ReadOnly;

            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            // Act - Try to execute code that checks permissions
            var result = await sandbox.ExecuteAsync(() =>
            {
                // Check if we have read permission
                if (!sandbox.ValidatePermission(PluginPermissions.ReadOnly))
                    throw new UnauthorizedAccessException("ReadOnly permission required");

                // Check if we have network permission (should fail)
                if (!sandbox.ValidatePermission(PluginPermissions.NetworkAccess))
                    throw new UnauthorizedAccessException("NetworkAccess permission required");

                return "Should not reach here";
            });

            // Assert
            Assert.IsFalse(result.Success);
            Assert.IsInstanceOfType(result.Exception, typeof(UnauthorizedAccessException));
            Assert.IsTrue(result.Exception.Message.Contains("NetworkAccess"));
        }

        [TestMethod]
        [TestCategory("Permissions")]
        public async Task TestMultiplePermissionChecks()
        {
            // Arrange
            var policy = new PluginSecurityPolicy
            {
                DefaultPermissions = PluginPermissions.ServicePlugin,
                MaxPermissions = PluginPermissions.ServicePlugin
            };

            using var sandbox = new AssemblyLoadContextSandbox();
            await sandbox.InitializeAsync(policy);

            var permissionChecks = new Dictionary<PluginPermissions, bool>();

            // Act
            var result = await sandbox.ExecuteAsync(() =>
            {
                permissionChecks[PluginPermissions.ReadOnly] =
                    sandbox.ValidatePermission(PluginPermissions.ReadOnly);
                permissionChecks[PluginPermissions.StorageAccess] =
                    sandbox.ValidatePermission(PluginPermissions.StorageAccess);
                permissionChecks[PluginPermissions.NetworkAccess] =
                    sandbox.ValidatePermission(PluginPermissions.NetworkAccess);
                permissionChecks[PluginPermissions.FileSystemAccess] =
                    sandbox.ValidatePermission(PluginPermissions.FileSystemAccess);
                permissionChecks[PluginPermissions.ProcessAccess] =
                    sandbox.ValidatePermission(PluginPermissions.ProcessAccess);

                return permissionChecks;
            });

            // Assert
            Assert.IsTrue(result.Success);
            var checks = result.Result as Dictionary<PluginPermissions, bool>;

            Assert.IsTrue(checks[PluginPermissions.ReadOnly]);
            Assert.IsTrue(checks[PluginPermissions.StorageAccess]);
            Assert.IsTrue(checks[PluginPermissions.NetworkAccess]);
            Assert.IsFalse(checks[PluginPermissions.FileSystemAccess]);
            Assert.IsFalse(checks[PluginPermissions.ProcessAccess]);
        }

        #endregion

        #region Permission String Representation Tests

        [TestMethod]
        [TestCategory("Permissions")]
        public void TestPermissionStringRepresentation()
        {
            // Test individual permissions
            Assert.AreEqual("None", PluginPermissions.None.ToString());
            Assert.AreEqual("ReadOnly", PluginPermissions.ReadOnly.ToString());
            Assert.AreEqual("NetworkAccess", PluginPermissions.NetworkAccess.ToString());
            Assert.AreEqual("FullAccess", PluginPermissions.FullAccess.ToString());

            // Test combined permissions
            var combined = PluginPermissions.ReadOnly | PluginPermissions.NetworkAccess;
            Assert.AreEqual("NetworkPlugin", combined.ToString());

            // Test custom combinations
            var custom = PluginPermissions.ReadOnly | PluginPermissions.FileSystemAccess;
            var str = custom.ToString();
            Assert.IsTrue(str.Contains("ReadOnly") || str.Contains("FileSystemAccess"));
        }

        #endregion

        #region Helper Methods

        private bool IsPowerOfTwo(uint value)
        {
            return value != 0 && (value & (value - 1)) == 0;
        }

        #endregion
    }
}
