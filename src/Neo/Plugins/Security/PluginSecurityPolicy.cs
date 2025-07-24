// Copyright (C) 2015-2025 The Neo Project.
//
// PluginSecurityPolicy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Defines the security policy and resource limits for plugins.
    /// </summary>
    public class PluginSecurityPolicy
    {
        /// <summary>
        /// Gets or sets the default permissions granted to plugins.
        /// </summary>
        public PluginPermissions DefaultPermissions { get; set; } = PluginPermissions.ReadOnly;

        /// <summary>
        /// Gets or sets the maximum permissions that can be granted.
        /// </summary>
        public PluginPermissions MaxPermissions { get; set; } = PluginPermissions.NetworkPlugin;

        /// <summary>
        /// Gets or sets the maximum memory usage in bytes.
        /// </summary>
        public long MaxMemoryBytes { get; set; } = SecurityConstants.Memory.DefaultMaxMemoryBytes;

        /// <summary>
        /// Gets or sets the maximum CPU usage percentage.
        /// </summary>
        public double MaxCpuPercent { get; set; } = SecurityConstants.Performance.DefaultMaxCpuPercent;

        /// <summary>
        /// Gets or sets the maximum number of threads.
        /// </summary>
        public int MaxThreads { get; set; } = SecurityConstants.Threading.DefaultMaxThreads;

        /// <summary>
        /// Gets or sets the maximum execution time in seconds.
        /// </summary>
        public int MaxExecutionTimeSeconds { get; set; } = SecurityConstants.Timeouts.DefaultExecutionTimeoutSeconds;

        /// <summary>
        /// Gets or sets the action to take when a violation occurs.
        /// </summary>
        public ViolationAction ViolationAction { get; set; } = ViolationAction.Suspend;

        /// <summary>
        /// Gets or sets whether to enable detailed monitoring.
        /// </summary>
        public bool EnableDetailedMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets whether strict mode is enabled.
        /// </summary>
        public bool StrictMode { get; set; } = true;

        /// <summary>
        /// Gets or sets whether signed plugins are required.
        /// </summary>
        public bool RequireSignedPlugins { get; set; } = true;

        /// <summary>
        /// Gets or sets the sandbox type to use.
        /// </summary>
        public SandboxType SandboxType { get; set; } = SandboxType.AssemblyLoadContext;

        /// <summary>
        /// Gets or sets the file system access configuration.
        /// </summary>
        public FileSystemAccessPolicy FileSystemAccess { get; set; } = new FileSystemAccessPolicy();

        /// <summary>
        /// Gets or sets the network access configuration.
        /// </summary>
        public NetworkAccessPolicy NetworkAccess { get; set; } = new NetworkAccessPolicy();

        /// <summary>
        /// Gets or sets the resource monitoring configuration.
        /// </summary>
        public ResourceMonitoringPolicy ResourceMonitoring { get; set; } = new ResourceMonitoringPolicy();

        /// <summary>
        /// Creates a default security policy.
        /// </summary>
        /// <returns>A new default security policy.</returns>
        public static PluginSecurityPolicy CreateDefault()
        {
            return new PluginSecurityPolicy();
        }

        /// <summary>
        /// Creates a restrictive security policy for untrusted plugins.
        /// </summary>
        /// <returns>A new restrictive security policy.</returns>
        public static PluginSecurityPolicy CreateRestrictive()
        {
            return new PluginSecurityPolicy
            {
                DefaultPermissions = PluginPermissions.ReadOnly,
                MaxPermissions = PluginPermissions.ReadOnly,
                MaxMemoryBytes = 64 * 1024 * 1024, // 64 MB
                MaxCpuPercent = 10.0,
                MaxThreads = 5,
                MaxExecutionTimeSeconds = 60,
                ViolationAction = ViolationAction.Terminate,
                StrictMode = true,
                RequireSignedPlugins = true,
                SandboxType = SandboxType.Process
            };
        }

        /// <summary>
        /// Creates a permissive security policy for trusted plugins.
        /// </summary>
        /// <returns>A new permissive security policy.</returns>
        public static PluginSecurityPolicy CreatePermissive()
        {
            return new PluginSecurityPolicy
            {
                DefaultPermissions = PluginPermissions.ServicePlugin,
                MaxPermissions = PluginPermissions.AdminPlugin,
                MaxMemoryBytes = 1024 * 1024 * 1024, // 1 GB
                MaxCpuPercent = 50.0,
                MaxThreads = 20,
                MaxExecutionTimeSeconds = 600,
                ViolationAction = ViolationAction.Log,
                StrictMode = false,
                RequireSignedPlugins = false,
                SandboxType = SandboxType.AssemblyLoadContext
            };
        }
    }

    /// <summary>
    /// Defines file system access policies.
    /// </summary>
    public class FileSystemAccessPolicy
    {
        /// <summary>
        /// Gets or sets whether file system access is allowed.
        /// </summary>
        public bool AllowFileSystemAccess { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of allowed file paths.
        /// </summary>
        public List<string> AllowedPaths { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of restricted file paths.
        /// </summary>
        public List<string> RestrictedPaths { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the maximum file size in bytes.
        /// </summary>
        public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100 MB

        /// <summary>
        /// Gets or sets the maximum number of files.
        /// </summary>
        public int MaxTotalFiles { get; set; } = 1000;
    }

    /// <summary>
    /// Defines network access policies.
    /// </summary>
    public class NetworkAccessPolicy
    {
        /// <summary>
        /// Gets or sets whether network access is allowed.
        /// </summary>
        public bool AllowNetworkAccess { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of allowed endpoints.
        /// </summary>
        public List<string> AllowedEndpoints { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of blocked endpoints.
        /// </summary>
        public List<string> BlockedEndpoints { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of allowed ports.
        /// </summary>
        public List<int> AllowedPorts { get; set; } = new List<int> { 80, 443, 10332, 10333, 20332, 20333 };

        /// <summary>
        /// Gets or sets the maximum number of connections.
        /// </summary>
        public int MaxConnections { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether SSL is required.
        /// </summary>
        public bool RequireSSL { get; set; } = true;
    }

    /// <summary>
    /// Defines resource monitoring policies.
    /// </summary>
    public class ResourceMonitoringPolicy
    {
        /// <summary>
        /// Gets or sets whether resource monitoring is enabled.
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets the monitoring check interval in milliseconds.
        /// </summary>
        public int CheckInterval { get; set; }

        public ResourceMonitoringPolicy()
        {
            // Use faster check interval in test environments
            CheckInterval = IsTestEnvironment() ? 100 : 5000;
        }

        private static bool IsTestEnvironment()
        {
            try
            {
                var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                return processName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                       processName.Contains("vstest", StringComparison.OrdinalIgnoreCase) ||
                       Environment.GetEnvironmentVariable("DOTNET_TEST_MODE")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets the memory warning threshold (0.0 to 1.0).
        /// </summary>
        public double MemoryWarningThreshold { get; set; } = 0.8;

        /// <summary>
        /// Gets or sets the CPU warning threshold (0.0 to 1.0).
        /// </summary>
        public double CpuWarningThreshold { get; set; } = 0.7;
    }

    /// <summary>
    /// Defines the actions to take when a security violation occurs.
    /// </summary>
    public enum ViolationAction
    {
        /// <summary>
        /// Log the violation but continue execution.
        /// </summary>
        Log,

        /// <summary>
        /// Suspend the plugin temporarily.
        /// </summary>
        Suspend,

        /// <summary>
        /// Terminate the plugin immediately.
        /// </summary>
        Terminate
    }
}
