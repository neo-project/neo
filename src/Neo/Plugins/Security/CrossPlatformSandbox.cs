// Copyright (C) 2015-2025 The Neo Project.
//
// CrossPlatformSandbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Cross-platform sandbox implementation that adapts to the runtime environment.
    /// Provides consistent sandboxing capabilities across different operating systems.
    /// </summary>
    public class CrossPlatformSandbox : IPluginSandbox
    {
        private readonly IPluginSandbox _underlyingSandbox;
        private readonly PlatformInfo _platformInfo;
        private bool _disposed = false;

        /// <inheritdoc />
        public SandboxType Type => _underlyingSandbox.Type;

        /// <inheritdoc />
        public bool IsActive => _underlyingSandbox?.IsActive ?? false;

        /// <summary>
        /// Initializes a new instance of the CrossPlatformSandbox.
        /// </summary>
        public CrossPlatformSandbox()
        {
            _platformInfo = PlatformInfo.Current;
            _underlyingSandbox = CreatePlatformSpecificSandbox();
        }

        /// <inheritdoc />
        public async Task InitializeAsync(PluginSecurityPolicy policy)
        {
            // Adjust policy based on platform capabilities
            var adjustedPolicy = AdjustPolicyForPlatform(policy);
            await _underlyingSandbox.InitializeAsync(adjustedPolicy);
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<object> action)
        {
            return await _underlyingSandbox.ExecuteAsync(action);
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<Task<object>> action)
        {
            return await _underlyingSandbox.ExecuteAsync(action);
        }

        /// <inheritdoc />
        public bool ValidatePermission(PluginPermissions permission)
        {
            return _underlyingSandbox.ValidatePermission(permission);
        }

        /// <inheritdoc />
        public ResourceUsage GetResourceUsage()
        {
            return _underlyingSandbox.GetResourceUsage();
        }

        /// <inheritdoc />
        public void Suspend()
        {
            _underlyingSandbox.Suspend();
        }

        /// <inheritdoc />
        public void Resume()
        {
            _underlyingSandbox.Resume();
        }

        /// <inheritdoc />
        public void Terminate()
        {
            _underlyingSandbox.Terminate();
        }

        private IPluginSandbox CreatePlatformSpecificSandbox()
        {
            // Choose the best sandbox type based on platform capabilities
            if (_platformInfo.IsWindows && _platformInfo.DotNetVersion.Major >= 5)
            {
                // Windows with .NET 5+ - use AssemblyLoadContext
                return new AssemblyLoadContextSandbox();
            }
            else if (_platformInfo.IsLinux && _platformInfo.HasContainerSupport)
            {
                // Linux with container support - use software container
                return new ContainerSandbox();
            }
            else if (_platformInfo.DotNetVersion.Major >= 5)
            {
                // Any platform with .NET 5+ - use AssemblyLoadContext
                return new AssemblyLoadContextSandbox();
            }
            else
            {
                // Fallback to PassThrough for maximum compatibility
                return new PassThroughSandbox();
            }
        }

        private PluginSecurityPolicy AdjustPolicyForPlatform(PluginSecurityPolicy policy)
        {
            if (policy == null) return null;

            var adjustedPolicy = new PluginSecurityPolicy
            {
                DefaultPermissions = policy.DefaultPermissions,
                MaxPermissions = policy.MaxPermissions,
                MaxMemoryBytes = policy.MaxMemoryBytes,
                MaxCpuPercent = policy.MaxCpuPercent,
                MaxThreads = policy.MaxThreads,
                MaxExecutionTimeSeconds = policy.MaxExecutionTimeSeconds,
                ViolationAction = policy.ViolationAction,
                EnableDetailedMonitoring = policy.EnableDetailedMonitoring,
                StrictMode = policy.StrictMode,
                RequireSignedPlugins = policy.RequireSignedPlugins,
                SandboxType = policy.SandboxType,
                FileSystemAccess = AdjustFileSystemPolicy(policy.FileSystemAccess),
                NetworkAccess = AdjustNetworkPolicy(policy.NetworkAccess),
                ResourceMonitoring = AdjustResourceMonitoringPolicy(policy.ResourceMonitoring)
            };

            // Adjust sandbox type if the requested type is not supported
            if (!IsSandboxTypeSupported(policy.SandboxType))
            {
                adjustedPolicy.SandboxType = GetFallbackSandboxType();
            }

            return adjustedPolicy;
        }

        private FileSystemAccessPolicy AdjustFileSystemPolicy(FileSystemAccessPolicy policy)
        {
            if (policy == null) return null;

            var adjustedPolicy = new FileSystemAccessPolicy
            {
                AllowFileSystemAccess = policy.AllowFileSystemAccess,
                MaxFileSize = policy.MaxFileSize,
                MaxTotalFiles = policy.MaxTotalFiles
            };

            // Adjust paths for platform
            foreach (var path in policy.AllowedPaths)
            {
                adjustedPolicy.AllowedPaths.Add(NormalizePath(path));
            }

            foreach (var path in policy.RestrictedPaths)
            {
                adjustedPolicy.RestrictedPaths.Add(NormalizePath(path));
            }

            // Add platform-specific restricted paths
            if (_platformInfo.IsWindows)
            {
                adjustedPolicy.RestrictedPaths.Add(@"C:\Windows");
                adjustedPolicy.RestrictedPaths.Add(@"C:\Program Files");
            }
            else if (_platformInfo.IsLinux)
            {
                adjustedPolicy.RestrictedPaths.Add("/etc");
                adjustedPolicy.RestrictedPaths.Add("/boot");
                adjustedPolicy.RestrictedPaths.Add("/sys");
            }
            else if (_platformInfo.IsMacOS)
            {
                adjustedPolicy.RestrictedPaths.Add("/System");
                adjustedPolicy.RestrictedPaths.Add("/Library");
            }

            return adjustedPolicy;
        }

        private NetworkAccessPolicy AdjustNetworkPolicy(NetworkAccessPolicy policy)
        {
            if (policy == null) return null;

            var adjustedPolicy = new NetworkAccessPolicy
            {
                AllowNetworkAccess = policy.AllowNetworkAccess,
                MaxConnections = policy.MaxConnections,
                RequireSSL = policy.RequireSSL
            };

            adjustedPolicy.AllowedEndpoints.AddRange(policy.AllowedEndpoints);
            adjustedPolicy.BlockedEndpoints.AddRange(policy.BlockedEndpoints);
            adjustedPolicy.AllowedPorts.AddRange(policy.AllowedPorts);

            // Block platform-specific dangerous ports
            if (_platformInfo.IsWindows)
            {
                // Block Windows-specific dangerous ports
                adjustedPolicy.BlockedEndpoints.Add("localhost:445"); // SMB
                adjustedPolicy.BlockedEndpoints.Add("localhost:135"); // RPC
            }

            return adjustedPolicy;
        }

        private ResourceMonitoringPolicy AdjustResourceMonitoringPolicy(ResourceMonitoringPolicy policy)
        {
            if (policy == null) return null;

            var adjustedPolicy = new ResourceMonitoringPolicy
            {
                EnableMonitoring = policy.EnableMonitoring,
                CheckInterval = policy.CheckInterval,
                MemoryWarningThreshold = policy.MemoryWarningThreshold,
                CpuWarningThreshold = policy.CpuWarningThreshold
            };

            // Adjust monitoring intervals based on platform performance
            if (_platformInfo.IsContainerized)
            {
                // In containers, use slightly longer intervals to reduce overhead
                adjustedPolicy.CheckInterval = Math.Max(adjustedPolicy.CheckInterval, 2000);
            }

            return adjustedPolicy;
        }

        private bool IsSandboxTypeSupported(SandboxType sandboxType)
        {
            return sandboxType switch
            {
                SandboxType.PassThrough => true, // Always supported
                SandboxType.AssemblyLoadContext => _platformInfo.DotNetVersion.Major >= 5,
                SandboxType.Process => true, // Supported on all platforms
                SandboxType.Container => _platformInfo.HasContainerSupport,
                _ => false
            };
        }

        private SandboxType GetFallbackSandboxType()
        {
            if (_platformInfo.DotNetVersion.Major >= 5)
                return SandboxType.AssemblyLoadContext;
            else
                return SandboxType.PassThrough;
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path; // Return original if normalization fails
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _underlyingSandbox?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Provides information about the current platform and runtime environment.
    /// </summary>
    public class PlatformInfo
    {
        private static PlatformInfo _current;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the current platform information.
        /// </summary>
        public static PlatformInfo Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_lock)
                    {
                        if (_current == null)
                            _current = new PlatformInfo();
                    }
                }
                return _current;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current platform is Windows.
        /// </summary>
        public bool IsWindows { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current platform is Linux.
        /// </summary>
        public bool IsLinux { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current platform is macOS.
        /// </summary>
        public bool IsMacOS { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the runtime is .NET Framework.
        /// </summary>
        public bool IsFramework { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the runtime is .NET Core/.NET 5+.
        /// </summary>
        public bool IsCore { get; private set; }

        /// <summary>
        /// Gets the .NET version.
        /// </summary>
        public Version DotNetVersion { get; private set; }

        /// <summary>
        /// Gets a value indicating whether container support is available.
        /// </summary>
        public bool HasContainerSupport { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the application is running in a container.
        /// </summary>
        public bool IsContainerized { get; private set; }

        /// <summary>
        /// Gets the number of processor cores.
        /// </summary>
        public int ProcessorCount { get; private set; }

        /// <summary>
        /// Gets the total physical memory in bytes.
        /// </summary>
        public long TotalMemory { get; private set; }

        private PlatformInfo()
        {
            DetectOperatingSystem();
            DetectRuntime();
            DetectCapabilities();
            DetectHardware();
        }

        private void DetectOperatingSystem()
        {
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        private void DetectRuntime()
        {
            var runtimeName = RuntimeInformation.FrameworkDescription;
            IsFramework = runtimeName.Contains(".NET Framework");
            IsCore = !IsFramework;

            try
            {
                DotNetVersion = Environment.Version;
            }
            catch
            {
                DotNetVersion = new Version(1, 0); // Fallback
            }
        }

        private void DetectCapabilities()
        {
            // Check for container support
            HasContainerSupport = CheckContainerSupport();

            // Check if running in container
            IsContainerized = CheckIfContainerized();
        }

        private void DetectHardware()
        {
            ProcessorCount = Environment.ProcessorCount;

            try
            {
                // Estimate total memory (this is approximate)
                TotalMemory = GC.GetTotalMemory(false) * 10; // Very rough estimate
            }
            catch
            {
                TotalMemory = 1024 * 1024 * 1024; // 1GB fallback
            }
        }

        private bool CheckContainerSupport()
        {
            try
            {
                if (IsLinux)
                {
                    // Check for Docker or Podman
                    return File.Exists("/usr/bin/docker") ||
                           File.Exists("/usr/bin/podman") ||
                           Directory.Exists("/sys/fs/cgroup");
                }
                else if (IsWindows)
                {
                    // Check for Docker Desktop
                    return File.Exists(@"C:\Program Files\Docker\Docker\Docker Desktop.exe");
                }
                else if (IsMacOS)
                {
                    // Check for Docker Desktop on macOS
                    return Directory.Exists("/Applications/Docker.app");
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }

        private bool CheckIfContainerized()
        {
            try
            {
                if (IsLinux)
                {
                    // Check for container environment indicators
                    return File.Exists("/.dockerenv") ||
                           Environment.GetEnvironmentVariable("container") != null ||
                           File.Exists("/proc/1/cgroup") &&
                           File.ReadAllText("/proc/1/cgroup").Contains("docker");
                }
                else if (IsWindows)
                {
                    // Check Windows container indicators
                    return Environment.GetEnvironmentVariable("DOCKER_CONTAINER") != null;
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }

        /// <summary>
        /// Gets a string representation of the platform information.
        /// </summary>
        /// <returns>Platform information string.</returns>
        public override string ToString()
        {
            var osName = IsWindows ? "Windows" : IsLinux ? "Linux" : IsMacOS ? "macOS" : "Unknown";
            var runtime = IsFramework ? ".NET Framework" : ".NET Core/.NET";

            return $"{osName} {runtime} {DotNetVersion} " +
                   $"(Cores: {ProcessorCount}, Container: {(HasContainerSupport ? "Yes" : "No")})";
        }
    }

    /// <summary>
    /// Factory for creating platform-optimized sandboxes.
    /// </summary>
    public static class SandboxFactory
    {
        /// <summary>
        /// Creates the best sandbox for the current platform and policy.
        /// </summary>
        /// <param name="policy">The security policy.</param>
        /// <returns>An optimized sandbox instance.</returns>
        public static IPluginSandbox CreateOptimized(PluginSecurityPolicy policy)
        {
            var platform = PlatformInfo.Current;

            // Override sandbox type based on platform capabilities and security requirements
            if (policy.StrictMode && platform.HasContainerSupport)
            {
                // For strict mode with container support, prefer container sandbox
                return new ContainerSandbox();
            }
            else if (policy.MaxMemoryBytes > 0 && platform.DotNetVersion.Major >= 5)
            {
                // For memory-constrained scenarios with .NET 5+, use AssemblyLoadContext
                return new AssemblyLoadContextSandbox();
            }
            else if (policy.RequireSignedPlugins && platform.IsWindows)
            {
                // For signed plugin requirements on Windows, use process sandbox
                return new ProcessSandbox();
            }
            else
            {
                // For general compatibility, use cross-platform sandbox
                return new CrossPlatformSandbox();
            }
        }
    }
}
