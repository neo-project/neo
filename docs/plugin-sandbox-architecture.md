# Neo Plugin Sandbox Security Architecture

## Table of Contents

- [Overview](#overview)
- [Architecture Design](#architecture-design)
- [Sandbox Types](#sandbox-types)
- [Security Model](#security-model)
- [Resource Management](#resource-management)
- [Cross-Platform Support](#cross-platform-support)
- [Performance Optimization](#performance-optimization)
- [Configuration Examples](#configuration-examples)
- [Architecture Diagrams](#architecture-diagrams)

## Overview

The Neo Plugin Sandbox System provides comprehensive security isolation for third-party plugins while maintaining high performance and cross-platform compatibility. The architecture implements a multi-tiered approach with four distinct isolation levels, each designed for specific trust requirements and performance characteristics.

### Core Principles

- **Defense in Depth**: Multiple layers of security controls
- **Principle of Least Privilege**: Minimal permissions by default
- **Performance First**: Optimized for production workloads
- **Cross-Platform**: Consistent behavior across operating systems
- **Extensible Design**: Modular architecture for future enhancements

### Key Components

```
┌─────────────────────────────────────────────┐
│              Neo Node Core                  │
├─────────────────────────────────────────────┤
│           Plugin Security Manager           │
├─────────────────────────────────────────────┤
│  ┌─────────────┐ ┌─────────────────────────┐ │
│  │   Policy    │ │     Resource Monitor    │ │
│  │  Manager    │ │        System           │ │
│  └─────────────┘ └─────────────────────────┘ │
├─────────────────────────────────────────────┤
│              Sandbox Layer                  │
│  ┌─────────┐ ┌──────────┐ ┌──────────────┐  │
│  │ PassThru│ │   ALC    │ │   Process    │  │
│  │         │ │ Sandbox  │ │   Sandbox    │  │
│  └─────────┘ └──────────┘ └──────────────┘  │
│                             ┌──────────────┐ │
│                             │  Container   │ │
│                             │   Sandbox    │ │
│                             └──────────────┘ │
└─────────────────────────────────────────────┘
```

## Architecture Design

### Component Architecture

The Neo Plugin Sandbox System consists of several key components working together to provide secure plugin execution:

#### 1. Plugin Security Manager

Central orchestrator responsible for:
- Plugin lifecycle management
- Security policy enforcement
- Sandbox selection and initialization
- Cross-component coordination

```csharp
// Core interface defining security management
public interface IPluginSecurityManager
{
    Task<IPluginSandbox> CreateSandboxAsync(string pluginId, PluginSecurityPolicy policy);
    bool ValidatePlugin(string pluginPath);
    void EnforcePolicy(string pluginId, PluginSecurityPolicy policy);
    ResourceUsage GetResourceUsage(string pluginId);
}
```

#### 2. Security Policy Engine

Defines and enforces security constraints:
- Permission validation
- Resource limit enforcement
- Violation handling
- Policy inheritance and composition

#### 3. Resource Monitoring System

Real-time tracking of plugin resource consumption:
- Memory usage monitoring
- CPU utilization tracking
- Thread count management
- Network connection monitoring
- File system access auditing

#### 4. Audit and Logging Framework

Comprehensive security event tracking:
- Security violation logging
- Performance metrics collection
- Compliance reporting
- Forensic analysis support

### Security Boundaries

The system implements multiple security boundaries to contain potentially malicious plugins:

```
┌──────────────────────────────────────────────────┐
│                 Host System                      │
│  ┌────────────────────────────────────────────┐  │
│  │             Neo Node Process               │  │
│  │  ┌──────────────────────────────────────┐  │  │
│  │  │        Plugin Sandbox Container      │  │  │
│  │  │  ┌────────────────────────────────┐  │  │  │
│  │  │  │      Plugin Assembly          │  │  │  │
│  │  │  │    (Isolated Execution)       │  │  │  │
│  │  │  └────────────────────────────────┘  │  │  │
│  │  └──────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘

Boundary 1: Assembly-level isolation (ALC)
Boundary 2: Process-level isolation (Process Sandbox)
Boundary 3: Container-level isolation (Container Sandbox)
Boundary 4: Host system protection
```

## Sandbox Types

The Neo Plugin Sandbox System provides four distinct sandbox types, each offering different levels of isolation and performance characteristics:

### 1. PassThrough Sandbox

**Security Level**: None
**Performance**: Highest
**Use Case**: Development and testing environments

```csharp
public class PassThroughSandbox : IPluginSandbox
{
    public SandboxType Type => SandboxType.PassThrough;
    public bool IsActive => true;
    
    // Direct execution without isolation
    public async Task<SandboxResult> ExecuteAsync(Func<object> action)
    {
        try
        {
            var result = action();
            return new SandboxResult 
            { 
                Success = true, 
                Result = result 
            };
        }
        catch (Exception ex)
        {
            return new SandboxResult 
            { 
                Success = false, 
                Exception = ex 
            };
        }
    }
}
```

**Features**:
- No security overhead
- Direct access to all system resources
- Minimal latency
- Ideal for trusted development scenarios

### 2. AssemblyLoadContext Sandbox

**Security Level**: Medium
**Performance**: High
**Use Case**: Trusted plugins with moderate isolation needs

The AssemblyLoadContext (ALC) sandbox provides application-domain-level isolation using .NET's modern isolation mechanisms:

```csharp
public class AssemblyLoadContextSandbox : IPluginSandbox
{
    private readonly PluginAssemblyLoadContext _loadContext;
    private readonly PermissionSet _permissions;
    
    public async Task InitializeAsync(PluginSecurityPolicy policy)
    {
        _loadContext = new PluginAssemblyLoadContext(
            policy.RequireSignedPlugins);
        _permissions = CreatePermissionSet(policy);
    }
    
    public async Task<SandboxResult> ExecuteAsync(Func<object> action)
    {
        return await Task.Run(() =>
        {
            using var scope = new PermissionScope(_permissions);
            return ExecuteWithinContext(action);
        });
    }
}
```

**Features**:
- Assembly-level isolation
- Controlled type loading
- Code Access Security (CAS) enforcement
- Resource access limitation
- Low performance overhead

### 3. Process Sandbox

**Security Level**: High
**Performance**: Medium
**Use Case**: Standard plugins requiring strong isolation

Process-level isolation provides robust security by executing plugins in separate operating system processes:

```csharp
public class ProcessSandbox : IPluginSandbox
{
    private Process _pluginProcess;
    private readonly ProcessResourceMonitor _monitor;
    
    public async Task InitializeAsync(PluginSecurityPolicy policy)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "Neo.PluginHost.exe",
            Arguments = $"--sandbox --policy={policy.ToJson()}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        ApplyProcessRestrictions(startInfo, policy);
        _pluginProcess = Process.Start(startInfo);
        _monitor = new ProcessResourceMonitor(_pluginProcess, policy);
    }
}
```

**Features**:
- Complete process isolation
- OS-level resource limits
- Crash isolation
- Inter-process communication via pipes/sockets
- Platform-specific optimizations

### 4. Container Sandbox

**Security Level**: Maximum
**Performance**: Lower
**Use Case**: Untrusted plugins requiring maximum isolation

Container-based isolation provides the highest level of security using Docker/OCI containers:

```csharp
public class ContainerSandbox : IPluginSandbox
{
    private readonly DockerClient _dockerClient;
    private string _containerId;
    
    public async Task InitializeAsync(PluginSecurityPolicy policy)
    {
        var containerConfig = new ContainerConfig
        {
            Image = "neo-plugin-runtime:latest",
            Env = policy.ToEnvironmentVariables(),
            HostConfig = new HostConfig
            {
                Memory = policy.MaxMemoryBytes,
                CpuShares = CalculateCpuShares(policy.MaxCpuPercent),
                NetworkMode = policy.NetworkAccess.AllowNetworkAccess ? "bridge" : "none"
            }
        };
        
        var response = await _dockerClient.Containers.CreateContainerAsync(containerConfig);
        _containerId = response.ID;
        await _dockerClient.Containers.StartContainerAsync(_containerId, new ContainerStartParameters());
    }
}
```

**Features**:
- Complete system isolation
- Resource limits enforced by container runtime
- Network isolation
- File system isolation
- Image-based deployment

## Security Model

### Permission System

The Neo Plugin Sandbox implements a comprehensive permission model with 18 distinct permission types:

```csharp
[Flags]
public enum PluginPermissions : uint
{
    None = 0,
    ReadOnly = 1,                    // Basic blockchain read access
    StorageAccess = 2,               // Blockchain write operations
    NetworkAccess = 4,               // Network communication
    FileSystemAccess = 8,            // File system operations
    RpcPlugin = 16,                  // RPC server functionality
    SystemAccess = 32,               // Advanced system operations
    CryptographicAccess = 64,        // Cryptographic operations
    ProcessAccess = 128,             // Process management
    RegistryAccess = 256,            // Windows registry access
    ServiceAccess = 512,             // Service management
    DatabaseAccess = 1024,           // Database operations
    ConsensusAccess = 2048,          // Consensus participation
    WalletAccess = 4096,             // Wallet operations
    OracleAccess = 8192,             // Oracle service access
    StateAccess = 16384,             // State service access
    LogAccess = 32768,               // Application logs access
    DebuggingAccess = 65536,         // Memory debugging/profiling
    HttpsOnly = 131072,              // HTTPS network access only
    AdminAccess = 262144,            // Administrative operations
    PluginLoaderAccess = 524288,     // Plugin loading capability
    
    // Predefined permission sets
    NetworkPlugin = ReadOnly | NetworkAccess,
    ServicePlugin = ReadOnly | StorageAccess | NetworkAccess,
    AdminPlugin = ReadOnly | StorageAccess | NetworkAccess | FileSystemAccess | 
                  RpcPlugin | CryptographicAccess | DatabaseAccess | LogAccess,
    FullAccess = 0xFFFFFFFF
}
```

### Policy Hierarchy

Security policies follow a hierarchical structure allowing for inheritance and specialization:

```
Global Policy
├── Default Policy
├── Restrictive Policy
└── Permissive Policy
    └── Plugin-Specific Overrides
        ├── RpcServer → Permissive
        ├── OracleService → Default
        └── UntrustedPlugin → Restrictive
```

### Permission Validation Flow

```
Plugin Request → Permission Check → Policy Lookup → Access Decision
     ↓              ↓                 ↓              ↓
   Network         Check if          Load policy    Allow/Deny
   Access          NetworkAccess     for plugin     + Audit Log
                   is granted        from config
```

## Resource Management

### Memory Management

The system implements sophisticated memory tracking and limiting:

```csharp
public class MemoryResourceMonitor
{
    private readonly long _maxMemoryBytes;
    private readonly MemoryMappedFile _sharedCounter;
    
    public bool CheckMemoryLimit(long requestedBytes)
    {
        var currentUsage = GetCurrentMemoryUsage();
        var newUsage = currentUsage + requestedBytes;
        
        if (newUsage > _maxMemoryBytes)
        {
            LogViolation(MemoryViolation, currentUsage, requestedBytes);
            return false;
        }
        
        return true;
    }
    
    private long GetCurrentMemoryUsage()
    {
        return GC.GetTotalMemory(false) + GetUnmanagedMemoryUsage();
    }
}
```

**Memory Limits by Sandbox Type**:
- **PassThrough**: No limits (host process limits apply)
- **AssemblyLoadContext**: Application domain memory tracking
- **Process**: OS process memory limits
- **Container**: Container runtime memory cgroups

### CPU Monitoring

CPU usage monitoring uses platform-specific mechanisms:

**Windows**: Performance counters and job objects
**Linux**: cgroups and /proc/stat monitoring
**macOS**: mach task info and BSD process information

```csharp
public class CpuResourceMonitor
{
    private readonly double _maxCpuPercent;
    private readonly Timer _monitoringTimer;
    
    public void StartMonitoring()
    {
        _monitoringTimer = new Timer(CheckCpuUsage, null, 
            TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }
    
    private void CheckCpuUsage(object state)
    {
        var usage = GetCpuUsagePercent();
        if (usage > _maxCpuPercent)
        {
            HandleCpuViolation(usage);
        }
    }
}
```

### Thread Management

Thread limiting prevents resource exhaustion attacks:

```csharp
public class ThreadResourceMonitor
{
    private readonly int _maxThreads;
    private readonly ConcurrentDictionary<int, ThreadInfo> _trackedThreads;
    
    public bool CanCreateThread()
    {
        var currentCount = _trackedThreads.Count;
        return currentCount < _maxThreads;
    }
    
    public void RegisterThread(int threadId)
    {
        _trackedThreads.TryAdd(threadId, new ThreadInfo
        {
            Id = threadId,
            CreatedAt = DateTime.UtcNow,
            StackTrace = Environment.StackTrace
        });
    }
}
```

## Cross-Platform Support

### Platform Detection and Adaptation

The system automatically adapts to different operating systems:

```csharp
public class CrossPlatformSandbox
{
    public static IPluginSandbox CreateOptimalSandbox(PluginSecurityPolicy policy)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsOptimizedSandbox(policy);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxOptimizedSandbox(policy);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOSOptimizedSandbox(policy);
        }
        
        return new GenericSandbox(policy);
    }
}
```

### Platform-Specific Features

**Windows**:
- Job objects for resource limiting
- Windows security descriptors
- Registry access control
- Windows service integration

**Linux**:
- cgroups for resource control
- seccomp system call filtering
- Linux capabilities
- systemd integration

**macOS**:
- sandbox-exec for process sandboxing
- BSD jail-like restrictions
- mach port communication
- launchd integration

## Performance Optimization

### Caching Strategies

Multiple levels of caching optimize performance:

```csharp
public class PerformanceOptimizedSandbox : IPluginSandbox
{
    private readonly IPermissionCacheManager _permissionCache;
    private readonly ResourceCache _resourceCache;
    
    public bool ValidatePermission(PluginPermissions permission)
    {
        // Level 1: In-memory permission cache
        if (_permissionCache.TryGetCachedResult(permission, out bool result))
        {
            return result;
        }
        
        // Level 2: Policy evaluation
        result = EvaluatePermission(permission);
        _permissionCache.CacheResult(permission, result);
        
        return result;
    }
}
```

### Resource Pooling

Pre-allocated resource pools reduce allocation overhead:

```csharp
public class SandboxResourcePool
{
    private readonly ObjectPool<AssemblyLoadContextSandbox> _alcPool;
    private readonly ObjectPool<ProcessSandbox> _processPool;
    
    public IPluginSandbox GetSandbox(SandboxType type)
    {
        return type switch
        {
            SandboxType.AssemblyLoadContext => _alcPool.Get(),
            SandboxType.Process => _processPool.Get(),
            _ => new PassThroughSandbox()
        };
    }
}
```

### Monitoring Optimization

Different monitoring profiles balance security and performance:

```json
{
  "MonitoringProfiles": {
    "HighPerformance": {
      "CheckInterval": 10000,
      "CacheSize": 1000,
      "DetailedLogging": false
    },
    "Balanced": {
      "CheckInterval": 5000,
      "CacheSize": 500,
      "DetailedLogging": true
    },
    "MaxSecurity": {
      "CheckInterval": 1000,
      "CacheSize": 100,
      "DetailedLogging": true
    }
  }
}
```

## Configuration Examples

### Production Configuration

```json
{
  "PluginSecurity": {
    "Enabled": true,
    "DefaultPolicy": "Production",
    "Policies": {
      "Production": {
        "DefaultPermissions": "ReadOnly",
        "MaxPermissions": "ServicePlugin",
        "MaxMemoryBytes": 268435456,
        "MaxCpuPercent": 25.0,
        "MaxThreads": 10,
        "SandboxType": "AssemblyLoadContext",
        "StrictMode": true,
        "RequireSignedPlugins": true
      }
    },
    "GlobalSettings": {
      "PerformanceProfile": "Balanced",
      "AutoSelectSandboxType": true
    }
  }
}
```

### Development Configuration

```json
{
  "PluginSecurity": {
    "Enabled": true,
    "DefaultPolicy": "Development",
    "Policies": {
      "Development": {
        "DefaultPermissions": "AdminPlugin",
        "MaxPermissions": "FullAccess",
        "SandboxType": "PassThrough",
        "StrictMode": false,
        "RequireSignedPlugins": false
      }
    },
    "GlobalSettings": {
      "TestModeEnabled": true,
      "PerformanceProfile": "HighPerformance"
    }
  }
}
```

## Architecture Diagrams

### High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Neo Node Core Engine                     │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                Plugin Security Manager                      │
│  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────┐ │
│  │ Policy Engine   │ │ Resource Monitor│ │ Audit Logger  │ │
│  └─────────────────┘ └─────────────────┘ └───────────────┘ │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                  Sandbox Factory                            │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌─────────────────┐│
│  │PassThru  │ │   ALC    │ │ Process  │ │   Container     ││
│  │ Sandbox  │ │ Sandbox  │ │ Sandbox  │ │   Sandbox       ││
│  └──────────┘ └──────────┘ └──────────┘ └─────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Plugin Execution Flow

```
Plugin Load Request
         │
         ▼
   Security Policy
     Resolution
         │
         ▼
   Sandbox Selection
    & Initialization
         │
         ▼
    Permission
     Validation
         │
         ▼
   Resource Limit
      Enforcement
         │
         ▼
   Plugin Execution
    (Within Sandbox)
         │
         ▼
    Result Return
   & Cleanup
```

### Multi-Tier Security Model

```
┌─────────────────────────────────────────────────┐ ← Security Tier 4
│              Host OS Security                   │   (Host Protection)
├─────────────────────────────────────────────────┤
│                Neo Node Process                 │ ← Security Tier 3
│  ┌───────────────────────────────────────────┐  │   (Process Isolation)
│  │            Plugin Sandbox                 │  │
│  │  ┌─────────────────────────────────────┐  │  │ ← Security Tier 2
│  │  │        Permission Boundary          │  │  │   (Permission Control)
│  │  │  ┌───────────────────────────────┐  │  │  │
│  │  │  │      Plugin Assembly          │  │  │  │ ← Security Tier 1
│  │  │  │    (Controlled Execution)     │  │  │  │   (Code Isolation)
│  │  │  └───────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

---

**Last Updated**: December 2024  
**Version**: 1.0  
**Compatibility**: Neo N3, .NET 9.0