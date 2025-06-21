# Neo Plugin Sandbox Configuration Guide

## Table of Contents

- [Overview](#overview)
- [Configuration Structure](#configuration-structure)
- [Security Policies](#security-policies)
- [Environment Configurations](#environment-configurations)
- [Platform-Specific Settings](#platform-specific-settings)
- [Performance Tuning](#performance-tuning)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

## Overview

This guide provides comprehensive configuration instructions for the Neo Plugin Sandbox Security System. Node operators can use these configurations to establish appropriate security policies based on their trust requirements, performance needs, and operational environment.

### Quick Start

1. **Copy the base configuration** from `/docs/security-config-template.json`
2. **Choose your security level**: Restrictive, Default, or Permissive
3. **Customize for your environment**: Adjust paths, limits, and network settings
4. **Test the configuration**: Enable test mode and validate plugin behavior
5. **Deploy to production**: Disable test mode and enable monitoring

## Configuration Structure

The Neo Plugin Sandbox configuration follows a hierarchical structure within the main Neo configuration file:

```json
{
  "ApplicationConfiguration": {
    "PluginSecurity": {
      "Enabled": true,
      "DefaultPolicy": "PolicyName",
      "Policies": { /* Policy definitions */ },
      "PluginSpecificPolicies": { /* Plugin overrides */ },
      "AuditLogging": { /* Logging configuration */ },
      "GlobalSettings": { /* System-wide settings */ }
    }
  }
}
```

### Core Configuration Elements

| Element | Description | Required |
|---------|-------------|----------|
| `Enabled` | Enable/disable the security system | Yes |
| `DefaultPolicy` | Name of the default policy to apply | Yes |
| `Policies` | Security policy definitions | Yes |
| `PluginSpecificPolicies` | Per-plugin policy overrides | No |
| `AuditLogging` | Security event logging configuration | No |
| `GlobalSettings` | System-wide performance and behavior settings | No |

## Security Policies

### Policy Structure

Each security policy defines a complete set of constraints and permissions:

```json
{
  "PolicyName": {
    "DefaultPermissions": "ReadOnly",
    "MaxPermissions": "NetworkPlugin", 
    "MaxMemoryBytes": 268435456,
    "MaxCpuPercent": 25.0,
    "MaxThreads": 10,
    "MaxExecutionTimeSeconds": 300,
    "ViolationAction": "Suspend",
    "EnableDetailedMonitoring": true,
    "StrictMode": true,
    "RequireSignedPlugins": true,
    "SandboxType": "AssemblyLoadContext",
    "FileSystemAccess": { /* File system policy */ },
    "NetworkAccess": { /* Network policy */ },
    "ResourceMonitoring": { /* Monitoring policy */ }
  }
}
```

### Permission Levels

Available permission values (can be combined with bitwise OR):

| Permission | Description | Risk Level |
|------------|-------------|------------|
| `None` | No permissions | None |
| `ReadOnly` | Blockchain read access only | Low |
| `NetworkAccess` | Network communication | Medium |
| `StorageAccess` | Blockchain write operations | Medium |
| `FileSystemAccess` | File system operations | High |
| `SystemAccess` | Advanced system operations | Very High |
| `AdminAccess` | Administrative operations | Critical |
| `FullAccess` | All permissions (dangerous) | Critical |

**Predefined Permission Sets**:
- `NetworkPlugin`: `ReadOnly | NetworkAccess`
- `ServicePlugin`: `ReadOnly | StorageAccess | NetworkAccess`  
- `AdminPlugin`: Most permissions except dangerous ones

### Sandbox Types

| Type | Security | Performance | Use Case |
|------|----------|-------------|----------|
| `PassThrough` | None | Highest | Development/Testing |
| `AssemblyLoadContext` | Medium | High | Trusted plugins |
| `Process` | High | Medium | Standard plugins |
| `Container` | Maximum | Lower | Untrusted plugins |

### Violation Actions

| Action | Description | When to Use |
|--------|-------------|-------------|
| `Log` | Log violation but continue | Development, trusted plugins |
| `Suspend` | Temporarily suspend plugin | Production, recoverable violations |
| `Terminate` | Immediately stop plugin | High security, critical violations |

## Environment Configurations

### Production Environment

Balanced security and performance for production Neo nodes:

```json
{
  "ApplicationConfiguration": {
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
          "MaxExecutionTimeSeconds": 300,
          "ViolationAction": "Suspend",
          "EnableDetailedMonitoring": true,
          "StrictMode": true,
          "RequireSignedPlugins": true,
          "SandboxType": "AssemblyLoadContext",
          "FileSystemAccess": {
            "AllowFileSystemAccess": false,
            "AllowedPaths": ["/opt/neo/plugins/data"],
            "RestrictedPaths": ["/etc", "/boot", "/sys", "/proc", "/root"],
            "MaxFileSize": 104857600,
            "MaxTotalFiles": 1000
          },
          "NetworkAccess": {
            "AllowNetworkAccess": true,
            "AllowedEndpoints": ["seed1.neo.org", "seed2.neo.org"],
            "BlockedEndpoints": ["localhost:22", "localhost:3389"],
            "AllowedPorts": [80, 443, 10332, 10333, 20332, 20333],
            "MaxConnections": 10,
            "RequireSSL": true
          },
          "ResourceMonitoring": {
            "EnableMonitoring": true,
            "CheckInterval": 5000,
            "MemoryWarningThreshold": 0.8,
            "CpuWarningThreshold": 0.7
          }
        }
      },
      "PluginSpecificPolicies": {
        "RpcServer": "Production",
        "LevelDBStore": "Production",
        "OracleService": "Production"
      },
      "AuditLogging": {
        "Enabled": true,
        "LogLevel": "Info",
        "LogFile": "/var/log/neo/security-audit.log",
        "MaxLogFiles": 30,
        "MaxLogFileSize": 52428800,
        "EventHandlers": ["FileHandler", "SyslogHandler"]
      },
      "GlobalSettings": {
        "PerformanceProfile": "Balanced",
        "CrossPlatformOptimization": true,
        "AutoSelectSandboxType": true
      }
    }
  }
}
```

### Development Environment

Permissive settings for plugin development and testing:

```json
{
  "ApplicationConfiguration": {
    "PluginSecurity": {
      "Enabled": true,
      "DefaultPolicy": "Development",
      "Policies": {
        "Development": {
          "DefaultPermissions": "AdminPlugin",
          "MaxPermissions": "FullAccess",
          "MaxMemoryBytes": 1073741824,
          "MaxCpuPercent": 75.0,
          "MaxThreads": 50,
          "MaxExecutionTimeSeconds": 1800,
          "ViolationAction": "Log",
          "EnableDetailedMonitoring": false,
          "StrictMode": false,
          "RequireSignedPlugins": false,
          "SandboxType": "PassThrough",
          "FileSystemAccess": {
            "AllowFileSystemAccess": true,
            "AllowedPaths": ["/home/developer", "/tmp", "/var/tmp"],
            "RestrictedPaths": ["/etc/passwd", "/etc/shadow"],
            "MaxFileSize": 1073741824,
            "MaxTotalFiles": 10000
          },
          "NetworkAccess": {
            "AllowNetworkAccess": true,
            "AllowedEndpoints": [],
            "BlockedEndpoints": [],
            "AllowedPorts": [],
            "MaxConnections": 100,
            "RequireSSL": false
          },
          "ResourceMonitoring": {
            "EnableMonitoring": true,
            "CheckInterval": 30000,
            "MemoryWarningThreshold": 0.9,
            "CpuWarningThreshold": 0.8
          }
        }
      },
      "GlobalSettings": {
        "TestModeEnabled": true,
        "PerformanceProfile": "Development",
        "CrossPlatformOptimization": false
      }
    }
  }
}
```

### High-Security Environment

Maximum security for handling untrusted plugins:

```json
{
  "ApplicationConfiguration": {
    "PluginSecurity": {
      "Enabled": true,
      "DefaultPolicy": "HighSecurity",
      "Policies": {
        "HighSecurity": {
          "DefaultPermissions": "ReadOnly",
          "MaxPermissions": "ReadOnly",
          "MaxMemoryBytes": 67108864,
          "MaxCpuPercent": 10.0,
          "MaxThreads": 3,
          "MaxExecutionTimeSeconds": 60,
          "ViolationAction": "Terminate",
          "EnableDetailedMonitoring": true,
          "StrictMode": true,
          "RequireSignedPlugins": true,
          "SandboxType": "Container",
          "FileSystemAccess": {
            "AllowFileSystemAccess": false,
            "AllowedPaths": [],
            "RestrictedPaths": ["/", "C:\\"],
            "MaxFileSize": 1048576,
            "MaxTotalFiles": 10
          },
          "NetworkAccess": {
            "AllowNetworkAccess": false,
            "AllowedEndpoints": [],
            "BlockedEndpoints": ["*"],
            "AllowedPorts": [],
            "MaxConnections": 0,
            "RequireSSL": true
          },
          "ResourceMonitoring": {
            "EnableMonitoring": true,
            "CheckInterval": 1000,
            "MemoryWarningThreshold": 0.9,
            "CpuWarningThreshold": 0.8
          }
        }
      },
      "AuditLogging": {
        "Enabled": true,
        "LogLevel": "Debug",
        "LogFile": "/var/log/neo/security-audit.log",
        "MaxLogFiles": 100,
        "MaxLogFileSize": 10485760,
        "EventHandlers": ["FileHandler", "SyslogHandler", "EmailHandler"]
      }
    }
  }
}
```

## Platform-Specific Settings

### Windows Configuration

```json
{
  "ApplicationConfiguration": {
    "PluginSecurity": {
      "Enabled": true,
      "DefaultPolicy": "WindowsProduction",
      "Policies": {
        "WindowsProduction": {
          "DefaultPermissions": "NetworkPlugin",
          "MaxPermissions": "ServicePlugin",
          "SandboxType": "Process",
          "FileSystemAccess": {
            "AllowFileSystemAccess": true,
            "AllowedPaths": [
              "C:\\ProgramData\\Neo\\Plugins",
              "C:\\Users\\Neo\\AppData\\Local\\Neo"
            ],
            "RestrictedPaths": [
              "C:\\Windows",
              "C:\\Program Files",
              "C:\\Users\\*\\Documents"
            ]
          },
          "NetworkAccess": {
            "AllowNetworkAccess": true,
            "BlockedEndpoints": [
              "localhost:135",
              "localhost:445",
              "localhost:3389"
            ],
            "RequireSSL": true
          }
        }
      },
      "AuditLogging": {
        "LogFile": "C:\\ProgramData\\Neo\\Logs\\security-audit.log",
        "EventHandlers": ["FileHandler", "WindowsEventLogHandler"]
      }
    }
  }
}
```

### Linux Configuration

```json
{
  "ApplicationConfiguration": {
    "PluginSecurity": {
      "Enabled": true,
      "DefaultPolicy": "LinuxProduction", 
      "Policies": {
        "LinuxProduction": {
          "DefaultPermissions": "NetworkPlugin",
          "MaxPermissions": "ServicePlugin",
          "SandboxType": "Process",
          "FileSystemAccess": {
            "AllowFileSystemAccess": true,
            "AllowedPaths": [
              "/opt/neo/plugins",
              "/var/lib/neo",
              "/tmp/neo-temp"
            ],
            "RestrictedPaths": [
              "/etc",
              "/boot", 
              "/sys",
              "/proc",
              "/root",
              "/home/*/.ssh"
            ]
          },
          "NetworkAccess": {
            "AllowNetworkAccess": true,
            "BlockedEndpoints": [
              "localhost:22",
              "localhost:25",
              "localhost:110",
              "localhost:143"
            ]
          }
        }
      },
      "AuditLogging": {
        "LogFile": "/var/log/neo/security-audit.log",
        "EventHandlers": ["FileHandler", "SyslogHandler"]
      }
    }
  }
}
```

### Docker Container Configuration

For Neo nodes running in containers:

```json
{
  "ApplicationConfiguration": {
    "PluginSecurity": {
      "Enabled": true,
      "DefaultPolicy": "ContainerizedNode",
      "Policies": {
        "ContainerizedNode": {
          "DefaultPermissions": "ServicePlugin",
          "MaxPermissions": "AdminPlugin",
          "SandboxType": "AssemblyLoadContext",
          "MaxMemoryBytes": 536870912,
          "FileSystemAccess": {
            "AllowFileSystemAccess": true,
            "AllowedPaths": [
              "/app/plugins",
              "/data",
              "/tmp"
            ],
            "RestrictedPaths": [
              "/etc/passwd",
              "/etc/shadow",
              "/root"
            ]
          },
          "NetworkAccess": {
            "AllowNetworkAccess": true,
            "AllowedPorts": [80, 443, 10332, 10333, 20332, 20333],
            "MaxConnections": 20
          }
        }
      },
      "GlobalSettings": {
        "CrossPlatformOptimization": true,
        "AutoSelectSandboxType": false
      }
    }
  }
}
```

## Performance Tuning

### Performance Profiles

Configure system-wide performance characteristics:

```json
{
  "GlobalSettings": {
    "PerformanceProfile": "HighPerformance",
    "EnablePerformanceMode": true,
    "CrossPlatformOptimization": true
  }
}
```

**Available Profiles**:

| Profile | Description | Cache Size | Monitor Interval | Use Case |
|---------|-------------|------------|------------------|----------|
| `HighPerformance` | Maximum speed, minimal overhead | 1000 | 10s | High-throughput nodes |
| `Balanced` | Balance of security and performance | 500 | 5s | General production |
| `MaxSecurity` | Security over performance | 100 | 1s | High-security environments |

### Cache Configuration

Optimize permission and policy caching:

```json
{
  "Policies": {
    "OptimizedPolicy": {
      "ResourceMonitoring": {
        "CheckInterval": 10000,
        "CacheSize": 1000,
        "EnableDetailedMonitoring": false
      }
    }
  }
}
```

### Memory Optimization

For memory-constrained environments:

```json
{
  "Policies": {
    "LowMemoryPolicy": {
      "MaxMemoryBytes": 134217728,
      "MaxThreads": 5,
      "ResourceMonitoring": {
        "CheckInterval": 15000,
        "MemoryWarningThreshold": 0.9
      }
    }
  }
}
```

## Troubleshooting

### Common Configuration Issues

#### Plugin Fails to Load

**Symptom**: Plugin initialization fails with security violations

**Causes & Solutions**:

1. **Insufficient permissions**
   ```json
   // Solution: Grant required permissions
   "DefaultPermissions": "NetworkPlugin"  // Instead of "ReadOnly"
   ```

2. **Sandbox type incompatibility**
   ```json
   // Solution: Use compatible sandbox type
   "SandboxType": "AssemblyLoadContext"  // Instead of "Container"
   ```

3. **Resource limits too restrictive**
   ```json
   // Solution: Increase limits
   "MaxMemoryBytes": 536870912,  // Increase from 256MB to 512MB
   "MaxExecutionTimeSeconds": 600  // Increase timeout
   ```

#### Performance Issues

**Symptom**: Slow plugin execution or high CPU usage

**Solutions**:

1. **Optimize monitoring intervals**
   ```json
   "ResourceMonitoring": {
     "CheckInterval": 10000,  // Increase from 5000
     "EnableDetailedMonitoring": false
   }
   ```

2. **Use performance profile**
   ```json
   "GlobalSettings": {
     "PerformanceProfile": "HighPerformance",
     "EnablePerformanceMode": true
   }
   ```

3. **Increase cache sizes**
   ```json
   "ResourceMonitoring": {
     "CacheSize": 1000  // Increase cache size
   }
   ```

#### Logging Issues

**Symptom**: Security events not being logged

**Solutions**:

1. **Check audit logging configuration**
   ```json
   "AuditLogging": {
     "Enabled": true,
     "LogLevel": "Info",
     "LogFile": "/var/log/neo/security-audit.log"
   }
   ```

2. **Verify file permissions**
   ```bash
   sudo chown neo:neo /var/log/neo/
   sudo chmod 755 /var/log/neo/
   ```

3. **Check disk space**
   ```bash
   df -h /var/log/
   ```

### Debugging Configuration

Enable test mode for configuration validation:

```json
{
  "GlobalSettings": {
    "TestModeEnabled": true,
    "PerformanceProfile": "Development"
  },
  "AuditLogging": {
    "LogLevel": "Debug",
    "EventHandlers": ["FileHandler", "ConsoleHandler"]
  }
}
```

### Validation Tools

Test your configuration with the included validation utility:

```bash
# Validate configuration syntax
neo-cli --validate-config /path/to/config.json

# Test plugin loading with specific policy
neo-cli --test-plugin MyPlugin.dll --policy Development

# Check sandbox compatibility
neo-cli --check-sandbox-support
```

## Best Practices

### Security Best Practices

1. **Start Restrictive**: Begin with restrictive policies and gradually grant permissions
2. **Principle of Least Privilege**: Grant only the minimum required permissions
3. **Regular Reviews**: Periodically review and update security policies
4. **Monitor Resources**: Enable detailed monitoring in production
5. **Sign Plugins**: Require signed plugins in production environments

### Configuration Management

1. **Version Control**: Keep configuration files in version control
2. **Environment Separation**: Use different configs for dev/test/prod
3. **Backup Configurations**: Maintain backups of working configurations
4. **Document Changes**: Log all configuration changes with rationale
5. **Test Before Deploy**: Validate configuration changes in test environment

### Performance Best Practices

1. **Profile-Based Tuning**: Use appropriate performance profiles
2. **Monitor Resource Usage**: Track memory, CPU, and thread utilization
3. **Cache Optimization**: Tune cache sizes based on actual usage
4. **Batch Operations**: Process multiple operations together when possible
5. **Periodic Cleanup**: Enable automatic cleanup of expired cache entries

### Operational Best Practices

1. **Gradual Rollout**: Deploy configuration changes gradually
2. **Monitoring Alerts**: Set up alerts for security violations
3. **Log Rotation**: Configure appropriate log rotation policies
4. **Capacity Planning**: Monitor resource usage trends
5. **Incident Response**: Establish procedures for security violations

---

**Last Updated**: December 2024  
**Version**: 1.0  
**Compatibility**: Neo N3, .NET 9.0