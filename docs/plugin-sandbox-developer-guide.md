# Neo Plugin Sandbox Developer Guide

## Table of Contents

- [Getting Started](#getting-started)
- [Understanding the Security Model](#understanding-the-security-model)
- [Creating Secure Plugins](#creating-secure-plugins)
- [Permission System](#permission-system)
- [Resource Management](#resource-management)
- [Best Practices](#best-practices)
- [Common Patterns](#common-patterns)
- [Testing and Debugging](#testing-and-debugging)
- [Performance Optimization](#performance-optimization)
- [Migration Guide](#migration-guide)

## Getting Started

The Neo Plugin Sandbox System provides a secure execution environment for third-party plugins. This guide will help you develop plugins that work effectively within the security constraints while maintaining high performance.

### Prerequisites

- .NET 9.0 or later
- Neo N3 development environment
- Understanding of C# and async programming
- Basic knowledge of security concepts

### Quick Start Example

Here's a minimal secure plugin implementation:

```csharp
using Neo.Plugins.Security;
using Neo.Plugins;

[Plugin]
public class MySecurePlugin : Plugin
{
    private IPluginSandbox _sandbox;
    private PluginSecurityPolicy _policy;

    protected override void OnSystemLoaded(NeoSystem system)
    {
        // Initialize security context
        InitializeSecurity();
        
        // Plugin-specific initialization
        base.OnSystemLoaded(system);
    }

    private async void InitializeSecurity()
    {
        // Define security requirements
        _policy = new PluginSecurityPolicy
        {
            DefaultPermissions = PluginPermissions.NetworkPlugin,
            MaxMemoryBytes = 128 * 1024 * 1024, // 128MB
            MaxCpuPercent = 25.0,
            MaxThreads = 10,
            ViolationAction = ViolationAction.Suspend
        };

        // Create sandbox
        _sandbox = new AssemblyLoadContextSandbox();
        await _sandbox.InitializeAsync(_policy);
    }

    // Secure method execution
    public async Task<string> ProcessDataAsync(string data)
    {
        return await ExecuteSecurely(async () =>
        {
            // Your plugin logic here
            return await ProcessDataInternal(data);
        });
    }

    private async Task<T> ExecuteSecurely<T>(Func<Task<T>> action)
    {
        var result = await _sandbox.ExecuteAsync(async () => await action());
        
        if (result.Success)
        {
            return result.GetResult<T>();
        }
        else
        {
            throw new PluginException("Execution failed", result.Exception);
        }
    }

    private async Task<string> ProcessDataInternal(string data)
    {
        // Implement your plugin logic
        return $"Processed: {data}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sandbox?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Understanding the Security Model

### Security Levels

The Neo Plugin Sandbox provides four levels of security isolation:

1. **PassThrough** (Development Only)
   - No security restrictions
   - Direct access to all resources
   - Highest performance
   - Use only in trusted development environments

2. **AssemblyLoadContext** (Recommended)
   - Application domain-level isolation
   - Controlled type loading
   - Good performance with security
   - Suitable for most production scenarios

3. **Process** (High Security)
   - OS process-level isolation
   - Complete memory separation
   - Inter-process communication overhead
   - Use for untrusted plugins

4. **Container** (Maximum Security)
   - Container-based isolation
   - Network and filesystem isolation
   - Highest security overhead
   - Use for completely untrusted code

### Trust Boundaries

Understanding trust boundaries helps design secure plugins:

```
┌─────────────────────────────────────────┐
│              Neo Node Core              │  ← Trusted Zone
├─────────────────────────────────────────┤
│        Plugin Security Manager         │  ← Security Boundary
├─────────────────────────────────────────┤
│             Plugin Sandbox             │  ← Controlled Zone
│  ┌───────────────────────────────────┐  │
│  │         Your Plugin Code          │  │  ← Plugin Zone
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Creating Secure Plugins

### Plugin Structure

A well-structured secure plugin follows this pattern:

```csharp
using Neo.Plugins.Security;

[Plugin]
public class SecureExamplePlugin : Plugin
{
    // Security components
    private IPluginSandbox _sandbox;
    private PluginSecurityPolicy _policy;
    private PluginResourceMonitor _resourceMonitor;
    private SecurityAuditLogger _auditLogger;

    // Plugin state
    private readonly Dictionary<string, object> _pluginState;
    private bool _initialized;

    public SecureExamplePlugin()
    {
        _pluginState = new Dictionary<string, object>();
    }

    protected override void Configure()
    {
        // Define plugin configuration
        Settings.Load(GetConfiguration());
    }

    protected override void OnSystemLoaded(NeoSystem system)
    {
        InitializeSecurityAsync().Wait();
        InitializePlugin(system);
    }

    private async Task InitializeSecurityAsync()
    {
        // 1. Create security policy
        _policy = CreateSecurityPolicy();
        
        // 2. Initialize sandbox
        _sandbox = CreateSandbox();
        await _sandbox.InitializeAsync(_policy);
        
        // 3. Setup resource monitoring
        _resourceMonitor = new PluginResourceMonitor(_policy);
        _resourceMonitor.ResourceViolation += OnResourceViolation;
        
        // 4. Configure audit logging
        _auditLogger = new SecurityAuditLogger("SecureExamplePlugin");
        
        _initialized = true;
    }

    private PluginSecurityPolicy CreateSecurityPolicy()
    {
        return new PluginSecurityPolicy
        {
            DefaultPermissions = PluginPermissions.ServicePlugin,
            MaxMemissions = PluginPermissions.AdminPlugin,
            MaxMemoryBytes = 256 * 1024 * 1024, // 256MB
            MaxCpuPercent = 30.0,
            MaxThreads = 15,
            MaxExecutionTimeSeconds = 300,
            ViolationAction = ViolationAction.Suspend,
            StrictMode = true,
            RequireSignedPlugins = true,
            SandboxType = SandboxType.AssemblyLoadContext,
            
            // File system restrictions
            FileSystemAccess = new FileSystemAccessPolicy
            {
                AllowFileSystemAccess = true,
                AllowedPaths = new List<string> { "/opt/neo/plugin-data" },
                RestrictedPaths = new List<string> { "/etc", "/sys", "/proc" },
                MaxFileSize = 100 * 1024 * 1024, // 100MB
                MaxTotalFiles = 1000
            },
            
            // Network restrictions
            NetworkAccess = new NetworkAccessPolicy
            {
                AllowNetworkAccess = true,
                AllowedEndpoints = new List<string> { "api.neo.org" },
                AllowedPorts = new List<int> { 80, 443, 10332, 10333 },
                MaxConnections = 10,
                RequireSSL = true
            }
        };
    }

    private IPluginSandbox CreateSandbox()
    {
        // Choose sandbox based on trust level
        return Environment.GetEnvironmentVariable("NEO_PLUGIN_TRUST_LEVEL") switch
        {
            "development" => new PassThroughSandbox(),
            "production" => new AssemblyLoadContextSandbox(),
            "high-security" => new ProcessSandbox(),
            "untrusted" => new ContainerSandbox(),
            _ => new AssemblyLoadContextSandbox()
        };
    }

    // Secure execution wrapper
    private async Task<T> ExecuteSecurely<T>(Func<Task<T>> action, string operationName = null)
    {
        if (!_initialized)
            throw new InvalidOperationException("Plugin not initialized");

        try
        {
            _resourceMonitor.StartMonitoring();
            _auditLogger.LogOperation(operationName ?? "Unknown", "Started");

            var result = await _sandbox.ExecuteAsync(async () => await action());

            if (result.Success)
            {
                _auditLogger.LogOperation(operationName ?? "Unknown", "Completed", result.ResourceUsage);
                return result.GetResult<T>();
            }
            else
            {
                _auditLogger.LogError(operationName ?? "Unknown", result.Exception);
                throw new PluginExecutionException("Secure execution failed", result.Exception);
            }
        }
        finally
        {
            _resourceMonitor.StopMonitoring();
        }
    }

    private void OnResourceViolation(object sender, ResourceViolationEventArgs e)
    {
        _auditLogger.LogViolation(e);
        
        switch (_policy.ViolationAction)
        {
            case ViolationAction.Log:
                // Already logged
                break;
            case ViolationAction.Suspend:
                _sandbox.Suspend();
                break;
            case ViolationAction.Terminate:
                _sandbox.Terminate();
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _resourceMonitor?.Dispose();
            _sandbox?.Dispose();
            _auditLogger?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Permission System

### Understanding Permissions

The permission system uses a bitwise flag enumeration allowing fine-grained control:

```csharp
// Basic permissions
PluginPermissions.ReadOnly          // Blockchain read access
PluginPermissions.StorageAccess     // Blockchain write access
PluginPermissions.NetworkAccess     // Network communication
PluginPermissions.FileSystemAccess  // File system operations

// Advanced permissions
PluginPermissions.SystemAccess      // System-level operations
PluginPermissions.CryptographicAccess // Crypto operations
PluginPermissions.ProcessAccess     // Process management
PluginPermissions.AdminAccess       // Administrative operations

// Predefined sets
PluginPermissions.NetworkPlugin     // ReadOnly + NetworkAccess
PluginPermissions.ServicePlugin     // ReadOnly + StorageAccess + NetworkAccess
PluginPermissions.AdminPlugin       // Most permissions except dangerous ones
```

### Permission Validation Patterns

#### 1. Check Before Execute

```csharp
public async Task<bool> SendNetworkRequestAsync(string url)
{
    // Always validate permissions before sensitive operations
    if (!_sandbox.ValidatePermission(PluginPermissions.NetworkAccess))
    {
        _auditLogger.LogPermissionDenied("NetworkAccess", url);
        return false;
    }

    return await ExecuteSecurely(async () =>
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        return response.IsSuccessStatusCode;
    }, "SendNetworkRequest");
}
```

#### 2. Conditional Functionality

```csharp
public class FeatureManager
{
    private readonly IPluginSandbox _sandbox;

    public FeatureManager(IPluginSandbox sandbox)
    {
        _sandbox = sandbox;
    }

    public bool CanUseDatabase => _sandbox.ValidatePermission(PluginPermissions.DatabaseAccess);
    public bool CanAccessFiles => _sandbox.ValidatePermission(PluginPermissions.FileSystemAccess);
    public bool CanUseNetwork => _sandbox.ValidatePermission(PluginPermissions.NetworkAccess);

    public async Task<string> GetDataAsync(string key)
    {
        if (CanUseDatabase)
        {
            return await GetFromDatabase(key);
        }
        else if (CanAccessFiles)
        {
            return await GetFromFile(key);
        }
        else
        {
            return GetFromMemory(key);
        }
    }
}
```

#### 3. Permission Escalation

```csharp
public class PermissionEscalator
{
    private readonly IPluginSandbox _sandbox;

    public async Task<T> RequestElevatedOperation<T>(
        PluginPermissions requiredPermission,
        Func<Task<T>> operation,
        string justification)
    {
        if (_sandbox.ValidatePermission(requiredPermission))
        {
            return await operation();
        }

        // Log permission request for admin review
        _auditLogger.LogPermissionRequest(requiredPermission, justification);
        
        // Could implement dynamic permission granting here
        throw new UnauthorizedAccessException(
            $"Operation requires {requiredPermission} permission: {justification}");
    }
}
```

## Resource Management

### Memory Management

Efficient memory usage is crucial in sandboxed environments:

```csharp
public class MemoryEfficientPlugin : Plugin
{
    private readonly MemoryPool<byte> _memoryPool;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public MemoryEfficientPlugin()
    {
        _memoryPool = MemoryPool<byte>.Shared;
        _stringBuilderPool = new DefaultObjectPool<StringBuilder>(
            new StringBuilderPooledObjectPolicy());
    }

    public async Task<string> ProcessLargeDataAsync(Stream data)
    {
        return await ExecuteSecurely(async () =>
        {
            // Use pooled memory to reduce allocations
            using var buffer = _memoryPool.Rent(8192);
            var stringBuilder = _stringBuilderPool.Get();
            
            try
            {
                int bytesRead;
                while ((bytesRead = await data.ReadAsync(buffer.Memory)) > 0)
                {
                    // Process chunk
                    var chunk = ProcessChunk(buffer.Memory.Span[..bytesRead]);
                    stringBuilder.Append(chunk);
                    
                    // Periodic GC hint for long-running operations
                    if (stringBuilder.Length % 100000 == 0)
                    {
                        GC.Collect(0, GCCollectionMode.Optimized);
                    }
                }
                
                return stringBuilder.ToString();
            }
            finally
            {
                stringBuilder.Clear();
                _stringBuilderPool.Return(stringBuilder);
            }
        }, "ProcessLargeData");
    }

    private string ProcessChunk(ReadOnlySpan<byte> chunk)
    {
        // Efficient chunk processing without allocations
        return System.Text.Encoding.UTF8.GetString(chunk);
    }
}
```

### Thread Management

Proper thread management prevents resource exhaustion:

```csharp
public class ThreadManagedPlugin : Plugin
{
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ThreadManagedPlugin()
    {
        // Limit concurrent operations based on sandbox policy
        var maxThreads = _policy?.MaxThreads ?? 10;
        _concurrencyLimiter = new SemaphoreSlim(maxThreads / 2, maxThreads / 2);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task<List<T>> ProcessConcurrentlyAsync<T>(
        IEnumerable<T> items,
        Func<T, Task<T>> processor)
    {
        var results = new ConcurrentBag<T>();
        var tasks = items.Select(async item =>
        {
            await _concurrencyLimiter.WaitAsync(_cancellationTokenSource.Token);
            
            try
            {
                var result = await ExecuteSecurely(async () => await processor(item));
                results.Add(result);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }

    // Graceful shutdown
    public async Task ShutdownAsync(TimeSpan timeout)
    {
        _cancellationTokenSource.Cancel();
        
        try
        {
            await Task.Delay(timeout, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _concurrencyLimiter?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Best Practices

### 1. Fail-Safe Design

Design your plugin to gracefully handle permission denials and resource limitations:

```csharp
public class RobustPlugin : Plugin
{
    public async Task<OperationResult> PerformOperationAsync(string data)
    {
        try
        {
            // Primary approach - full functionality
            if (_sandbox.ValidatePermission(PluginPermissions.ServicePlugin))
            {
                return await PerformFullOperationAsync(data);
            }
            
            // Fallback approach - limited functionality
            if (_sandbox.ValidatePermission(PluginPermissions.ReadOnly))
            {
                return await PerformLimitedOperationAsync(data);
            }
            
            // Minimal approach - cached/offline functionality
            return PerformOfflineOperation(data);
        }
        catch (Exception ex)
        {
            _auditLogger.LogError("PerformOperation", ex);
            return OperationResult.CreateFailure(ex.Message);
        }
    }

    private async Task<OperationResult> PerformFullOperationAsync(string data)
    {
        return await ExecuteSecurely(async () =>
        {
            // Full implementation with all features
            var result = await ProcessWithNetworkAndStorage(data);
            return OperationResult.CreateSuccess(result);
        });
    }

    private async Task<OperationResult> PerformLimitedOperationAsync(string data)
    {
        return await ExecuteSecurely(async () =>
        {
            // Limited implementation without network/storage
            var result = ProcessLocally(data);
            return OperationResult.CreateSuccess(result);
        });
    }

    private OperationResult PerformOfflineOperation(string data)
    {
        // Minimal functionality without sandbox
        var result = GetCachedResult(data);
        return OperationResult.CreateSuccess(result);
    }
}
```

### 2. Resource Monitoring

Actively monitor and respond to resource usage:

```csharp
public class ResourceAwarePlugin : Plugin
{
    private readonly ResourceUsageTracker _usageTracker;

    public ResourceAwarePlugin()
    {
        _usageTracker = new ResourceUsageTracker();
    }

    public async Task<T> AdaptiveExecutionAsync<T>(Func<Task<T>> operation)
    {
        var currentUsage = _sandbox.GetResourceUsage();
        
        // Adapt behavior based on current resource usage
        if (currentUsage.MemoryUsed > _policy.MaxMemoryBytes * 0.8)
        {
            // High memory usage - trigger GC before operation
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
        }

        if (_usageTracker.GetCpuUsagePercent() > _policy.MaxCpuPercent * 0.7)
        {
            // High CPU usage - add delay to throttle
            await Task.Delay(100);
        }

        return await ExecuteSecurely(operation);
    }

    private class ResourceUsageTracker
    {
        private readonly Queue<DateTime> _recentOperations = new();
        
        public double GetCpuUsagePercent()
        {
            // Simple CPU usage estimation based on operation frequency
            var now = DateTime.UtcNow;
            var recentThreshold = now.AddSeconds(-60);
            
            while (_recentOperations.Count > 0 && _recentOperations.Peek() < recentThreshold)
            {
                _recentOperations.Dequeue();
            }
            
            _recentOperations.Enqueue(now);
            
            // Rough estimation: operations per minute as CPU percentage
            return Math.Min(100.0, _recentOperations.Count * 2.0);
        }
    }
}
```

### 3. Error Handling and Recovery

Implement comprehensive error handling with recovery mechanisms:

```csharp
public class ResilientPlugin : Plugin
{
    private readonly CircuitBreaker _circuitBreaker;
    private readonly RetryPolicy _retryPolicy;

    public ResilientPlugin()
    {
        _circuitBreaker = new CircuitBreaker(
            failureThreshold: 5,
            recoveryTimeout: TimeSpan.FromMinutes(1));
            
        _retryPolicy = new RetryPolicy(
            maxAttempts: 3,
            backoffStrategy: ExponentialBackoff.CreateDefault());
    }

    public async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            if (_circuitBreaker.State == CircuitBreakerState.Open)
            {
                throw new CircuitBreakerOpenException("Circuit breaker is open");
            }

            try
            {
                var result = await ExecuteSecurely(operation);
                _circuitBreaker.RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                _circuitBreaker.RecordFailure();
                
                if (IsRetriableException(ex))
                {
                    throw; // Let retry policy handle it
                }
                
                // Non-retriable exception
                throw new NonRetriableException("Operation failed permanently", ex);
            }
        });
    }

    private bool IsRetriableException(Exception ex)
    {
        return ex is TimeoutException ||
               ex is SocketException ||
               ex is HttpRequestException ||
               ex is ResourceViolationException;
    }
}
```

## Common Patterns

### 1. Plugin Factory Pattern

Create plugins with appropriate security configurations:

```csharp
public static class SecurePluginFactory
{
    public static T CreatePlugin<T>(PluginSecurityLevel securityLevel) 
        where T : Plugin, new()
    {
        var plugin = new T();
        var policy = CreatePolicyForLevel(securityLevel);
        var sandbox = CreateSandboxForLevel(securityLevel);
        
        // Initialize security context
        plugin.SetSecurityContext(sandbox, policy);
        
        return plugin;
    }

    private static PluginSecurityPolicy CreatePolicyForLevel(PluginSecurityLevel level)
    {
        return level switch
        {
            PluginSecurityLevel.Development => PluginSecurityPolicy.CreatePermissive(),
            PluginSecurityLevel.Production => PluginSecurityPolicy.CreateDefault(),
            PluginSecurityLevel.HighSecurity => PluginSecurityPolicy.CreateRestrictive(),
            _ => PluginSecurityPolicy.CreateDefault()
        };
    }

    private static IPluginSandbox CreateSandboxForLevel(PluginSecurityLevel level)
    {
        return level switch
        {
            PluginSecurityLevel.Development => new PassThroughSandbox(),
            PluginSecurityLevel.Production => new AssemblyLoadContextSandbox(),
            PluginSecurityLevel.HighSecurity => new ProcessSandbox(),
            _ => new AssemblyLoadContextSandbox()
        };
    }
}

public enum PluginSecurityLevel
{
    Development,
    Production,
    HighSecurity
}
```

### 2. Command Pattern with Security

Implement commands with built-in security validation:

```csharp
public abstract class SecureCommand
{
    protected readonly IPluginSandbox Sandbox;
    protected readonly SecurityAuditLogger AuditLogger;

    protected SecureCommand(IPluginSandbox sandbox, SecurityAuditLogger auditLogger)
    {
        Sandbox = sandbox;
        AuditLogger = auditLogger;
    }

    public async Task<CommandResult> ExecuteAsync()
    {
        // Pre-execution security check
        if (!ValidatePermissions())
        {
            AuditLogger.LogPermissionDenied(GetType().Name, GetRequiredPermissions());
            return CommandResult.CreateFailure("Insufficient permissions");
        }

        try
        {
            AuditLogger.LogCommandStart(GetType().Name);
            
            var result = await Sandbox.ExecuteAsync(async () => await ExecuteImplementationAsync());
            
            if (result.Success)
            {
                AuditLogger.LogCommandSuccess(GetType().Name, result.ResourceUsage);
                return CommandResult.CreateSuccess(result.Result);
            }
            else
            {
                AuditLogger.LogCommandFailure(GetType().Name, result.Exception);
                return CommandResult.CreateFailure(result.Exception.Message);
            }
        }
        catch (Exception ex)
        {
            AuditLogger.LogCommandError(GetType().Name, ex);
            return CommandResult.CreateFailure(ex.Message);
        }
    }

    protected abstract Task<object> ExecuteImplementationAsync();
    protected abstract PluginPermissions GetRequiredPermissions();

    private bool ValidatePermissions()
    {
        var required = GetRequiredPermissions();
        return Sandbox.ValidatePermission(required);
    }
}

// Example implementation
public class SendNotificationCommand : SecureCommand
{
    private readonly string _message;
    private readonly string _endpoint;

    public SendNotificationCommand(string message, string endpoint, 
        IPluginSandbox sandbox, SecurityAuditLogger auditLogger)
        : base(sandbox, auditLogger)
    {
        _message = message;
        _endpoint = endpoint;
    }

    protected override async Task<object> ExecuteImplementationAsync()
    {
        using var client = new HttpClient();
        var response = await client.PostAsync(_endpoint, 
            new StringContent(_message, Encoding.UTF8, "application/json"));
        
        return response.IsSuccessStatusCode;
    }

    protected override PluginPermissions GetRequiredPermissions()
    {
        return PluginPermissions.NetworkAccess;
    }
}
```

### 3. Observer Pattern for Security Events

Implement security-aware event handling:

```csharp
public class SecurityAwareEventManager
{
    private readonly Dictionary<Type, List<ISecureEventHandler>> _handlers;
    private readonly IPluginSandbox _sandbox;
    private readonly SecurityAuditLogger _auditLogger;

    public SecurityAwareEventManager(IPluginSandbox sandbox, SecurityAuditLogger auditLogger)
    {
        _handlers = new Dictionary<Type, List<ISecureEventHandler>>();
        _sandbox = sandbox;
        _auditLogger = auditLogger;
    }

    public void RegisterHandler<T>(ISecureEventHandler<T> handler) where T : class
    {
        var eventType = typeof(T);
        
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<ISecureEventHandler>();
        }
        
        _handlers[eventType].Add(handler);
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        var eventType = typeof(T);
        
        if (!_handlers.ContainsKey(eventType))
            return;

        var handlers = _handlers[eventType].Cast<ISecureEventHandler<T>>().ToList();
        
        await Task.WhenAll(handlers.Select(async handler =>
        {
            try
            {
                // Check if handler has required permissions
                if (!_sandbox.ValidatePermission(handler.RequiredPermissions))
                {
                    _auditLogger.LogPermissionDenied(handler.GetType().Name, handler.RequiredPermissions);
                    return;
                }

                // Execute handler within sandbox
                await _sandbox.ExecuteAsync(async () =>
                {
                    await handler.HandleAsync(eventData);
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                _auditLogger.LogEventHandlerError(handler.GetType().Name, ex);
            }
        }));
    }
}

public interface ISecureEventHandler
{
    PluginPermissions RequiredPermissions { get; }
}

public interface ISecureEventHandler<T> : ISecureEventHandler where T : class
{
    Task HandleAsync(T eventData);
}

// Example implementation
public class NetworkEventHandler : ISecureEventHandler<NetworkEvent>
{
    public PluginPermissions RequiredPermissions => PluginPermissions.NetworkAccess;

    public async Task HandleAsync(NetworkEvent eventData)
    {
        // Handle network event securely
        await ProcessNetworkEvent(eventData);
    }

    private async Task ProcessNetworkEvent(NetworkEvent eventData)
    {
        // Implementation
        await Task.Delay(100); // Simulate processing
    }
}
```

## Testing and Debugging

### Unit Testing Secure Plugins

Create comprehensive tests for your secure plugins:

```csharp
[TestClass]
public class SecurePluginTests
{
    private IPluginSandbox _sandbox;
    private PluginSecurityPolicy _policy;
    private MySecurePlugin _plugin;

    [TestInitialize]
    public async Task Setup()
    {
        _policy = PluginSecurityPolicy.CreateDefault();
        _sandbox = new PassThroughSandbox(); // Use pass-through for testing
        await _sandbox.InitializeAsync(_policy);
        
        _plugin = new MySecurePlugin();
        _plugin.SetSecurityContext(_sandbox, _policy);
    }

    [TestMethod]
    public async Task ProcessData_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var input = "test data";
        var expected = "Processed: test data";

        // Act
        var result = await _plugin.ProcessDataAsync(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public async Task ProcessData_WithNetworkPermission_AllowsNetworkAccess()
    {
        // Arrange
        _policy.DefaultPermissions = PluginPermissions.NetworkAccess;
        await _sandbox.InitializeAsync(_policy);

        // Act & Assert
        Assert.IsTrue(_sandbox.ValidatePermission(PluginPermissions.NetworkAccess));
    }

    [TestMethod]
    [ExpectedException(typeof(UnauthorizedAccessException))]
    public async Task ProcessData_WithoutNetworkPermission_ThrowsException()
    {
        // Arrange
        _policy.DefaultPermissions = PluginPermissions.ReadOnly;
        await _sandbox.InitializeAsync(_policy);

        // Act
        await _plugin.SendNetworkRequestAsync("http://example.com");

        // Should throw UnauthorizedAccessException
    }

    [TestMethod]
    public async Task ResourceUsage_DoesNotExceedLimits()
    {
        // Arrange
        _policy.MaxMemoryBytes = 50 * 1024 * 1024; // 50MB
        var monitor = new PluginResourceMonitor(_policy);

        // Act
        monitor.StartMonitoring();
        await _plugin.ProcessLargeDataAsync(CreateLargeDataStream());
        monitor.StopMonitoring();

        // Assert
        var usage = monitor.GetCurrentUsage();
        Assert.IsTrue(usage.MemoryUsed <= _policy.MaxMemoryBytes);
    }

    private Stream CreateLargeDataStream()
    {
        var data = new byte[10 * 1024 * 1024]; // 10MB
        return new MemoryStream(data);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _plugin?.Dispose();
        _sandbox?.Dispose();
    }
}
```

### Integration Testing

Test plugin integration with the Neo system:

```csharp
[TestClass]
public class PluginIntegrationTests
{
    private NeoSystem _neoSystem;
    private MySecurePlugin _plugin;

    [TestInitialize]
    public void Setup()
    {
        // Create test Neo system
        var settings = new ProtocolSettings
        {
            Network = 844378958u, // Test network
        };
        
        _neoSystem = new NeoSystem(settings, "test-data");
        
        // Load plugin with production security settings
        _plugin = new MySecurePlugin();
        _plugin.Initialize(_neoSystem);
    }

    [TestMethod]
    public async Task Plugin_IntegratesWithNeoSystem_SuccessfullyHandlesBlocks()
    {
        // Arrange
        var testBlock = CreateTestBlock();

        // Act
        var handled = await _plugin.HandleBlockAsync(testBlock);

        // Assert
        Assert.IsTrue(handled);
    }

    [TestMethod]
    public async Task Plugin_WithRestrictivePolicy_FailsGracefully()
    {
        // Arrange
        var restrictivePolicy = PluginSecurityPolicy.CreateRestrictive();
        _plugin.UpdateSecurityPolicy(restrictivePolicy);

        // Act
        var result = await _plugin.TryNetworkOperationAsync();

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage.Contains("permission"));
    }

    private Block CreateTestBlock()
    {
        // Create a test block for integration testing
        return new Block
        {
            // Block properties
        };
    }

    [TestCleanup]
    public void Cleanup()
    {
        _plugin?.Dispose();
        _neoSystem?.Dispose();
    }
}
```

### Debugging Security Issues

Use the built-in debugging features:

```csharp
public class DebugHelper
{
    public static void EnableDebugMode(IPluginSandbox sandbox)
    {
        if (sandbox is AssemblyLoadContextSandbox alcSandbox)
        {
            // Enable detailed monitoring
            var policy = alcSandbox.GetPolicy();
            policy.EnableDetailedMonitoring = true;
            
            // Add debug event handlers
            alcSandbox.PermissionDenied += OnPermissionDenied;
            alcSandbox.ResourceViolation += OnResourceViolation;
        }
    }

    private static void OnPermissionDenied(object sender, PermissionDeniedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            $"Permission denied: {e.Permission} for operation {e.Operation}");
    }

    private static void OnResourceViolation(object sender, ResourceViolationEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            $"Resource violation: {e.ViolationType} " +
            $"(Current: {e.CurrentValue}, Limit: {e.LimitValue})");
    }

    public static void DumpSecurityState(IPluginSandbox sandbox)
    {
        Console.WriteLine("=== Security State ===");
        Console.WriteLine($"Sandbox Type: {sandbox.Type}");
        Console.WriteLine($"Is Active: {sandbox.IsActive}");
        
        var usage = sandbox.GetResourceUsage();
        Console.WriteLine($"Memory Usage: {usage.MemoryUsed / 1024 / 1024}MB");
        Console.WriteLine($"Thread Count: {usage.ThreadsCreated}");
        Console.WriteLine($"Execution Time: {usage.ExecutionTime}ms");
        
        // Check all permissions
        foreach (PluginPermissions permission in Enum.GetValues<PluginPermissions>())
        {
            if (permission != PluginPermissions.None && permission != PluginPermissions.FullAccess)
            {
                var allowed = sandbox.ValidatePermission(permission);
                Console.WriteLine($"{permission}: {(allowed ? "ALLOWED" : "DENIED")}");
            }
        }
    }
}
```

## Performance Optimization

### 1. Minimize Sandbox Overhead

Reduce the performance impact of security measures:

```csharp
public class OptimizedPlugin : Plugin
{
    private readonly BatchProcessor _batchProcessor;
    private readonly PermissionCache _permissionCache;

    public OptimizedPlugin()
    {
        _batchProcessor = new BatchProcessor(batchSize: 100);
        _permissionCache = new PermissionCache(TimeSpan.FromMinutes(5));
    }

    // Batch operations to reduce sandbox overhead
    public async Task<List<T>> ProcessItemsBatchAsync<T>(IEnumerable<T> items, Func<T, T> processor)
    {
        var batches = _batchProcessor.CreateBatches(items);
        var results = new List<T>();

        foreach (var batch in batches)
        {
            var batchResult = await ExecuteSecurely(async () =>
            {
                return await Task.Run(() => batch.Select(processor).ToList());
            });

            results.AddRange(batchResult);
        }

        return results;
    }

    // Cache permission checks to avoid repeated validation
    public bool ValidatePermissionCached(PluginPermissions permission)
    {
        return _permissionCache.GetOrCompute(permission, () => _sandbox.ValidatePermission(permission));
    }

    // Use async patterns efficiently
    public async Task ProcessConcurrentlyAsync<T>(IEnumerable<T> items, Func<T, Task> processor)
    {
        var semaphore = new SemaphoreSlim(_policy.MaxThreads / 2);
        
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                await processor(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}

// Helper classes
public class BatchProcessor
{
    private readonly int _batchSize;

    public BatchProcessor(int batchSize)
    {
        _batchSize = batchSize;
    }

    public IEnumerable<IEnumerable<T>> CreateBatches<T>(IEnumerable<T> items)
    {
        return items.Chunk(_batchSize);
    }
}

public class PermissionCache
{
    private readonly ConcurrentDictionary<PluginPermissions, (bool Result, DateTime Expiry)> _cache;
    private readonly TimeSpan _cacheTimeout;

    public PermissionCache(TimeSpan cacheTimeout)
    {
        _cache = new ConcurrentDictionary<PluginPermissions, (bool, DateTime)>();
        _cacheTimeout = cacheTimeout;
    }

    public bool GetOrCompute(PluginPermissions permission, Func<bool> computer)
    {
        var now = DateTime.UtcNow;
        
        if (_cache.TryGetValue(permission, out var cached) && cached.Expiry > now)
        {
            return cached.Result;
        }

        var result = computer();
        _cache[permission] = (result, now.Add(_cacheTimeout));
        return result;
    }
}
```

### 2. Efficient Resource Usage

Optimize for the constrained sandbox environment:

```csharp
public class ResourceOptimizedPlugin : Plugin
{
    private readonly ObjectPool<byte[]> _bufferPool;
    private readonly StringBuilderCache _stringBuilderCache;

    public ResourceOptimizedPlugin()
    {
        _bufferPool = new DefaultObjectPool<byte[]>(
            new BufferPooledObjectPolicy(bufferSize: 8192));
        _stringBuilderCache = new StringBuilderCache();
    }

    // Use object pooling to reduce allocations
    public async Task<string> ProcessStreamAsync(Stream stream)
    {
        var buffer = _bufferPool.Get();
        var stringBuilder = _stringBuilderCache.Acquire();

        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                stringBuilder.Append(text);
            }

            return stringBuilder.ToString();
        }
        finally
        {
            _bufferPool.Return(buffer);
            _stringBuilderCache.Release(stringBuilder);
        }
    }

    // Use stackalloc for small, short-lived arrays
    public string ProcessSmallData(ReadOnlySpan<byte> data)
    {
        Span<char> charBuffer = stackalloc char[data.Length];
        
        for (int i = 0; i < data.Length; i++)
        {
            charBuffer[i] = (char)data[i];
        }

        return new string(charBuffer);
    }

    // Implement efficient cleanup
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stringBuilderCache?.Dispose();
            // Note: ObjectPool doesn't need explicit disposal
        }
        base.Dispose(disposing);
    }
}

// Helper classes
public class BufferPooledObjectPolicy : PooledObjectPolicy<byte[]>
{
    private readonly int _bufferSize;

    public BufferPooledObjectPolicy(int bufferSize)
    {
        _bufferSize = bufferSize;
    }

    public override byte[] Create() => new byte[_bufferSize];

    public override bool Return(byte[] obj)
    {
        Array.Clear(obj, 0, obj.Length);
        return true;
    }
}

public class StringBuilderCache : IDisposable
{
    private readonly ConcurrentQueue<StringBuilder> _cache = new();
    private const int MaxCacheSize = 10;

    public StringBuilder Acquire()
    {
        if (_cache.TryDequeue(out var stringBuilder))
        {
            return stringBuilder;
        }

        return new StringBuilder();
    }

    public void Release(StringBuilder stringBuilder)
    {
        if (_cache.Count < MaxCacheSize)
        {
            stringBuilder.Clear();
            _cache.Enqueue(stringBuilder);
        }
    }

    public void Dispose()
    {
        while (_cache.TryDequeue(out _))
        {
            // Clear the cache
        }
    }
}
```

## Migration Guide

### Migrating Existing Plugins

Convert existing plugins to use the sandbox system:

#### Before (Unsecure Plugin)

```csharp
public class OldPlugin : Plugin
{
    public override void Configure()
    {
        // Direct system access
        Settings.Load(GetConfiguration());
    }

    public async Task ProcessDataAsync(string data)
    {
        // Direct network access
        using var client = new HttpClient();
        await client.PostAsync("http://api.example.com", new StringContent(data));
        
        // Direct file access
        await File.WriteAllTextAsync("/tmp/output.txt", data);
    }
}
```

#### After (Secure Plugin)

```csharp
public class NewSecurePlugin : Plugin
{
    private IPluginSandbox _sandbox;
    private PluginSecurityPolicy _policy;

    public override void Configure()
    {
        Settings.Load(GetConfiguration());
        InitializeSecurity();
    }

    private async void InitializeSecurity()
    {
        _policy = new PluginSecurityPolicy
        {
            DefaultPermissions = PluginPermissions.NetworkAccess | PluginPermissions.FileSystemAccess,
            MaxMemoryBytes = 128 * 1024 * 1024,
            FileSystemAccess = new FileSystemAccessPolicy
            {
                AllowFileSystemAccess = true,
                AllowedPaths = new List<string> { "/tmp" }
            }
        };

        _sandbox = new AssemblyLoadContextSandbox();
        await _sandbox.InitializeAsync(_policy);
    }

    public async Task ProcessDataAsync(string data)
    {
        await ExecuteSecurely(async () =>
        {
            // Check permissions before operations
            if (_sandbox.ValidatePermission(PluginPermissions.NetworkAccess))
            {
                using var client = new HttpClient();
                await client.PostAsync("http://api.example.com", new StringContent(data));
            }

            if (_sandbox.ValidatePermission(PluginPermissions.FileSystemAccess))
            {
                await File.WriteAllTextAsync("/tmp/output.txt", data);
            }
        });
    }

    private async Task ExecuteSecurely(Func<Task> action)
    {
        var result = await _sandbox.ExecuteAsync(async () =>
        {
            await action();
            return Task.CompletedTask;
        });

        if (!result.Success)
        {
            throw result.Exception;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sandbox?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

### Phased Migration Strategy

1. **Phase 1: Assessment**
   - Analyze existing plugins for security requirements
   - Identify required permissions for each plugin
   - Plan resource usage limits

2. **Phase 2: Preparation**
   - Add sandbox infrastructure to plugins
   - Implement permission checking without enforcement
   - Add resource monitoring

3. **Phase 3: Implementation**
   - Enable sandbox enforcement in development
   - Test with various security policies
   - Implement fallback mechanisms

4. **Phase 4: Deployment**
   - Deploy with permissive policies initially
   - Gradually tighten security constraints
   - Monitor and adjust based on real usage

---

**Last Updated**: December 2024  
**Version**: 1.0  
**Compatibility**: Neo N3, .NET 9.0