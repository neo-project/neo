// Copyright (C) 2015-2025 The Neo Project.
//
// SecurityAuditLogger.cs file belongs to the neo project and is free
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Enhanced security audit logging system for tracking plugin security events.
    /// </summary>
    public class SecurityAuditLogger : IDisposable
    {
        private static SecurityAuditLogger _instance;
        private static readonly object _lockObject = new object();

        private readonly ConcurrentQueue<SecurityEvent> _eventQueue = new();
        private readonly ConcurrentDictionary<string, SecurityEventStats> _eventStats = new();
        private readonly Timer _flushTimer;
        private readonly string _logDirectory;
        private readonly int _maxEventsInMemory = 10000;
        private readonly ConcurrentDictionary<ISecurityEventHandler, byte> _eventHandlers = new();
        private bool _disposed = false;

        /// <summary>
        /// Gets the singleton instance of the SecurityAuditLogger.
        /// </summary>
        public static SecurityAuditLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                            _instance = new SecurityAuditLogger();
                    }
                }
                return _instance;
            }
        }

        private SecurityAuditLogger()
        {
            // Skip initialization in test environment
            if (!IsTestEnvironment())
            {
                try
                {
                    var pluginDir = Plugin.PluginsDirectory ?? Path.Combine(AppContext.BaseDirectory, "Plugins");
                    _logDirectory = Path.Combine(pluginDir, "security-logs");
                    Directory.CreateDirectory(_logDirectory);

                    // Flush events to disk every 30 seconds
                    _flushTimer = new Timer(FlushEventsToDisk, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

                    // Add default file handler
                    AddEventHandler(new FileSecurityEventHandler(_logDirectory));
                }
                catch
                {
                    // If initialization fails in production, continue without file logging
                    _logDirectory = null;
                }
            }
        }

        /// <summary>
        /// Adds a security event handler.
        /// </summary>
        /// <param name="handler">The event handler to add.</param>
        public void AddEventHandler(ISecurityEventHandler handler)
        {
            if (handler != null)
            {
                _eventHandlers.TryAdd(handler, 0);
            }
        }

        /// <summary>
        /// Removes a security event handler.
        /// </summary>
        /// <param name="handler">The event handler to remove.</param>
        public void RemoveEventHandler(ISecurityEventHandler handler)
        {
            _eventHandlers.TryRemove(handler, out _);
        }

        /// <summary>
        /// Logs a security event.
        /// </summary>
        /// <param name="eventType">The type of security event.</param>
        /// <param name="pluginName">The name of the plugin involved.</param>
        /// <param name="message">A descriptive message.</param>
        /// <param name="details">Additional event details.</param>
        /// <param name="severity">The severity level of the event.</param>
        public void LogSecurityEvent(SecurityEventType eventType, string pluginName, string message,
            object details = null, SecurityEventSeverity severity = SecurityEventSeverity.Medium)
        {
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                EventType = eventType,
                PluginName = pluginName,
                Message = message,
                Details = details,
                Severity = severity,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            LogEvent(securityEvent);
        }

        /// <summary>
        /// Logs a plugin permission request.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="permission">The requested permission.</param>
        /// <param name="granted">Whether the permission was granted.</param>
        public void LogPermissionRequest(string pluginName, PluginPermissions permission, bool granted)
        {
            LogSecurityEvent(
                SecurityEventType.PermissionRequest,
                pluginName,
                $"Permission request: {permission} - {(granted ? "GRANTED" : "DENIED")}",
                new { Permission = permission, Granted = granted },
                granted ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium
            );
        }

        /// <summary>
        /// Logs a resource violation.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="violation">The resource violation details.</param>
        public void LogResourceViolation(string pluginName, ResourceViolation violation)
        {
            LogSecurityEvent(
                SecurityEventType.ResourceViolation,
                pluginName,
                $"Resource violation: {violation.ViolationType}",
                violation,
                SecurityEventSeverity.High
            );
        }

        /// <summary>
        /// Logs a sandbox operation.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="operation">The sandbox operation.</param>
        /// <param name="sandboxType">The type of sandbox.</param>
        /// <param name="success">Whether the operation was successful.</param>
        public void LogSandboxOperation(string pluginName, string operation, SandboxType sandboxType, bool success)
        {
            LogSecurityEvent(
                SecurityEventType.SandboxOperation,
                pluginName,
                $"Sandbox {operation}: {sandboxType} - {(success ? "SUCCESS" : "FAILED")}",
                new { Operation = operation, SandboxType = sandboxType, Success = success },
                success ? SecurityEventSeverity.Low : SecurityEventSeverity.Medium
            );
        }

        /// <summary>
        /// Logs a configuration change.
        /// </summary>
        /// <param name="configType">The type of configuration changed.</param>
        /// <param name="details">Details about the change.</param>
        public void LogConfigurationChange(string configType, object details)
        {
            LogSecurityEvent(
                SecurityEventType.ConfigurationChange,
                "System",
                $"Configuration changed: {configType}",
                details,
                SecurityEventSeverity.Medium
            );
        }

        /// <summary>
        /// Gets security event statistics.
        /// </summary>
        /// <returns>Dictionary of event types and their statistics.</returns>
        public Dictionary<SecurityEventType, SecurityEventStats> GetEventStatistics()
        {
            return _eventStats.ToDictionary(
                kvp => Enum.Parse<SecurityEventType>(kvp.Key),
                kvp => kvp.Value
            );
        }

        /// <summary>
        /// Gets recent security events.
        /// </summary>
        /// <param name="count">Number of recent events to retrieve.</param>
        /// <returns>Array of recent security events.</returns>
        public SecurityEvent[] GetRecentEvents(int count = 100)
        {
            return _eventQueue.TakeLast(Math.Min(count, _eventQueue.Count)).ToArray();
        }

        /// <summary>
        /// Gets security events for a specific plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="count">Maximum number of events to retrieve.</param>
        /// <returns>Array of security events for the plugin.</returns>
        public SecurityEvent[] GetPluginEvents(string pluginName, int count = 100)
        {
            return _eventQueue
                .Where(e => string.Equals(e.PluginName, pluginName, StringComparison.OrdinalIgnoreCase))
                .TakeLast(Math.Min(count, _eventQueue.Count))
                .ToArray();
        }

        /// <summary>
        /// Gets security events by type.
        /// </summary>
        /// <param name="eventType">The event type to filter by.</param>
        /// <param name="count">Maximum number of events to retrieve.</param>
        /// <returns>Array of security events of the specified type.</returns>
        public SecurityEvent[] GetEventsByType(SecurityEventType eventType, int count = 100)
        {
            return _eventQueue
                .Where(e => e.EventType == eventType)
                .TakeLast(Math.Min(count, _eventQueue.Count))
                .ToArray();
        }

        /// <summary>
        /// Gets security events within a time range.
        /// </summary>
        /// <param name="from">Start time.</param>
        /// <param name="to">End time.</param>
        /// <returns>Array of security events within the time range.</returns>
        public SecurityEvent[] GetEventsByTimeRange(DateTime from, DateTime to)
        {
            return _eventQueue
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .ToArray();
        }

        /// <summary>
        /// Checks if a plugin has recent high-severity events.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="timeWindow">Time window to check (default: last hour).</param>
        /// <returns>True if high-severity events exist; otherwise, false.</returns>
        public bool HasRecentHighSeverityEvents(string pluginName, TimeSpan? timeWindow = null)
        {
            var window = timeWindow ?? TimeSpan.FromHours(1);
            var cutoff = DateTime.UtcNow.Subtract(window);

            return _eventQueue.Any(e =>
                string.Equals(e.PluginName, pluginName, StringComparison.OrdinalIgnoreCase) &&
                e.Timestamp >= cutoff &&
                e.Severity == SecurityEventSeverity.Critical
            );
        }

        private void LogEvent(SecurityEvent securityEvent)
        {
            // Add to queue
            _eventQueue.Enqueue(securityEvent);

            // Update statistics
            var key = securityEvent.EventType.ToString();
            _eventStats.AddOrUpdate(key,
                new SecurityEventStats { Count = 1, LastOccurrence = securityEvent.Timestamp },
                (k, v) => new SecurityEventStats { Count = v.Count + 1, LastOccurrence = securityEvent.Timestamp }
            );

            // Notify handlers asynchronously
            _ = Task.Run(async () => await NotifyHandlersAsync(securityEvent));

            // Limit memory usage
            if (_eventQueue.Count > _maxEventsInMemory)
            {
                for (int i = 0; i < _maxEventsInMemory / 10; i++)
                {
                    _eventQueue.TryDequeue(out _);
                }
            }
        }

        private async Task NotifyHandlersAsync(SecurityEvent securityEvent)
        {
            foreach (var handler in _eventHandlers.Keys)
            {
                try
                {
                    await handler.HandleEventAsync(securityEvent);
                }
                catch (Exception ex)
                {
                    // Log handler errors but don't let them break the audit system
                    Utility.Log("SecurityAuditLogger", LogLevel.Warning,
                        $"Event handler error: {ex.Message}");
                }
            }
        }

        private void FlushEventsToDisk(object state)
        {
            // This is handled by individual event handlers
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _flushTimer?.Dispose();

                // Dispose all handlers
                foreach (var handler in _eventHandlers.Keys)
                {
                    try
                    {
                        handler?.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }

                _disposed = true;
            }
        }

        private static bool IsTestEnvironment()
        {
            try
            {
                // Check environment variable first
                var testMode = Environment.GetEnvironmentVariable("DOTNET_TEST_MODE");
                if (testMode?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                // Check if running under dotnet test
                var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                if (processName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                    processName.Contains("vstest", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Check for test assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var name = assembly.GetName().Name;
                    if (name != null && (
                        name.Contains("Microsoft.VisualStudio.TestTools") ||
                        name.Contains("Microsoft.TestPlatform")))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Represents a security event.
    /// </summary>
    public class SecurityEvent
    {
        /// <summary>
        /// Unique identifier for the event.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Type of security event.
        /// </summary>
        public SecurityEventType EventType { get; set; }

        /// <summary>
        /// Name of the plugin involved in the event.
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// Descriptive message about the event.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Additional details about the event.
        /// </summary>
        public object Details { get; set; }

        /// <summary>
        /// Severity level of the event.
        /// </summary>
        public SecurityEventSeverity Severity { get; set; }

        /// <summary>
        /// Thread ID where the event occurred.
        /// </summary>
        public int ThreadId { get; set; }
    }

    /// <summary>
    /// Types of security events.
    /// </summary>
    public enum SecurityEventType
    {
        /// <summary>
        /// Plugin permission request.
        /// </summary>
        PermissionRequest,

        /// <summary>
        /// Resource usage violation.
        /// </summary>
        ResourceViolation,

        /// <summary>
        /// Sandbox operation (create, suspend, terminate).
        /// </summary>
        SandboxOperation,

        /// <summary>
        /// Plugin lifecycle event (load, unload, error).
        /// </summary>
        PluginLifecycle,

        /// <summary>
        /// Security configuration change.
        /// </summary>
        ConfigurationChange,

        /// <summary>
        /// Authentication or authorization event.
        /// </summary>
        Authentication,

        /// <summary>
        /// File system access attempt.
        /// </summary>
        FileSystemAccess,

        /// <summary>
        /// Network access attempt.
        /// </summary>
        NetworkAccess,

        /// <summary>
        /// Cryptographic operation.
        /// </summary>
        CryptographicOperation,

        /// <summary>
        /// System-level access attempt.
        /// </summary>
        SystemAccess,

        /// <summary>
        /// General security violation.
        /// </summary>
        SecurityViolation
    }

    /// <summary>
    /// Security event severity levels.
    /// </summary>
    public enum SecurityEventSeverity
    {
        /// <summary>
        /// Low severity - informational.
        /// </summary>
        Low,

        /// <summary>
        /// Medium severity - warning.
        /// </summary>
        Medium,

        /// <summary>
        /// High severity - potential security issue.
        /// </summary>
        High,

        /// <summary>
        /// Critical severity - immediate attention required.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Statistics for security events.
    /// </summary>
    public class SecurityEventStats
    {
        /// <summary>
        /// Number of occurrences of this event type.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Timestamp of the last occurrence.
        /// </summary>
        public DateTime LastOccurrence { get; set; }
    }

    /// <summary>
    /// Interface for security event handlers.
    /// </summary>
    public interface ISecurityEventHandler : IDisposable
    {
        /// <summary>
        /// Handles a security event.
        /// </summary>
        /// <param name="securityEvent">The security event to handle.</param>
        /// <returns>Task representing the handling operation.</returns>
        Task HandleEventAsync(SecurityEvent securityEvent);
    }

    /// <summary>
    /// File-based security event handler.
    /// </summary>
    public class FileSecurityEventHandler : ISecurityEventHandler
    {
        private readonly string _logDirectory;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the FileSecurityEventHandler.
        /// </summary>
        /// <param name="logDirectory">Directory to store log files.</param>
        public FileSecurityEventHandler(string logDirectory)
        {
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            Directory.CreateDirectory(_logDirectory);
        }

        /// <inheritdoc />
        public async Task HandleEventAsync(SecurityEvent securityEvent)
        {
            if (_disposed) return;

            var fileName = $"security-audit-{DateTime.UtcNow:yyyy-MM-dd}.json";
            var filePath = Path.Combine(_logDirectory, fileName);

            await _fileLock.WaitAsync();
            try
            {
                var logEntry = new
                {
                    securityEvent.Id,
                    securityEvent.Timestamp,
                    EventType = securityEvent.EventType.ToString(),
                    securityEvent.PluginName,
                    securityEvent.Message,
                    securityEvent.Details,
                    Severity = securityEvent.Severity.ToString(),
                    securityEvent.ThreadId
                };

                var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                await File.AppendAllTextAsync(filePath, json + Environment.NewLine);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _fileLock?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Console-based security event handler for debugging.
    /// </summary>
    public class ConsoleSecurityEventHandler : ISecurityEventHandler
    {
        private bool _disposed = false;

        /// <inheritdoc />
        public Task HandleEventAsync(SecurityEvent securityEvent)
        {
            if (_disposed) return Task.CompletedTask;

            var color = securityEvent.Severity switch
            {
                SecurityEventSeverity.Critical => ConsoleColor.Red,
                SecurityEventSeverity.High => ConsoleColor.Yellow,
                SecurityEventSeverity.Medium => ConsoleColor.White,
                SecurityEventSeverity.Low => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };

            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"[{securityEvent.Timestamp:yyyy-MM-dd HH:mm:ss}] [{securityEvent.Severity}] [{securityEvent.EventType}] {securityEvent.PluginName}: {securityEvent.Message}");
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _disposed = true;
        }
    }
}
