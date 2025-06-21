// Copyright (C) 2015-2025 The Neo Project.
//
// AssemblyLoadContextSandbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using System.Runtime.Loader;
#endif

namespace Neo.Plugins.Security
{
    /// <summary>
    /// A sandbox implementation using .NET Core/5+ AssemblyLoadContext for isolation.
    /// Provides medium-level isolation suitable for most plugin scenarios.
    /// </summary>
    public class AssemblyLoadContextSandbox : IPluginSandbox
    {
        private PluginSecurityPolicy _policy;
#if NET5_0_OR_GREATER
        private AssemblyLoadContext _loadContext;
#endif
        private bool _disposed = false;
        private bool _suspended = false;
        private readonly object _lockObject = new object();
        private long _memoryAtStart;
        private DateTime _startTime;
        private readonly string _pluginName = "Unknown";

        /// <inheritdoc />
        public SandboxType Type => SandboxType.AssemblyLoadContext;

        /// <inheritdoc />
        public bool IsActive { get; private set; }

        /// <inheritdoc />
        public Task InitializeAsync(PluginSecurityPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));

#if NET5_0_OR_GREATER
            // Create isolated AssemblyLoadContext
            _loadContext = new AssemblyLoadContext($"PluginSandbox_{Guid.NewGuid()}", isCollectible: true);
#else
            // AssemblyLoadContext not available in .NET Standard 2.1
            // Fallback to basic isolation monitoring
#endif

            _memoryAtStart = GC.GetTotalMemory(false);
            _startTime = DateTime.UtcNow;
            IsActive = true;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<object> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return await ExecuteWithMonitoring(() => Task.FromResult(action()));
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<Task<object>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return await ExecuteWithMonitoring(action);
        }

        private async Task<SandboxResult> ExecuteWithMonitoring(Func<Task<object>> action)
        {
            lock (_lockObject)
            {
                if (_suspended)
                    throw new InvalidOperationException("Sandbox is suspended");
                if (!IsActive)
                    throw new InvalidOperationException("Sandbox is not active");
            }

            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            var result = new SandboxResult { ResourceUsage = new ResourceUsage() };

            // Set up cancellation for timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_policy.MaxExecutionTimeSeconds));
            var startTicks = Environment.TickCount;

            try
            {
                // Report operation start
                var operationId = Guid.NewGuid().ToString();
                PluginResourceMonitor.Instance.EventMonitor?.ReportOperationStart(_pluginName, operationId);

                // Monitor resource usage during execution
                var monitoringTask = MonitorResourceUsage(cts.Token);

                // Execute the action
                var actionTask = action();

                // Wait for either completion or timeout
                var completedTask = await Task.WhenAny(actionTask, Task.Delay(Timeout.Infinite, cts.Token));

                if (completedTask == actionTask)
                {
                    result.Result = await actionTask;
                    result.Success = true;
                }
                else
                {
                    result.Success = false;
                    result.Exception = new TimeoutException($"Operation timed out after {_policy.MaxExecutionTimeSeconds} seconds");
                }

                // Stop monitoring
                cts.Cancel();
                try { await monitoringTask; } catch { }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
            }
            finally
            {
                // Calculate resource usage
                var endTime = DateTime.UtcNow;
                var endMemory = GC.GetTotalMemory(false);
                var endTicks = Environment.TickCount;

                result.ResourceUsage.ExecutionTime = (long)(endTime - startTime).TotalMilliseconds;
                result.ResourceUsage.MemoryUsed = Math.Max(0, endMemory - startMemory);
                result.ResourceUsage.CpuTimeUsed = endTicks - startTicks;
#if NET5_0_OR_GREATER
                result.ResourceUsage.ThreadsCreated = System.Threading.ThreadPool.ThreadCount;
#else
                result.ResourceUsage.ThreadsCreated = 0; // ThreadCount not available in .NET Standard 2.1
#endif
            }

            return result;
        }

        private async Task MonitorResourceUsage(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_policy.ResourceMonitoring.CheckInterval, cancellationToken);

                    // Check memory usage
                    var currentMemory = GC.GetTotalMemory(false) - _memoryAtStart;
                    if (currentMemory > _policy.MaxMemoryBytes)
                    {
                        HandleViolation($"Memory limit exceeded: {currentMemory} bytes > {_policy.MaxMemoryBytes} bytes");
                    }

                    // Additional monitoring can be added here
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
                    Utility.Log("PluginSandbox", LogLevel.Warning, message);
                    break;
                case ViolationAction.Suspend:
                    Suspend();
                    Utility.Log("PluginSandbox", LogLevel.Warning, $"Plugin suspended: {message}");
                    break;
                case ViolationAction.Terminate:
                    Terminate();
                    Utility.Log("PluginSandbox", LogLevel.Error, $"Plugin terminated: {message}");
                    break;
            }
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
            var currentTime = DateTime.UtcNow;
            var currentMemory = GC.GetTotalMemory(false);

            return new ResourceUsage
            {
                MemoryUsed = Math.Max(0, currentMemory - _memoryAtStart),
                CpuTimeUsed = Environment.TickCount,
#if NET5_0_OR_GREATER
                ThreadsCreated = System.Threading.ThreadPool.ThreadCount,
#else
                ThreadsCreated = 0, // ThreadCount not available in .NET Standard 2.1
#endif
                ExecutionTime = (long)(currentTime - _startTime).TotalMilliseconds
            };
        }

        /// <inheritdoc />
        public void Suspend()
        {
            lock (_lockObject)
            {
                _suspended = true;
            }
        }

        /// <inheritdoc />
        public void Resume()
        {
            lock (_lockObject)
            {
                _suspended = false;
            }
        }

        /// <inheritdoc />
        public void Terminate()
        {
            lock (_lockObject)
            {
                IsActive = false;
                _suspended = true;
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
#if NET5_0_OR_GREATER
                    _loadContext?.Unload();
#endif
                }
                catch
                {
                    // Ignore unload errors
                }

                _disposed = true;
            }
        }
    }
}
