# Neo Plugin Sandbox API Reference

## Table of Contents

- [Overview](#overview)
- [Core Interfaces](#core-interfaces)
- [Security Classes](#security-classes)
- [Sandbox Implementations](#sandbox-implementations)
- [Resource Management](#resource-management)
- [Monitoring and Auditing](#monitoring-and-auditing)
- [Utility Classes](#utility-classes)
- [Examples](#examples)

## Overview

The Neo Plugin Sandbox API provides a comprehensive set of interfaces and classes for implementing secure plugin isolation. This reference documents all public APIs, their methods, properties, and usage patterns.

### API Hierarchy

```
Neo.Plugins.Security
├── Interfaces/
│   ├── IPluginSandbox
│   ├── IPermissionCacheManager
│   └── IThreadSafeStateManager
├── Security Policies/
│   ├── PluginSecurityPolicy
│   ├── PluginPermissions (enum)
│   └── ViolationAction (enum)
├── Sandbox Implementations/
│   ├── PassThroughSandbox
│   ├── AssemblyLoadContextSandbox
│   ├── ProcessSandbox
│   └── ContainerSandbox
├── Resource Management/
│   ├── PluginResourceMonitor
│   ├── EventDrivenResourceMonitor
│   └── ResourceUsage
└── Utility Classes/
    ├── SecurityConstants
    ├── SecurityAuditLogger
    └── ServiceLocator
```

## Core Interfaces

### IPluginSandbox

The primary interface for plugin sandbox implementations.

```csharp
namespace Neo.Plugins.Security
{
    /// <summary>
    /// Defines the interface for plugin sandboxes that provide security isolation.
    /// </summary>
    public interface IPluginSandbox : IDisposable
    {
        /// <summary>
        /// Gets the type of sandbox implementation.
        /// </summary>
        SandboxType Type { get; }

        /// <summary>
        /// Gets a value indicating whether the sandbox is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Initializes the sandbox with the specified security policy.
        /// </summary>
        /// <param name="policy">The security policy to apply.</param>
        /// <returns>A task representing the initialization operation.</returns>
        Task InitializeAsync(PluginSecurityPolicy policy);

        /// <summary>
        /// Executes an action within the sandbox.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>A task representing the sandboxed execution.</returns>
        Task<SandboxResult> ExecuteAsync(Func<object> action);

        /// <summary>
        /// Executes an asynchronous action within the sandbox.
        /// </summary>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <returns>A task representing the sandboxed execution.</returns>
        Task<SandboxResult> ExecuteAsync(Func<Task<object>> action);

        /// <summary>
        /// Validates if a specific permission is allowed.
        /// </summary>
        /// <param name="permission">The permission to validate.</param>
        /// <returns>True if the permission is allowed; otherwise, false.</returns>
        bool ValidatePermission(PluginPermissions permission);

        /// <summary>
        /// Gets the current resource usage of the sandbox.
        /// </summary>
        /// <returns>The current resource usage statistics.</returns>
        ResourceUsage GetResourceUsage();

        /// <summary>
        /// Suspends the sandbox execution.
        /// </summary>
        void Suspend();

        /// <summary>
        /// Resumes the sandbox execution.
        /// </summary>
        void Resume();

        /// <summary>
        /// Terminates the sandbox forcefully.
        /// </summary>
        void Terminate();
    }
}
```

**Usage Example**:
```csharp
// Create and initialize a sandbox
IPluginSandbox sandbox = new AssemblyLoadContextSandbox();
var policy = PluginSecurityPolicy.CreateDefault();
await sandbox.InitializeAsync(policy);

// Execute code within the sandbox
var result = await sandbox.ExecuteAsync(() => {
    // Plugin code here
    return "Hello from sandbox!";
});

if (result.Success)
{
    Console.WriteLine($"Result: {result.Result}");
}
```

### IPermissionCacheManager

Interface for managing permission validation caching.

```csharp
namespace Neo.Plugins.Security.Interfaces
{
    /// <summary>
    /// Provides caching functionality for permission validation results.
    /// </summary>
    public interface IPermissionCacheManager
    {
        /// <summary>
        /// Tries to get a cached permission result.
        /// </summary>
        /// <param name="permission">The permission to check.</param>
        /// <param name="result">The cached result if found.</param>
        /// <returns>True if a cached result was found; otherwise, false.</returns>
        bool TryGetCachedResult(PluginPermissions permission, out bool result);

        /// <summary>
        /// Caches a permission validation result.
        /// </summary>
        /// <param name="permission">The permission that was validated.</param>
        /// <param name="result">The validation result.</param>
        void CacheResult(PluginPermissions permission, bool result);

        /// <summary>
        /// Invalidates cached results for a specific permission.
        /// </summary>
        /// <param name="permission">The permission to invalidate.</param>
        void InvalidatePermission(PluginPermissions permission);

        /// <summary>
        /// Clears all cached permission results.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        /// <returns>Cache performance statistics.</returns>
        CacheStatistics GetStatistics();
    }
}
```

### IThreadSafeStateManager

Interface for thread-safe state management in plugin sandboxes.

```csharp
namespace Neo.Plugins.Security.Interfaces
{
    /// <summary>
    /// Provides thread-safe state management for plugin sandboxes.
    /// </summary>
    public interface IThreadSafeStateManager
    {
        /// <summary>
        /// Sets a state value with exclusive access.
        /// </summary>
        /// <typeparam name="T">The type of the state value.</typeparam>
        /// <param name="key">The state key.</param>
        /// <param name="value">The state value.</param>
        /// <param name="timeout">Optional timeout for acquiring exclusive access.</param>
        /// <returns>True if the state was set successfully; otherwise, false.</returns>
        Task<bool> SetStateAsync<T>(string key, T value, TimeSpan? timeout = null);

        /// <summary>
        /// Gets a state value with shared access.
        /// </summary>
        /// <typeparam name="T">The type of the state value.</typeparam>
        /// <param name="key">The state key.</param>
        /// <param name="timeout">Optional timeout for acquiring shared access.</param>
        /// <returns>The state value if found; otherwise, default(T).</returns>
        Task<T> GetStateAsync<T>(string key, TimeSpan? timeout = null);

        /// <summary>
        /// Removes a state value with exclusive access.
        /// </summary>
        /// <param name="key">The state key to remove.</param>
        /// <param name="timeout">Optional timeout for acquiring exclusive access.</param>
        /// <returns>True if the state was removed; otherwise, false.</returns>
        Task<bool> RemoveStateAsync(string key, TimeSpan? timeout = null);

        /// <summary>
        /// Checks if a state key exists.
        /// </summary>
        /// <param name="key">The state key to check.</param>
        /// <returns>True if the key exists; otherwise, false.</returns>
        bool ContainsKey(string key);

        /// <summary>
        /// Gets all state keys.
        /// </summary>
        /// <returns>Collection of all state keys.</returns>
        IEnumerable<string> GetKeys();
    }
}
```

## Security Classes

### PluginSecurityPolicy

Defines security constraints and permissions for plugins.

```csharp
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
                MaxMemoryBytes = SecurityConstants.Memory.RestrictiveMaxMemoryBytes,
                MaxCpuPercent = SecurityConstants.Performance.RestrictiveMaxCpuPercent,
                MaxThreads = SecurityConstants.Threading.RestrictiveMaxThreads,
                MaxExecutionTimeSeconds = SecurityConstants.Timeouts.RestrictiveExecutionTimeoutSeconds,
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
                MaxMemoryBytes = SecurityConstants.Memory.PermissiveMaxMemoryBytes,
                MaxCpuPercent = SecurityConstants.Performance.PermissiveMaxCpuPercent,
                MaxThreads = SecurityConstants.Threading.PermissiveMaxThreads,
                MaxExecutionTimeSeconds = SecurityConstants.Timeouts.PermissiveExecutionTimeoutSeconds,
                ViolationAction = ViolationAction.Log,
                StrictMode = false,
                RequireSignedPlugins = false,
                SandboxType = SandboxType.AssemblyLoadContext
            };
        }

        /// <summary>
        /// Validates the policy configuration.
        /// </summary>
        /// <returns>True if the policy is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            return MaxMemoryBytes > 0 &&
                   MaxCpuPercent > 0 && MaxCpuPercent <= 100 &&
                   MaxThreads > 0 &&
                   MaxExecutionTimeSeconds > 0;
        }

        /// <summary>
        /// Creates a copy of this policy.
        /// </summary>
        /// <returns>A deep copy of the policy.</returns>
        public PluginSecurityPolicy Clone()
        {
            return new PluginSecurityPolicy
            {
                DefaultPermissions = this.DefaultPermissions,
                MaxPermissions = this.MaxPermissions,
                MaxMemoryBytes = this.MaxMemoryBytes,
                MaxCpuPercent = this.MaxCpuPercent,
                MaxThreads = this.MaxThreads,
                MaxExecutionTimeSeconds = this.MaxExecutionTimeSeconds,
                ViolationAction = this.ViolationAction,
                EnableDetailedMonitoring = this.EnableDetailedMonitoring,
                StrictMode = this.StrictMode,
                RequireSignedPlugins = this.RequireSignedPlugins,
                SandboxType = this.SandboxType,
                FileSystemAccess = this.FileSystemAccess.Clone(),
                NetworkAccess = this.NetworkAccess.Clone(),
                ResourceMonitoring = this.ResourceMonitoring.Clone()
            };
        }
    }
}
```

### PluginPermissions

Enumeration of available plugin permissions.

```csharp
namespace Neo.Plugins.Security
{
    /// <summary>
    /// Defines the permissions that can be granted to plugins.
    /// </summary>
    [Flags]
    public enum PluginPermissions : uint
    {
        /// <summary>
        /// No permissions granted.
        /// </summary>
        None = 0,

        /// <summary>
        /// Basic read-only access to blockchain data.
        /// </summary>
        ReadOnly = 1,

        /// <summary>
        /// Access to write blockchain data and submit transactions.
        /// </summary>
        StorageAccess = 2,

        /// <summary>
        /// Network communication capabilities.
        /// </summary>
        NetworkAccess = 4,

        /// <summary>
        /// File system access within allowed paths.
        /// </summary>
        FileSystemAccess = 8,

        /// <summary>
        /// RPC server functionality.
        /// </summary>
        RpcPlugin = 16,

        /// <summary>
        /// Advanced system operations.
        /// </summary>
        SystemAccess = 32,

        /// <summary>
        /// Cryptographic operations.
        /// </summary>
        CryptographicAccess = 64,

        /// <summary>
        /// Process creation and management.
        /// </summary>
        ProcessAccess = 128,

        /// <summary>
        /// Registry access (Windows only).
        /// </summary>
        RegistryAccess = 256,

        /// <summary>
        /// Service management access.
        /// </summary>
        ServiceAccess = 512,

        /// <summary>
        /// Database access.
        /// </summary>
        DatabaseAccess = 1024,

        /// <summary>
        /// Consensus participation.
        /// </summary>
        ConsensusAccess = 2048,

        /// <summary>
        /// Wallet operations.
        /// </summary>
        WalletAccess = 4096,

        /// <summary>
        /// Oracle service access.
        /// </summary>
        OracleAccess = 8192,

        /// <summary>
        /// State service access.
        /// </summary>
        StateAccess = 16384,

        /// <summary>
        /// Application logs access.
        /// </summary>
        LogAccess = 32768,

        /// <summary>
        /// Memory debugging and profiling.
        /// </summary>
        DebuggingAccess = 65536,

        /// <summary>
        /// HTTP/HTTPS network access only.
        /// </summary>
        HttpsOnly = 131072,

        /// <summary>
        /// Administrative operations.
        /// </summary>
        AdminAccess = 262144,

        /// <summary>
        /// Plugin can load other plugins.
        /// </summary>
        PluginLoaderAccess = 524288,

        // Predefined permission sets
        /// <summary>
        /// Standard plugin permissions (ReadOnly + NetworkAccess).
        /// </summary>
        NetworkPlugin = ReadOnly | NetworkAccess,

        /// <summary>
        /// Service plugin permissions (ReadOnly + StorageAccess + NetworkAccess).
        /// </summary>
        ServicePlugin = ReadOnly | StorageAccess | NetworkAccess,

        /// <summary>
        /// Administrative plugin permissions (most permissions except dangerous ones).
        /// </summary>
        AdminPlugin = ReadOnly | StorageAccess | NetworkAccess | FileSystemAccess |
                     RpcPlugin | CryptographicAccess | DatabaseAccess | LogAccess,

        /// <summary>
        /// Full system access (all permissions - use with extreme caution).
        /// </summary>
        FullAccess = 0xFFFFFFFF
    }

    /// <summary>
    /// Extension methods for PluginPermissions enum.
    /// </summary>
    public static class PluginPermissionsExtensions
    {
        /// <summary>
        /// Checks if the permissions include the specified permission.
        /// </summary>
        /// <param name="permissions">The permissions to check.</param>
        /// <param name="permission">The permission to check for.</param>
        /// <returns>True if the permission is included; otherwise, false.</returns>
        public static bool HasPermission(this PluginPermissions permissions, PluginPermissions permission)
        {
            return (permissions & permission) == permission;
        }

        /// <summary>
        /// Adds a permission to the existing permissions.
        /// </summary>
        /// <param name="permissions">The existing permissions.</param>
        /// <param name="permission">The permission to add.</param>
        /// <returns>The combined permissions.</returns>
        public static PluginPermissions AddPermission(this PluginPermissions permissions, PluginPermissions permission)
        {
            return permissions | permission;
        }

        /// <summary>
        /// Removes a permission from the existing permissions.
        /// </summary>
        /// <param name="permissions">The existing permissions.</param>
        /// <param name="permission">The permission to remove.</param>
        /// <returns>The permissions with the specified permission removed.</returns>
        public static PluginPermissions RemovePermission(this PluginPermissions permissions, PluginPermissions permission)
        {
            return permissions & ~permission;
        }

        /// <summary>
        /// Gets a human-readable description of the permissions.
        /// </summary>
        /// <param name="permissions">The permissions to describe.</param>
        /// <returns>A string description of the permissions.</returns>
        public static string GetDescription(this PluginPermissions permissions)
        {
            if (permissions == PluginPermissions.None)
                return "No permissions";

            if (permissions == PluginPermissions.FullAccess)
                return "Full system access";

            var parts = new List<string>();
            
            if (permissions.HasPermission(PluginPermissions.ReadOnly))
                parts.Add("Blockchain read access");
            if (permissions.HasPermission(PluginPermissions.StorageAccess))
                parts.Add("Storage access");
            if (permissions.HasPermission(PluginPermissions.NetworkAccess))
                parts.Add("Network access");
            if (permissions.HasPermission(PluginPermissions.FileSystemAccess))
                parts.Add("File system access");
            if (permissions.HasPermission(PluginPermissions.AdminAccess))
                parts.Add("Administrative access");

            return string.Join(", ", parts);
        }
    }
}
```

### SandboxResult

Represents the result of a sandboxed operation.

```csharp
namespace Neo.Plugins.Security
{
    /// <summary>
    /// Represents the result of a sandboxed operation.
    /// </summary>
    public class SandboxResult
    {
        /// <summary>
        /// Indicates if the operation completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result value if the operation succeeded.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// The exception if the operation failed.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Resource usage statistics for the operation.
        /// </summary>
        public ResourceUsage ResourceUsage { get; set; }

        /// <summary>
        /// The execution time for the operation.
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Additional metadata about the execution.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <param name="result">The operation result.</param>
        /// <param name="resourceUsage">Resource usage statistics.</param>
        /// <returns>A successful sandbox result.</returns>
        public static SandboxResult CreateSuccess(object result, ResourceUsage resourceUsage = null)
        {
            return new SandboxResult
            {
                Success = true,
                Result = result,
                ResourceUsage = resourceUsage ?? new ResourceUsage()
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="resourceUsage">Resource usage statistics.</param>
        /// <returns>A failed sandbox result.</returns>
        public static SandboxResult CreateFailure(Exception exception, ResourceUsage resourceUsage = null)
        {
            return new SandboxResult
            {
                Success = false,
                Exception = exception,
                ResourceUsage = resourceUsage ?? new ResourceUsage()
            };
        }

        /// <summary>
        /// Gets the result as a specific type.
        /// </summary>
        /// <typeparam name="T">The type to cast the result to.</typeparam>
        /// <returns>The result cast to the specified type.</returns>
        public T GetResult<T>()
        {
            if (!Success)
                throw new InvalidOperationException("Cannot get result from failed operation");

            if (Result is T typedResult)
                return typedResult;

            throw new InvalidCastException($"Cannot cast result of type {Result?.GetType()} to {typeof(T)}");
        }
    }
}
```

## Sandbox Implementations

### PassThroughSandbox

No-isolation sandbox for development and testing.

```csharp
namespace Neo.Plugins.Security
{
    /// <summary>
    /// A sandbox implementation that provides no isolation (pass-through mode).
    /// </summary>
    public class PassThroughSandbox : IPluginSandbox
    {
        private PluginSecurityPolicy _policy;
        private bool _disposed;

        /// <inheritdoc />
        public SandboxType Type => SandboxType.PassThrough;

        /// <inheritdoc />
        public bool IsActive => !_disposed;

        /// <inheritdoc />
        public async Task InitializeAsync(PluginSecurityPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<object> action)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PassThroughSandbox));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var result = await Task.Run(action);
                stopwatch.Stop();

                return SandboxResult.CreateSuccess(result, new ResourceUsage
                {
                    ExecutionTime = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                return SandboxResult.CreateFailure(ex, new ResourceUsage
                {
                    ExecutionTime = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<Task<object>> action)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PassThroughSandbox));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var result = await action();
                stopwatch.Stop();

                return SandboxResult.CreateSuccess(result, new ResourceUsage
                {
                    ExecutionTime = stopwatch.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                return SandboxResult.CreateFailure(ex, new ResourceUsage
                {
                    ExecutionTime = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <inheritdoc />
        public bool ValidatePermission(PluginPermissions permission)
        {
            // Pass-through sandbox allows all permissions
            return true;
        }

        /// <inheritdoc />
        public ResourceUsage GetResourceUsage()
        {
            // Return basic resource usage for pass-through mode
            return new ResourceUsage
            {
                MemoryUsed = GC.GetTotalMemory(false),
                ThreadsCreated = Process.GetCurrentProcess().Threads.Count
            };
        }

        /// <inheritdoc />
        public void Suspend()
        {
            // No-op for pass-through sandbox
        }

        /// <inheritdoc />
        public void Resume()
        {
            // No-op for pass-through sandbox
        }

        /// <inheritdoc />
        public void Terminate()
        {
            Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
```

### AssemblyLoadContextSandbox

Assembly-level isolation using .NET's AssemblyLoadContext.

```csharp
namespace Neo.Plugins.Security
{
    /// <summary>
    /// A sandbox implementation using AssemblyLoadContext for application domain isolation.
    /// </summary>
    public class AssemblyLoadContextSandbox : IPluginSandbox
    {
        private PluginAssemblyLoadContext _loadContext;
        private PluginSecurityPolicy _policy;
        private readonly PermissionCacheManager _permissionCache;
        private bool _disposed;
        private bool _suspended;

        /// <summary>
        /// Initializes a new instance of the AssemblyLoadContextSandbox class.
        /// </summary>
        public AssemblyLoadContextSandbox()
        {
            _permissionCache = new PermissionCacheManager();
        }

        /// <inheritdoc />
        public SandboxType Type => SandboxType.AssemblyLoadContext;

        /// <inheritdoc />
        public bool IsActive => !_disposed && !_suspended && _loadContext != null;

        /// <inheritdoc />
        public async Task InitializeAsync(PluginSecurityPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            
            _loadContext = new PluginAssemblyLoadContext(
                policy.RequireSignedPlugins,
                policy.StrictMode);

            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<object> action)
        {
            ValidateState();

            var resourceMonitor = new PluginResourceMonitor(_policy);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                resourceMonitor.StartMonitoring();

                var result = await Task.Run(() =>
                {
                    using var scope = new AssemblyLoadContextScope(_loadContext);
                    return action();
                });

                stopwatch.Stop();
                var resourceUsage = resourceMonitor.GetCurrentUsage();
                resourceUsage.ExecutionTime = stopwatch.ElapsedMilliseconds;

                return SandboxResult.CreateSuccess(result, resourceUsage);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var resourceUsage = resourceMonitor.GetCurrentUsage();
                resourceUsage.ExecutionTime = stopwatch.ElapsedMilliseconds;

                return SandboxResult.CreateFailure(ex, resourceUsage);
            }
            finally
            {
                resourceMonitor.StopMonitoring();
            }
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<Task<object>> action)
        {
            ValidateState();

            var resourceMonitor = new PluginResourceMonitor(_policy);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                resourceMonitor.StartMonitoring();

                var result = await Task.Run(async () =>
                {
                    using var scope = new AssemblyLoadContextScope(_loadContext);
                    return await action();
                });

                stopwatch.Stop();
                var resourceUsage = resourceMonitor.GetCurrentUsage();
                resourceUsage.ExecutionTime = stopwatch.ElapsedMilliseconds;

                return SandboxResult.CreateSuccess(result, resourceUsage);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var resourceUsage = resourceMonitor.GetCurrentUsage();
                resourceUsage.ExecutionTime = stopwatch.ElapsedMilliseconds;

                return SandboxResult.CreateFailure(ex, resourceUsage);
            }
            finally
            {
                resourceMonitor.StopMonitoring();
            }
        }

        /// <inheritdoc />
        public bool ValidatePermission(PluginPermissions permission)
        {
            if (_permissionCache.TryGetCachedResult(permission, out bool cachedResult))
            {
                return cachedResult;
            }

            bool result = (_policy.DefaultPermissions & permission) == permission &&
                         (_policy.MaxPermissions & permission) == permission;

            _permissionCache.CacheResult(permission, result);
            return result;
        }

        /// <inheritdoc />
        public ResourceUsage GetResourceUsage()
        {
            return new ResourceUsage
            {
                MemoryUsed = GC.GetTotalMemory(false),
                ThreadsCreated = Process.GetCurrentProcess().Threads.Count
            };
        }

        /// <inheritdoc />
        public void Suspend()
        {
            _suspended = true;
        }

        /// <inheritdoc />
        public void Resume()
        {
            _suspended = false;
        }

        /// <inheritdoc />
        public void Terminate()
        {
            Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _loadContext?.Unload();
                _permissionCache?.Dispose();
                _disposed = true;
            }
        }

        private void ValidateState()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AssemblyLoadContextSandbox));
            
            if (_suspended)
                throw new InvalidOperationException("Sandbox is suspended");
            
            if (_loadContext == null)
                throw new InvalidOperationException("Sandbox not initialized");
        }
    }
}
```

## Resource Management

### ResourceUsage

Represents resource consumption statistics.

```csharp
namespace Neo.Plugins.Security
{
    /// <summary>
    /// Represents resource usage statistics.
    /// </summary>
    public class ResourceUsage
    {
        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsed { get; set; }

        /// <summary>
        /// CPU time used in milliseconds.
        /// </summary>
        public long CpuTimeUsed { get; set; }

        /// <summary>
        /// Number of threads created.
        /// </summary>
        public int ThreadsCreated { get; set; }

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public long ExecutionTime { get; set; }

        /// <summary>
        /// Number of network connections opened.
        /// </summary>
        public int NetworkConnections { get; set; }

        /// <summary>
        /// Number of files accessed.
        /// </summary>
        public int FilesAccessed { get; set; }

        /// <summary>
        /// Peak memory usage during execution.
        /// </summary>
        public long PeakMemoryUsed { get; set; }

        /// <summary>
        /// Gets the memory usage as a percentage of the available system memory.
        /// </summary>
        /// <returns>Memory usage percentage (0.0 to 1.0).</returns>
        public double GetMemoryUsagePercentage()
        {
            var totalMemory = GC.GetTotalMemory(false);
            return totalMemory > 0 ? (double)MemoryUsed / totalMemory : 0.0;
        }

        /// <summary>
        /// Creates a snapshot of current resource usage.
        /// </summary>
        /// <returns>A new ResourceUsage instance with current values.</returns>
        public static ResourceUsage CreateSnapshot()
        {
            return new ResourceUsage
            {
                MemoryUsed = GC.GetTotalMemory(false),
                ThreadsCreated = Process.GetCurrentProcess().Threads.Count,
                ExecutionTime = Environment.TickCount64
            };
        }

        /// <summary>
        /// Calculates the difference between two resource usage snapshots.
        /// </summary>
        /// <param name="other">The other resource usage to compare with.</param>
        /// <returns>A new ResourceUsage representing the difference.</returns>
        public ResourceUsage Subtract(ResourceUsage other)
        {
            return new ResourceUsage
            {
                MemoryUsed = Math.Max(0, this.MemoryUsed - other.MemoryUsed),
                CpuTimeUsed = Math.Max(0, this.CpuTimeUsed - other.CpuTimeUsed),
                ThreadsCreated = Math.Max(0, this.ThreadsCreated - other.ThreadsCreated),
                ExecutionTime = Math.Max(0, this.ExecutionTime - other.ExecutionTime),
                NetworkConnections = Math.Max(0, this.NetworkConnections - other.NetworkConnections),
                FilesAccessed = Math.Max(0, this.FilesAccessed - other.FilesAccessed)
            };
        }

        /// <summary>
        /// Returns a string representation of the resource usage.
        /// </summary>
        /// <returns>A formatted string showing resource usage statistics.</returns>
        public override string ToString()
        {
            return $"Memory: {MemoryUsed / 1024 / 1024}MB, " +
                   $"CPU: {CpuTimeUsed}ms, " +
                   $"Threads: {ThreadsCreated}, " +
                   $"Execution: {ExecutionTime}ms";
        }
    }
}
```

### PluginResourceMonitor

Monitors plugin resource consumption in real-time.

```csharp
namespace Neo.Plugins.Security
{
    /// <summary>
    /// Monitors resource usage for plugin sandboxes.
    /// </summary>
    public class PluginResourceMonitor : IDisposable
    {
        private readonly PluginSecurityPolicy _policy;
        private readonly Timer _monitoringTimer;
        private readonly object _lockObject = new object();
        private ResourceUsage _currentUsage;
        private ResourceUsage _baselineUsage;
        private bool _disposed;
        private bool _monitoring;

        /// <summary>
        /// Event raised when a resource limit is exceeded.
        /// </summary>
        public event EventHandler<ResourceViolationEventArgs> ResourceViolation;

        /// <summary>
        /// Initializes a new instance of the PluginResourceMonitor class.
        /// </summary>
        /// <param name="policy">The security policy defining resource limits.</param>
        public PluginResourceMonitor(PluginSecurityPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _currentUsage = new ResourceUsage();
            
            var interval = _policy.ResourceMonitoring?.CheckInterval ?? 
                          SecurityConstants.Monitoring.DefaultMinIntervalMs;
            
            _monitoringTimer = new Timer(CheckResourceLimits, null, 
                Timeout.Infinite, interval);
        }

        /// <summary>
        /// Starts resource monitoring.
        /// </summary>
        public void StartMonitoring()
        {
            lock (_lockObject)
            {
                if (_disposed || _monitoring)
                    return;

                _baselineUsage = ResourceUsage.CreateSnapshot();
                _monitoring = true;
                
                var interval = _policy.ResourceMonitoring?.CheckInterval ?? 
                              SecurityConstants.Monitoring.DefaultMinIntervalMs;
                
                _monitoringTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(interval));
            }
        }

        /// <summary>
        /// Stops resource monitoring.
        /// </summary>
        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_monitoring)
                    return;

                _monitoring = false;
                _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Gets the current resource usage.
        /// </summary>
        /// <returns>Current resource usage statistics.</returns>
        public ResourceUsage GetCurrentUsage()
        {
            lock (_lockObject)
            {
                if (_baselineUsage == null)
                    return new ResourceUsage();

                var currentSnapshot = ResourceUsage.CreateSnapshot();
                return currentSnapshot.Subtract(_baselineUsage);
            }
        }

        /// <summary>
        /// Checks if the current resource usage exceeds policy limits.
        /// </summary>
        /// <returns>True if limits are exceeded; otherwise, false.</returns>
        public bool CheckLimits()
        {
            var usage = GetCurrentUsage();
            
            if (usage.MemoryUsed > _policy.MaxMemoryBytes)
            {
                OnResourceViolation(new ResourceViolationEventArgs
                {
                    ViolationType = ResourceViolationType.Memory,
                    CurrentValue = usage.MemoryUsed,
                    LimitValue = _policy.MaxMemoryBytes,
                    ResourceUsage = usage
                });
                return false;
            }

            if (usage.ThreadsCreated > _policy.MaxThreads)
            {
                OnResourceViolation(new ResourceViolationEventArgs
                {
                    ViolationType = ResourceViolationType.Threads,
                    CurrentValue = usage.ThreadsCreated,
                    LimitValue = _policy.MaxThreads,
                    ResourceUsage = usage
                });
                return false;
            }

            return true;
        }

        private void CheckResourceLimits(object state)
        {
            if (!_monitoring || _disposed)
                return;

            try
            {
                CheckLimits();
            }
            catch (Exception ex)
            {
                // Log exception but continue monitoring
                System.Diagnostics.Debug.WriteLine($"Resource monitoring error: {ex.Message}");
            }
        }

        private void OnResourceViolation(ResourceViolationEventArgs args)
        {
            ResourceViolation?.Invoke(this, args);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                StopMonitoring();
                _monitoringTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Event arguments for resource violation events.
    /// </summary>
    public class ResourceViolationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the type of resource violation.
        /// </summary>
        public ResourceViolationType ViolationType { get; set; }

        /// <summary>
        /// Gets or sets the current resource value.
        /// </summary>
        public long CurrentValue { get; set; }

        /// <summary>
        /// Gets or sets the limit value that was exceeded.
        /// </summary>
        public long LimitValue { get; set; }

        /// <summary>
        /// Gets or sets the complete resource usage snapshot.
        /// </summary>
        public ResourceUsage ResourceUsage { get; set; }
    }

    /// <summary>
    /// Types of resource violations.
    /// </summary>
    public enum ResourceViolationType
    {
        /// <summary>
        /// Memory usage exceeded limit.
        /// </summary>
        Memory,

        /// <summary>
        /// CPU usage exceeded limit.
        /// </summary>
        Cpu,

        /// <summary>
        /// Thread count exceeded limit.
        /// </summary>
        Threads,

        /// <summary>
        /// Execution time exceeded limit.
        /// </summary>
        ExecutionTime,

        /// <summary>
        /// Network connections exceeded limit.
        /// </summary>
        NetworkConnections,

        /// <summary>
        /// File access exceeded limit.
        /// </summary>
        FileAccess
    }
}
```

## Examples

### Basic Plugin with Sandbox

```csharp
using Neo.Plugins.Security;

public class MySecurePlugin : IPlugin
{
    private IPluginSandbox _sandbox;
    private PluginSecurityPolicy _policy;

    public async Task InitializeAsync()
    {
        // Create security policy
        _policy = PluginSecurityPolicy.CreateDefault();
        _policy.DefaultPermissions = PluginPermissions.NetworkPlugin;
        _policy.MaxMemoryBytes = 128 * 1024 * 1024; // 128MB

        // Create and initialize sandbox
        _sandbox = new AssemblyLoadContextSandbox();
        await _sandbox.InitializeAsync(_policy);
    }

    public async Task<string> ProcessDataAsync(string input)
    {
        // Execute plugin logic within sandbox
        var result = await _sandbox.ExecuteAsync(async () =>
        {
            // Check permissions before sensitive operations
            if (!_sandbox.ValidatePermission(PluginPermissions.NetworkAccess))
            {
                throw new UnauthorizedAccessException("Network access not permitted");
            }

            // Perform secure processing
            return await ProcessInputSafely(input);
        });

        if (result.Success)
        {
            return result.GetResult<string>();
        }
        else
        {
            throw result.Exception;
        }
    }

    private async Task<string> ProcessInputSafely(string input)
    {
        // Plugin implementation here
        return $"Processed: {input}";
    }

    public void Dispose()
    {
        _sandbox?.Dispose();
    }
}
```

### Custom Resource Monitor

```csharp
using Neo.Plugins.Security;

public class CustomResourceMonitor
{
    private readonly PluginResourceMonitor _monitor;
    private readonly PluginSecurityPolicy _policy;

    public CustomResourceMonitor()
    {
        _policy = new PluginSecurityPolicy
        {
            MaxMemoryBytes = 256 * 1024 * 1024, // 256MB
            MaxCpuPercent = 30.0,
            MaxThreads = 15
        };

        _monitor = new PluginResourceMonitor(_policy);
        _monitor.ResourceViolation += OnResourceViolation;
    }

    public async Task MonitorPluginAsync(Func<Task> pluginAction)
    {
        try
        {
            _monitor.StartMonitoring();
            await pluginAction();
        }
        finally
        {
            _monitor.StopMonitoring();
            
            var usage = _monitor.GetCurrentUsage();
            Console.WriteLine($"Plugin executed - {usage}");
        }
    }

    private void OnResourceViolation(object sender, ResourceViolationEventArgs e)
    {
        Console.WriteLine($"Resource violation: {e.ViolationType} " +
                         $"(Current: {e.CurrentValue}, Limit: {e.LimitValue})");
        
        // Handle violation based on policy
        switch (_policy.ViolationAction)
        {
            case ViolationAction.Log:
                LogViolation(e);
                break;
            case ViolationAction.Suspend:
                SuspendPlugin();
                break;
            case ViolationAction.Terminate:
                TerminatePlugin();
                break;
        }
    }

    private void LogViolation(ResourceViolationEventArgs e)
    {
        // Log to audit system
        SecurityAuditLogger.LogViolation(e);
    }

    private void SuspendPlugin()
    {
        // Implement plugin suspension logic
    }

    private void TerminatePlugin()
    {
        // Implement plugin termination logic
    }
}
```

### Permission Validation Example

```csharp
using Neo.Plugins.Security;

public class PermissionValidator
{
    private readonly IPluginSandbox _sandbox;

    public PermissionValidator(IPluginSandbox sandbox)
    {
        _sandbox = sandbox;
    }

    public async Task<bool> TryExecuteWithPermissionAsync<T>(
        PluginPermissions requiredPermission,
        Func<Task<T>> action,
        out T result)
    {
        result = default(T);

        // Check permission before execution
        if (!_sandbox.ValidatePermission(requiredPermission))
        {
            return false;
        }

        try
        {
            var sandboxResult = await _sandbox.ExecuteAsync(async () => await action());
            
            if (sandboxResult.Success)
            {
                result = sandboxResult.GetResult<T>();
                return true;
            }
        }
        catch
        {
            // Handle execution failure
        }

        return false;
    }

    public bool HasPermissions(params PluginPermissions[] permissions)
    {
        return permissions.All(p => _sandbox.ValidatePermission(p));
    }

    public PluginPermissions GetGrantedPermissions()
    {
        var granted = PluginPermissions.None;
        
        foreach (PluginPermissions permission in Enum.GetValues<PluginPermissions>())
        {
            if (permission != PluginPermissions.None && 
                permission != PluginPermissions.FullAccess &&
                _sandbox.ValidatePermission(permission))
            {
                granted |= permission;
            }
        }

        return granted;
    }
}
```

---

**Last Updated**: December 2024  
**Version**: 1.0  
**Compatibility**: Neo N3, .NET 9.0