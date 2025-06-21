// Copyright (C) 2015-2025 The Neo Project.
//
// PerformanceOptimizedSandbox.cs file belongs to the neo project and is free
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Performance-optimized sandbox wrapper that reduces monitoring overhead
    /// and improves execution efficiency while maintaining security.
    /// </summary>
    public class PerformanceOptimizedSandbox : IPluginSandbox
    {
        private readonly IPluginSandbox _baseSandbox;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly OptimizationSettings _settings;
        private bool _disposed = false;

        /// <inheritdoc />
        public SandboxType Type => _baseSandbox.Type;

        /// <inheritdoc />
        public bool IsActive => _baseSandbox?.IsActive ?? false;

        /// <summary>
        /// Initializes a new instance of the PerformanceOptimizedSandbox.
        /// </summary>
        /// <param name="baseSandbox">The underlying sandbox to optimize.</param>
        /// <param name="settings">Performance optimization settings.</param>
        public PerformanceOptimizedSandbox(IPluginSandbox baseSandbox, OptimizationSettings settings = null)
        {
            _baseSandbox = baseSandbox ?? throw new ArgumentNullException(nameof(baseSandbox));
            _settings = settings ?? OptimizationSettings.Default;
            _performanceMonitor = new PerformanceMonitor(_settings);
        }

        /// <inheritdoc />
        public async Task InitializeAsync(PluginSecurityPolicy policy)
        {
            // Optimize policy for performance
            var optimizedPolicy = OptimizePolicy(policy);
            await _baseSandbox.InitializeAsync(optimizedPolicy);
            _performanceMonitor.Start();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<SandboxResult> ExecuteAsync(Func<object> action)
        {
            return await ExecuteWithOptimization(() => _baseSandbox.ExecuteAsync(action));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<SandboxResult> ExecuteAsync(Func<Task<object>> action)
        {
            return await ExecuteWithOptimization(() => _baseSandbox.ExecuteAsync(action));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidatePermission(PluginPermissions permission)
        {
            // Use centralized cache system for permission validation
            // Note: We don't have access to plugin name here, so delegate to base sandbox
            // The centralized cache will be used at the PluginSecurityManager level
            return _baseSandbox.ValidatePermission(permission);
        }

        /// <inheritdoc />
        public ResourceUsage GetResourceUsage()
        {
            // Use cached resource usage if recent enough
            return _performanceMonitor.GetCachedResourceUsage() ??
                   _performanceMonitor.CacheResourceUsage(_baseSandbox.GetResourceUsage());
        }

        /// <inheritdoc />
        public void Suspend()
        {
            _baseSandbox.Suspend();
            _performanceMonitor.OnSuspend();
        }

        /// <inheritdoc />
        public void Resume()
        {
            _baseSandbox.Resume();
            _performanceMonitor.OnResume();
        }

        /// <inheritdoc />
        public void Terminate()
        {
            _baseSandbox.Terminate();
            _performanceMonitor.OnTerminate();
        }

        private async Task<SandboxResult> ExecuteWithOptimization(Func<Task<SandboxResult>> execution)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Pre-execution optimizations
                if (_settings.EnableGcOptimization)
                {
                    GC.TryStartNoGCRegion(1024 * 1024); // 1MB no-GC region
                }

                // Execute with performance monitoring
                var result = await execution();

                // Post-execution optimizations
                _performanceMonitor.RecordExecution(stopwatch.ElapsedMilliseconds);

                return result;
            }
            finally
            {
                if (_settings.EnableGcOptimization)
                {
                    try { GC.EndNoGCRegion(); } catch { }
                }
                stopwatch.Stop();
            }
        }

        private PluginSecurityPolicy OptimizePolicy(PluginSecurityPolicy policy)
        {
            if (policy == null) return null;

            var optimizedPolicy = new PluginSecurityPolicy
            {
                DefaultPermissions = policy.DefaultPermissions,
                MaxPermissions = policy.MaxPermissions,
                MaxMemoryBytes = policy.MaxMemoryBytes,
                MaxCpuPercent = policy.MaxCpuPercent,
                MaxThreads = policy.MaxThreads,
                MaxExecutionTimeSeconds = policy.MaxExecutionTimeSeconds,
                ViolationAction = policy.ViolationAction,
                EnableDetailedMonitoring = _settings.EnableDetailedMonitoring ? policy.EnableDetailedMonitoring : false,
                StrictMode = policy.StrictMode,
                RequireSignedPlugins = policy.RequireSignedPlugins,
                SandboxType = policy.SandboxType,
                FileSystemAccess = policy.FileSystemAccess,
                NetworkAccess = policy.NetworkAccess,
                ResourceMonitoring = OptimizeResourceMonitoring(policy.ResourceMonitoring)
            };

            return optimizedPolicy;
        }

        private ResourceMonitoringPolicy OptimizeResourceMonitoring(ResourceMonitoringPolicy policy)
        {
            if (policy == null) return null;

            return new ResourceMonitoringPolicy
            {
                EnableMonitoring = policy.EnableMonitoring,
                CheckInterval = Math.Max(policy.CheckInterval, _settings.MinMonitoringInterval),
                MemoryWarningThreshold = policy.MemoryWarningThreshold,
                CpuWarningThreshold = policy.CpuWarningThreshold
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _performanceMonitor?.Dispose();
                _baseSandbox?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Performance optimization settings for sandboxes.
    /// </summary>
    public class OptimizationSettings
    {
        /// <summary>
        /// Gets the default optimization settings.
        /// </summary>
        public static OptimizationSettings Default => new OptimizationSettings();

        /// <summary>
        /// Gets the high-performance optimization settings.
        /// </summary>
        public static OptimizationSettings HighPerformance => new OptimizationSettings
        {
            EnableDetailedMonitoring = false,
            MinMonitoringInterval = 10000, // 10 seconds
            EnableGcOptimization = true,
            CacheTimeout = TimeSpan.FromMinutes(5),
            PermissionCacheSize = 1000,
            ResourceUsageCacheTimeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Gets the low-latency optimization settings.
        /// </summary>
        public static OptimizationSettings LowLatency => new OptimizationSettings
        {
            EnableDetailedMonitoring = false,
            MinMonitoringInterval = 30000, // 30 seconds
            EnableGcOptimization = true,
            CacheTimeout = TimeSpan.FromMinutes(10),
            PermissionCacheSize = 500,
            ResourceUsageCacheTimeout = TimeSpan.FromMinutes(1)
        };

        /// <summary>
        /// Gets or sets whether to enable detailed monitoring.
        /// </summary>
        public bool EnableDetailedMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum monitoring interval in milliseconds.
        /// </summary>
        public int MinMonitoringInterval { get; set; } = 5000;

        /// <summary>
        /// Gets or sets whether to enable garbage collection optimizations.
        /// </summary>
        public bool EnableGcOptimization { get; set; } = false;

        /// <summary>
        /// Gets or sets the cache timeout for optimization data.
        /// </summary>
        public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the maximum size of the permission cache.
        /// </summary>
        public int PermissionCacheSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the resource usage cache timeout.
        /// </summary>
        public TimeSpan ResourceUsageCacheTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Monitors and optimizes sandbox performance.
    /// </summary>
    internal class PerformanceMonitor : IDisposable
    {
        private readonly OptimizationSettings _settings;
        private readonly ConcurrentDictionary<PluginPermissions, (bool Result, DateTime Cached)> _permissionCache;
        private readonly Timer _cacheCleanupTimer;
        private ResourceUsage _cachedResourceUsage;
        private DateTime _resourceUsageCacheTime;
        private readonly object _resourceCacheLock = new object();
        private bool _disposed = false;

        // Performance metrics
        private long _totalExecutions;
        private long _totalExecutionTime;
        private DateTime _startTime;
        private bool _suspended = false;

        public PerformanceMonitor(OptimizationSettings settings)
        {
            _settings = settings;
            _permissionCache = new ConcurrentDictionary<PluginPermissions, (bool, DateTime)>();
            _cacheCleanupTimer = new Timer(CleanupCaches, null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public void Start()
        {
            _startTime = DateTime.UtcNow;
        }

        public bool? GetCachedPermissionResult(PluginPermissions permission)
        {
            if (_permissionCache.TryGetValue(permission, out var cached))
            {
                if (DateTime.UtcNow - cached.Cached < _settings.CacheTimeout)
                {
                    return cached.Result;
                }

                // Remove expired entry
                _permissionCache.TryRemove(permission, out _);
            }

            return null;
        }

        public bool CachePermissionResult(PluginPermissions permission, bool result)
        {
            // Limit cache size
            if (_permissionCache.Count >= _settings.PermissionCacheSize)
            {
                CleanupPermissionCache();
            }

            _permissionCache.TryAdd(permission, (result, DateTime.UtcNow));
            return result;
        }

        public ResourceUsage GetCachedResourceUsage()
        {
            lock (_resourceCacheLock)
            {
                if (_cachedResourceUsage != null &&
                    DateTime.UtcNow - _resourceUsageCacheTime < _settings.ResourceUsageCacheTimeout)
                {
                    return _cachedResourceUsage;
                }

                return null;
            }
        }

        public ResourceUsage CacheResourceUsage(ResourceUsage usage)
        {
            lock (_resourceCacheLock)
            {
                _cachedResourceUsage = usage;
                _resourceUsageCacheTime = DateTime.UtcNow;
                return usage;
            }
        }

        public void RecordExecution(long executionTimeMs)
        {
            Interlocked.Increment(ref _totalExecutions);
            Interlocked.Add(ref _totalExecutionTime, executionTimeMs);
        }

        public void OnSuspend()
        {
            _suspended = true;
        }

        public void OnResume()
        {
            _suspended = false;
        }

        public void OnTerminate()
        {
            // Log performance statistics
            var avgExecutionTime = _totalExecutions > 0 ? _totalExecutionTime / (double)_totalExecutions : 0;
            var uptime = DateTime.UtcNow - _startTime;

            Utility.Log("PerformanceMonitor", LogLevel.Info,
                $"Performance stats - Executions: {_totalExecutions}, " +
                $"Avg time: {avgExecutionTime:F2}ms, Uptime: {uptime:g}");
        }

        private void CleanupCaches(object state)
        {
            if (_disposed || _suspended) return;

            CleanupPermissionCache();

            // Clear resource usage cache if expired
            lock (_resourceCacheLock)
            {
                if (_cachedResourceUsage != null &&
                    DateTime.UtcNow - _resourceUsageCacheTime > _settings.ResourceUsageCacheTimeout)
                {
                    _cachedResourceUsage = null;
                }
            }
        }

        private void CleanupPermissionCache()
        {
            var cutoff = DateTime.UtcNow - _settings.CacheTimeout;
            var toRemove = new List<PluginPermissions>();

            foreach (var kvp in _permissionCache)
            {
                if (kvp.Value.Cached < cutoff)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                _permissionCache.TryRemove(key, out _);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cacheCleanupTimer?.Dispose();
                _permissionCache?.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Factory for creating performance-optimized sandboxes.
    /// </summary>
    public static class OptimizedSandboxFactory
    {
        /// <summary>
        /// Creates a performance-optimized sandbox based on the specified requirements.
        /// </summary>
        /// <param name="baseSandboxType">The base sandbox type to optimize.</param>
        /// <param name="performanceProfile">The performance optimization profile.</param>
        /// <returns>An optimized sandbox instance.</returns>
        public static IPluginSandbox CreateOptimized(SandboxType baseSandboxType, PerformanceProfile performanceProfile)
        {
            IPluginSandbox baseSandbox = baseSandboxType switch
            {
                SandboxType.PassThrough => new PassThroughSandbox(),
                SandboxType.AssemblyLoadContext => new AssemblyLoadContextSandbox(),
                SandboxType.Process => new ProcessSandbox(),
                SandboxType.Container => new ContainerSandbox(),
                _ => new CrossPlatformSandbox()
            };

            var settings = performanceProfile switch
            {
                PerformanceProfile.HighPerformance => OptimizationSettings.HighPerformance,
                PerformanceProfile.LowLatency => OptimizationSettings.LowLatency,
                PerformanceProfile.Balanced => OptimizationSettings.Default,
                _ => OptimizationSettings.Default
            };

            return new PerformanceOptimizedSandbox(baseSandbox, settings);
        }

        /// <summary>
        /// Creates an auto-optimized sandbox that selects the best configuration
        /// based on the current system and workload characteristics.
        /// </summary>
        /// <param name="policy">The security policy.</param>
        /// <returns>An auto-optimized sandbox instance.</returns>
        public static IPluginSandbox CreateAutoOptimized(PluginSecurityPolicy policy)
        {
            var platform = PlatformInfo.Current;

            // Select base sandbox type
            var sandboxType = policy.SandboxType;
            if (sandboxType == SandboxType.Container && !platform.HasContainerSupport)
            {
                sandboxType = SandboxType.AssemblyLoadContext;
            }

            // Select performance profile based on system characteristics
            var profile = DetermineOptimalProfile(platform, policy);

            return CreateOptimized(sandboxType, profile);
        }

        private static PerformanceProfile DetermineOptimalProfile(PlatformInfo platform, PluginSecurityPolicy policy)
        {
            // High-performance systems with relaxed security can use high-performance profile
            if (platform.ProcessorCount >= 8 && !policy.StrictMode)
            {
                return PerformanceProfile.HighPerformance;
            }

            // Memory-constrained or containerized environments prefer low-latency
            if (platform.IsContainerized || platform.TotalMemory < 2L * 1024 * 1024 * 1024) // Less than 2GB
            {
                return PerformanceProfile.LowLatency;
            }

            // Default to balanced for most scenarios
            return PerformanceProfile.Balanced;
        }
    }

    /// <summary>
    /// Performance optimization profiles.
    /// </summary>
    public enum PerformanceProfile
    {
        /// <summary>
        /// Balanced performance and monitoring.
        /// </summary>
        Balanced,

        /// <summary>
        /// Optimized for maximum execution performance.
        /// </summary>
        HighPerformance,

        /// <summary>
        /// Optimized for low latency and reduced overhead.
        /// </summary>
        LowLatency
    }
}
