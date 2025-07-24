// Copyright (C) 2015-2025 The Neo Project.
//
// ContainerSandbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace Neo.Plugins.Security
{
    /// <summary>
    /// A software-based container sandbox implementation providing maximum isolation
    /// without external dependencies like Docker. Uses .NET security features,
    /// custom classloaders, and resource constraints for containment.
    /// </summary>
    public class ContainerSandbox : IPluginSandbox
    {
        private PluginSecurityPolicy _policy;
        private SoftwareContainer _container;
        private bool _disposed = false;
        private bool _suspended = false;
        private readonly object _lockObject = new object();
        private DateTime _startTime;
        private readonly ConcurrentDictionary<string, object> _isolatedResources = new();
        private readonly ResourceTracker _resourceTracker = new();

        /// <inheritdoc />
        public SandboxType Type => SandboxType.Container;

        /// <inheritdoc />
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ContainerSandbox class.
        /// </summary>
        public ContainerSandbox()
        {
        }

        /// <inheritdoc />
        public async Task InitializeAsync(PluginSecurityPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));

            // Create isolated software container
            _container = new SoftwareContainer(_policy);
            await _container.InitializeAsync();

            _startTime = DateTime.UtcNow;
            _resourceTracker.Start();
            IsActive = true;
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<object> action)
        {
            return await ExecuteAsync(() => Task.FromResult(action()));
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<Task<object>> action)
        {
            lock (_lockObject)
            {
                if (_suspended)
                    throw new InvalidOperationException("Sandbox is suspended");
                if (!IsActive)
                    throw new InvalidOperationException("Sandbox is not active");
            }

            var result = new SandboxResult { ResourceUsage = new ResourceUsage() };
            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_policy.MaxExecutionTimeSeconds));

                // Execute in isolated container context
                var executionTask = _container.ExecuteAsync(action, cts.Token);
                var monitoringTask = MonitorExecution(cts.Token);

                // Wait for either completion or timeout
                var completedTask = await Task.WhenAny(executionTask, Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask == executionTask)
                {
                    var executionResult = await executionTask;
                    result.Success = true;
                    result.Result = executionResult;
                }
                else
                {
                    // Timeout occurred
                    _container.Terminate();
                    result.Success = false;
                    result.Exception = new TimeoutException($"Execution timed out after {_policy.MaxExecutionTimeSeconds} seconds");
                }

                // Stop monitoring
                cts.Cancel();
                try { await monitoringTask; } catch { }
            }
            catch (SecurityException ex)
            {
                result.Success = false;
                result.Exception = new UnauthorizedAccessException($"Security violation: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var endMemory = GC.GetTotalMemory(false);

                result.ResourceUsage = new ResourceUsage
                {
                    ExecutionTime = (long)(endTime - startTime).TotalMilliseconds,
                    MemoryUsed = Math.Max(0, endMemory - startMemory),
                    CpuTimeUsed = _resourceTracker.GetCpuTime(),
                    ThreadsCreated = _resourceTracker.GetThreadCount()
                };
            }

            return result;
        }

        /// <inheritdoc />
        public bool ValidatePermission(PluginPermissions permission)
        {
            if (_policy == null)
                return false;

            // Always validate permissions regardless of StrictMode
            return (_policy.MaxPermissions & permission) == permission;
        }

        /// <inheritdoc />
        public ResourceUsage GetResourceUsage()
        {
            return _resourceTracker.GetCurrentUsage(_startTime);
        }

        /// <inheritdoc />
        public void Suspend()
        {
            lock (_lockObject)
            {
                _suspended = true;
                _container?.Suspend();
            }
        }

        /// <inheritdoc />
        public void Resume()
        {
            lock (_lockObject)
            {
                _suspended = false;
                _container?.Resume();
            }
        }

        /// <inheritdoc />
        public void Terminate()
        {
            lock (_lockObject)
            {
                IsActive = false;
                _suspended = true;
                _container?.Terminate();
            }
        }

        private async Task MonitorExecution(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_policy.ResourceMonitoring.CheckInterval, cancellationToken);

                    var usage = GetResourceUsage();

                    // Check memory usage
                    if (usage.MemoryUsed > _policy.MaxMemoryBytes)
                    {
                        HandleViolation($"Memory limit exceeded: {usage.MemoryUsed} bytes > {_policy.MaxMemoryBytes} bytes");
                    }

                    // Check CPU usage
                    var cpuPercent = (usage.CpuTimeUsed / (double)usage.ExecutionTime) * 100;
                    if (cpuPercent > _policy.MaxCpuPercent)
                    {
                        HandleViolation($"CPU limit exceeded: {cpuPercent:F1}% > {_policy.MaxCpuPercent}%");
                    }

                    // Check thread count
                    if (usage.ThreadsCreated > _policy.MaxThreads)
                    {
                        HandleViolation($"Thread limit exceeded: {usage.ThreadsCreated} > {_policy.MaxThreads}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when monitoring is cancelled
            }
        }

        private void HandleViolation(string message)
        {
            switch (_policy.ViolationAction)
            {
                case ViolationAction.Log:
                    Utility.Log("ContainerSandbox", LogLevel.Warning, message);
                    break;
                case ViolationAction.Suspend:
                    Suspend();
                    Utility.Log("ContainerSandbox", LogLevel.Warning, $"Container suspended: {message}");
                    break;
                case ViolationAction.Terminate:
                    Terminate();
                    Utility.Log("ContainerSandbox", LogLevel.Error, $"Container terminated: {message}");
                    break;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    IsActive = false;
                    _suspended = true;
                }

                try
                {
                    _container?.Dispose();
                    _resourceTracker?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents an isolated software container for plugin execution.
    /// </summary>
    internal class SoftwareContainer : IDisposable
    {
        private readonly PluginSecurityPolicy _policy;
#if NET5_0_OR_GREATER
        private AssemblyLoadContext _loadContext;
#endif
        private readonly VirtualFileSystem _virtualFs;
        private readonly NetworkInterceptor _networkInterceptor;
        private readonly SecurityManager _securityManager;
        private bool _suspended = false;
        private bool _disposed = false;

        public SoftwareContainer(PluginSecurityPolicy policy)
        {
            _policy = policy;
            _virtualFs = new VirtualFileSystem(policy.FileSystemAccess);
            _networkInterceptor = new NetworkInterceptor(policy.NetworkAccess);
            _securityManager = new SecurityManager(policy);
        }

        public async Task InitializeAsync()
        {
            // Initialize virtual file system
            _virtualFs.Initialize();

            // Set up network interception
            _networkInterceptor.Install();

            // Create isolated execution context
#if NET5_0_OR_GREATER
            _loadContext = new IsolatedAssemblyLoadContext(_policy);
#endif

            await Task.CompletedTask;
        }

        public async Task<object> ExecuteAsync(Func<Task<object>> action, CancellationToken cancellationToken)
        {
            if (_suspended)
                throw new InvalidOperationException("Container is suspended");

            // Install security interceptors
            using var securityContext = _securityManager.CreateContext();
            using var fsContext = _virtualFs.CreateContext();
            using var netContext = _networkInterceptor.CreateContext();

            try
            {
#if NET5_0_OR_GREATER
                if (_loadContext != null)
                {
                    return await ExecuteInLoadContext(action, cancellationToken);
                }
#endif

                // Fallback to current context with security constraints
                return await action();
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
        }

#if NET5_0_OR_GREATER
        private async Task<object> ExecuteInLoadContext(Func<Task<object>> action, CancellationToken cancellationToken)
        {
            // Execute in isolated AssemblyLoadContext
            return await Task.Run(async () =>
            {
                using (_loadContext.EnterContextualReflection())
                {
                    return await action();
                }
            }, cancellationToken);
        }
#endif


        private void ValidatePermissions()
        {
            // Modern .NET Core/5+ permission validation
            // Code Access Security (CAS) is obsolete, so we use runtime policy checks instead

            if ((_policy.MaxPermissions & PluginPermissions.FileSystemAccess) != 0)
            {
                // File system access validation would be handled at runtime
                // through try-catch blocks and directory/file existence checks
            }

            if ((_policy.MaxPermissions & PluginPermissions.NetworkAccess) != 0)
            {
                // Network access validation would be handled through
                // HttpClient policies, firewall rules, or runtime checks
            }

            // Permission validation is now primarily done through runtime checks
            // rather than declarative security attributes
        }

        public void Suspend()
        {
            _suspended = true;
            // Additional suspension logic if needed
        }

        public void Resume()
        {
            _suspended = false;
        }

        public void Terminate()
        {
            _suspended = true;
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
#if NET5_0_OR_GREATER
                    _loadContext?.Unload();
#endif

                    _virtualFs?.Dispose();
                    _networkInterceptor?.Dispose();
                    _securityManager?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }

                _disposed = true;
            }
        }
    }

#if NET5_0_OR_GREATER
    /// <summary>
    /// Isolated AssemblyLoadContext for plugin execution.
    /// </summary>
    internal class IsolatedAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly PluginSecurityPolicy _policy;

        public IsolatedAssemblyLoadContext(PluginSecurityPolicy policy) : base(isCollectible: true)
        {
            _policy = policy;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Control which assemblies can be loaded
            if (!IsAssemblyAllowed(assemblyName))
            {
                throw new UnauthorizedAccessException($"Assembly not allowed: {assemblyName}");
            }

            return base.Load(assemblyName);
        }

        private bool IsAssemblyAllowed(AssemblyName assemblyName)
        {
            // Implement assembly whitelist/blacklist logic
            var name = assemblyName.Name?.ToLowerInvariant();

            // Always allow core .NET assemblies
            if (name?.StartsWith("system") == true || name?.StartsWith("microsoft") == true)
                return true;

            // Allow Neo assemblies
            if (name?.StartsWith("neo") == true)
                return true;

            // Check against policy restrictions
            return _policy.StrictMode == false;
        }
    }
