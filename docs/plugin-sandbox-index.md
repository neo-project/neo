# Neo Plugin Sandbox Documentation

## Table of Contents

### 📋 Overview

The Neo Plugin Sandbox System provides comprehensive security isolation for third-party plugins, protecting the core Neo node while enabling powerful plugin functionality.

### 📚 Documentation

| Document | Description | Audience |
|----------|-------------|----------|
| **[Architecture Guide](plugin-sandbox-architecture.md)** | Complete system architecture and design | System architects, advanced developers |
| **[Configuration Guide](plugin-sandbox-configuration.md)** | Security policy configuration and setup | Node operators, system administrators |
| **[API Reference](plugin-sandbox-api.md)** | Complete API documentation | Plugin developers |
| **[Developer Guide](plugin-sandbox-developer-guide.md)** | Secure plugin development guide | Plugin developers |

### 🚀 Quick Start

#### For Plugin Developers

1. **Read the [Developer Guide](plugin-sandbox-developer-guide.md)** - Learn how to create secure plugins
2. **Review the [API Reference](plugin-sandbox-api.md)** - Understand available interfaces and classes
3. **Check out example implementations** in the `/src/Plugins/` directory

#### For Node Operators

1. **Read the [Configuration Guide](plugin-sandbox-configuration.md)** - Learn how to configure security policies
2. **Review the [Architecture Guide](plugin-sandbox-architecture.md)** - Understand the security model
3. **Configure your security settings** based on your trust level and requirements

### 🔒 Security Levels

| Sandbox Type | Security Level | Performance | Use Case |
|--------------|----------------|-------------|----------|
| **PassThrough** | None | Highest | Development/Testing |
| **AssemblyLoadContext** | Medium | High | Trusted plugins |
| **Process** | High | Medium | Standard plugins |
| **Container** | Maximum | Lower | Untrusted plugins |

### 🎯 Key Features

- **Multi-tiered Security Architecture** with 4 isolation levels
- **Granular Permission System** with 9 permission types
- **Real-time Resource Monitoring** for memory, CPU, and threads
- **Cross-platform Compatibility** with adaptive sandbox selection
- **Comprehensive Audit Logging** for security events
- **Performance Optimization** with caching and monitoring profiles

---

**Last Updated**: December 2024  
**Version**: 1.0  
**Compatibility**: Neo N3, .NET 9.0