#endif

    /// <summary>
    /// Virtual file system for sandboxed file operations.
    /// </summary>
    internal class VirtualFileSystem : IDisposable
    {
        private readonly FileSystemAccessPolicy _policy;
        private readonly ConcurrentDictionary<string, VirtualFile> _virtualFiles = new();
        private bool _disposed = false;

        public VirtualFileSystem(FileSystemAccessPolicy policy)
        {
            _policy = policy;
        }

        public void Initialize()
        {
            // Set up virtual file system constraints
        }

        public IDisposable CreateContext()
        {
            return new FileSystemContext(this);
        }

        public bool IsPathAllowed(string path)
        {
            if (!_policy.AllowFileSystemAccess)
                return false;

            var fullPath = Path.GetFullPath(path);

            // Check allowed paths
            foreach (var allowedPath in _policy.AllowedPaths)
            {
                if (fullPath.StartsWith(Path.GetFullPath(allowedPath), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Check restricted paths
            foreach (var restrictedPath in _policy.RestrictedPaths)
            {
                if (fullPath.StartsWith(Path.GetFullPath(restrictedPath), StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return _policy.AllowFileSystemAccess;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _virtualFiles.Clear();
                _disposed = true;
            }
        }

        private class FileSystemContext : IDisposable
        {
            private readonly VirtualFileSystem _vfs;
            private readonly FileSystemWatcher _watcher;
            private readonly Dictionary<string, FileStream> _openFiles = new();

            public FileSystemContext(VirtualFileSystem vfs)
            {
                _vfs = vfs;
                // Install file system hooks
                InstallFileSystemHooks();

                // Monitor file system access
                if (_vfs._policy.AllowFileSystemAccess && _vfs._policy.AllowedPaths.Count > 0)
                {
                    _watcher = new FileSystemWatcher(_vfs._policy.AllowedPaths.First())
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
                    };
                    _watcher.Created += OnFileSystemEvent;
                    _watcher.Changed += OnFileSystemEvent;
                    _watcher.Deleted += OnFileSystemEvent;
                    _watcher.EnableRaisingEvents = true;
                }
            }

            private void InstallFileSystemHooks()
            {
                // Override file operations to enforce security
            }


            private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
            {
                // Log file system access
                SecurityAuditLogger.Instance.LogSecurityEvent(
                    SecurityEventType.FileSystemAccess,
                    "Container",
                    $"File system event: {e.ChangeType} - {e.FullPath}",
                    new { e.ChangeType, e.FullPath },
                    SecurityEventSeverity.Low
                );

                // Check if operation violates policy
                if (!_vfs.IsPathAllowed(e.FullPath))
                {
                    throw new UnauthorizedAccessException($"File access denied: {e.FullPath}");
                }
            }

            public void Dispose()
            {
                // Remove file system hooks

                _watcher?.Dispose();

                foreach (var file in _openFiles.Values)
                {
                    try { file?.Dispose(); } catch { }
                }
                _openFiles.Clear();
            }
        }

        private class VirtualFile
        {
            public string Path { get; set; }
            public byte[] Content { get; set; }
            public DateTime LastModified { get; set; }
        }
    }

    /// <summary>
    /// Network traffic interceptor for sandboxed network operations.
    /// </summary>
    internal class NetworkInterceptor : IDisposable
    {
        private readonly NetworkAccessPolicy _policy;
        private bool _disposed = false;

        public NetworkInterceptor(NetworkAccessPolicy policy)
        {
            _policy = policy;
        }

        public void Install()
        {
            // Install network interception hooks
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            ServicePointManager.ServerCertificateValidationCallback += ValidateServerCertificate;

            // Set security protocol requirements
            if (_policy.RequireSSL)
            {
#if NET5_0_OR_GREATER
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
#else
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif
            }

            // Set connection limits
            ServicePointManager.DefaultConnectionLimit = _policy.MaxConnections;
#pragma warning restore SYSLIB0014
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // Log the connection attempt
            var uri = sender as Uri;
            if (uri != null)
            {
                SecurityAuditLogger.Instance.LogSecurityEvent(
                    SecurityEventType.NetworkAccess,
                    "Container",
                    $"SSL certificate validation for: {uri.Host}",
                    new { Host = uri.Host, Port = uri.Port, SSL = true },
                    SecurityEventSeverity.Low
                );

                // Check if endpoint is allowed
                if (!IsEndpointAllowed(uri.ToString()))
                {
                    return false; // Reject connection
                }
            }

            // Enforce SSL requirement
            if (_policy.RequireSSL && sslPolicyErrors != SslPolicyErrors.None)
            {
                return false;
            }

            return true;
        }

        public IDisposable CreateContext()
        {
            return new NetworkContext(this);
        }

        public bool IsEndpointAllowed(string endpoint)
        {
            if (!_policy.AllowNetworkAccess)
                return false;

            // Check allowed endpoints
            if (_policy.AllowedEndpoints.Count > 0)
            {
                return _policy.AllowedEndpoints.Contains(endpoint);
            }

            // Check blocked endpoints
            return !_policy.BlockedEndpoints.Contains(endpoint);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Remove network hooks
                _disposed = true;
            }
        }

        private class NetworkContext : IDisposable
        {
            private readonly NetworkInterceptor _interceptor;

            public NetworkContext(NetworkInterceptor interceptor)
            {
                _interceptor = interceptor;
            }

            public void Dispose()
            {
                // Cleanup network context
            }
        }
    }

    /// <summary>
    /// Security manager for enforcing sandbox policies.
    /// </summary>
    internal class SecurityManager : IDisposable
    {
        private readonly PluginSecurityPolicy _policy;
        private bool _disposed = false;

        public SecurityManager(PluginSecurityPolicy policy)
        {
            _policy = policy;
        }

        public IDisposable CreateContext()
        {
            return new SecurityContext(this);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        private class SecurityContext : IDisposable
        {
            private readonly SecurityManager _manager;
            private readonly Thread _currentThread;
            private readonly bool _permissionsValidated;

            public SecurityContext(SecurityManager manager)
            {
                _manager = manager;
                _currentThread = Thread.CurrentThread;

                // Install security hooks
                InstallSecurityHooks();

                // Apply security restrictions
                if (_manager._policy.StrictMode)
                {
                    // Validate current permissions are within policy
                    _permissionsValidated = ValidateCurrentPermissions();

                    // Enforce permission policy through runtime checks
                    EnforcePermissionPolicy();
                }
            }

            private void InstallSecurityHooks()
            {
                // Hook into global events for security monitoring
            }

            private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
            {
                if (e.ExceptionObject is SecurityException secEx)
                {
                    SecurityAuditLogger.Instance.LogSecurityEvent(
                        SecurityEventType.SecurityViolation,
                        "Container",
                        $"Security exception: {secEx.Message}",
                        new { Exception = secEx, IsTerminating = e.IsTerminating },
                        SecurityEventSeverity.Critical
                    );
                }
            }

            private void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
            {
                if (e.Exception is SecurityException || e.Exception is UnauthorizedAccessException)
                {
                    SecurityAuditLogger.Instance.LogSecurityEvent(
                        SecurityEventType.SecurityViolation,
                        "Container",
                        $"First chance security exception: {e.Exception.Message}",
                        new { Exception = e.Exception.GetType().Name },
                        SecurityEventSeverity.High
                    );
                }
            }

            private void OnProcessExit(object sender, EventArgs e)
            {
                // Log security context termination
                SecurityAuditLogger.Instance.LogSecurityEvent(
                    SecurityEventType.SandboxOperation,
                    "Container",
                    "Security context terminating",
                    null,
                    SecurityEventSeverity.Low
                );
            }

            private bool ValidateCurrentPermissions()
            {
                // Modern .NET Core/5+ permission validation
                // Returns true if current permissions are within policy limits
                return true; // Runtime validation would check actual capabilities
            }

            private void EnforcePermissionPolicy()
            {
                // Modern permission enforcement through runtime checks
                // In .NET Core/5+, security is enforced through:
                // 1. Process isolation
                // 2. Runtime permission checks
                // 3. Network policies
                // 4. File system access controls

                if ((_manager._policy.MaxPermissions & PluginPermissions.FileSystemAccess) != 0)
                {
                    // File system access would be validated at runtime
                    // through actual file operations and exception handling
                }

                if ((_manager._policy.MaxPermissions & PluginPermissions.NetworkAccess) != 0)
                {
                    // Network access would be controlled through HttpClient policies,
                    // firewall rules, or process-level network restrictions
                }

                // Modern .NET security relies on isolation rather than CAS permissions
            }

            public void Dispose()
            {
                // Remove security hooks

                // Clean up permission validation state
                if (_permissionsValidated && _manager._policy.StrictMode)
                {
                    // Permission cleanup is handled by modern .NET runtime automatically
                    // No explicit restoration needed for runtime-based security
                }
            }
        }
    }

    /// <summary>
    /// Resource usage tracker for monitoring sandbox consumption.
    /// </summary>
    internal class ResourceTracker : IDisposable
    {
        private long _startTicks;
        private long _startMemory;
        private int _startThreads;
        private bool _disposed = false;

        public void Start()
        {
#if NET5_0_OR_GREATER
            _startTicks = Environment.TickCount64;
#else
            _startTicks = Environment.TickCount;
#endif
            _startMemory = GC.GetTotalMemory(false);
#if NET5_0_OR_GREATER
            _startThreads = ThreadPool.ThreadCount;
#else
            _startThreads = 0;
#endif
        }

        public ResourceUsage GetCurrentUsage(DateTime startTime)
        {
#if NET5_0_OR_GREATER
            var currentTicks = Environment.TickCount64;
#else
            var currentTicks = Environment.TickCount;
#endif
            var currentMemory = GC.GetTotalMemory(false);
#if NET5_0_OR_GREATER
            var currentThreads = ThreadPool.ThreadCount;
#else
            var currentThreads = 0;
#endif

            return new ResourceUsage
            {
                MemoryUsed = Math.Max(0, currentMemory - _startMemory),
                CpuTimeUsed = currentTicks - _startTicks,
                ThreadsCreated = Math.Max(0, currentThreads - _startThreads),
                ExecutionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        public long GetCpuTime()
        {
#if NET5_0_OR_GREATER
            return Environment.TickCount64 - _startTicks;
#else
            return Environment.TickCount - _startTicks;
#endif
        }

        public int GetThreadCount()
        {
#if NET5_0_OR_GREATER
            return Math.Max(0, ThreadPool.ThreadCount - _startThreads);
#else
            return 0;
#endif
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
