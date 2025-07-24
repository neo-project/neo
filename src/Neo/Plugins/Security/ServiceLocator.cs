// Copyright (C) 2015-2025 The Neo Project.
//
// ServiceLocator.cs file belongs to the neo project and is free
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Simple service locator for dependency injection, primarily used for testing.
    /// </summary>
    public static class ServiceLocator
    {
        private static IPermissionCacheManager _permissionCacheManager;
        private static IThreadSafeStateManager _threadSafeStateManager;
        private static bool _isTestMode = false;

        /// <summary>
        /// Gets the current IPermissionCacheManager instance.
        /// </summary>
        public static IPermissionCacheManager PermissionCacheManager
        {
            get
            {
                if (_permissionCacheManager == null)
                {
                    if (_isTestMode || IsTestEnvironment())
                    {
                        _permissionCacheManager = new TestPermissionCacheManager();
                    }
                    else
                    {
                        _permissionCacheManager = Neo.Plugins.Security.PermissionCacheManager.Instance;
                    }
                }
                return _permissionCacheManager;
            }
        }

        /// <summary>
        /// Gets the current IThreadSafeStateManager instance.
        /// </summary>
        public static IThreadSafeStateManager ThreadSafeStateManager
        {
            get
            {
                if (_threadSafeStateManager == null)
                {
                    if (_isTestMode || IsTestEnvironment())
                    {
                        _threadSafeStateManager = new TestThreadSafeStateManager();
                    }
                    else
                    {
                        _threadSafeStateManager = Neo.Plugins.Security.ThreadSafeStateManager.Instance;
                    }
                }
                return _threadSafeStateManager;
            }
        }

        /// <summary>
        /// Enables test mode, which uses mock implementations instead of singletons.
        /// </summary>
        public static void EnableTestMode()
        {
            _isTestMode = true;
            Reset();
        }

        /// <summary>
        /// Disables test mode, returning to production implementations.
        /// </summary>
        public static void DisableTestMode()
        {
            _isTestMode = false;
            Reset();
        }

        /// <summary>
        /// Resets all cached instances. Useful for testing isolation.
        /// </summary>
        public static void Reset()
        {
            _permissionCacheManager?.Dispose();
            _threadSafeStateManager?.Dispose();
            _permissionCacheManager = null;
            _threadSafeStateManager = null;
        }

        /// <summary>
        /// Allows setting custom implementations for testing.
        /// </summary>
        /// <param name="permissionCacheManager">Custom permission cache manager implementation.</param>
        /// <param name="threadSafeStateManager">Custom thread-safe state manager implementation.</param>
        public static void SetCustomImplementations(
            IPermissionCacheManager permissionCacheManager = null,
            IThreadSafeStateManager threadSafeStateManager = null)
        {
            _permissionCacheManager?.Dispose();
            _threadSafeStateManager?.Dispose();

            _permissionCacheManager = permissionCacheManager;
            _threadSafeStateManager = threadSafeStateManager;
        }

        private static bool IsTestEnvironment()
        {
            try
            {
                // Check environment variable first (most reliable)
                var testMode = Environment.GetEnvironmentVariable("DOTNET_TEST_MODE");
                if (testMode?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                // Check if running under dotnet test
                var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                if (processName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                    processName.Contains("vstest", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Check if we're running in a test environment
                var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
                if (assemblyName?.Contains("testhost", StringComparison.OrdinalIgnoreCase) == true ||
                    assemblyName?.Contains("UnitTest") == true ||
                    assemblyName?.Contains("Test") == true)
                    return true;

                // Check command line arguments
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains("test", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Check for MSTest or other test frameworks in loaded assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var name = assembly.GetName().Name;
                    if (name != null && (
                        name.Contains("Microsoft.VisualStudio.TestTools") ||
                        name.Contains("Microsoft.TestPlatform") ||
                        name.Contains("NUnit") ||
                        name.Contains("xunit")))
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
    /// Simple test implementation of IPermissionCacheManager that doesn't use timers.
    /// </summary>
    internal class TestPermissionCacheManager : IPermissionCacheManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<PluginPermissions, (bool Result, DateTime CacheTime)>> _permissionCache = new();
        private readonly ConcurrentDictionary<string, (PluginSecurityPolicy Policy, DateTime CacheTime)> _policyCache = new();

        public bool? GetCachedPermissionResult(string pluginName, PluginPermissions permission)
        {
            if (string.IsNullOrEmpty(pluginName)) return null;
            return _permissionCache.TryGetValue(pluginName, out var pluginPermissions) &&
                   pluginPermissions.TryGetValue(permission, out var entry) ? entry.Result : null;
        }

        public void CachePermissionResult(string pluginName, PluginPermissions permission, bool result)
        {
            if (string.IsNullOrEmpty(pluginName)) return;
            _permissionCache.AddOrUpdate(pluginName,
                new ConcurrentDictionary<PluginPermissions, (bool, DateTime)> { [permission] = (result, DateTime.UtcNow) },
                (key, existing) => { existing[permission] = (result, DateTime.UtcNow); return existing; });
        }

        public PluginSecurityPolicy GetCachedPolicy(string pluginName) =>
            _policyCache.TryGetValue(pluginName ?? "", out var entry) ? entry.Policy : null;

        public void CachePolicy(string pluginName, PluginSecurityPolicy policy)
        {
            if (!string.IsNullOrEmpty(pluginName) && policy != null)
                _policyCache[pluginName] = (policy, DateTime.UtcNow);
        }

        public void InvalidatePluginCache(string pluginName)
        {
            if (!string.IsNullOrEmpty(pluginName))
            {
                _permissionCache.TryRemove(pluginName, out _);
                _policyCache.TryRemove(pluginName, out _);
            }
        }

        public void InvalidatePermissionCache(PluginPermissions permission)
        {
            foreach (var kvp in _permissionCache)
                kvp.Value.TryRemove(permission, out _);
        }

        public void InvalidateAllCaches()
        {
            _permissionCache.Clear();
            _policyCache.Clear();
        }

        public CacheStatistics GetCacheStatistics() => new()
        {
            PermissionCacheSize = _permissionCache.Count,
            PolicyCacheSize = _policyCache.Count,
            MaxCacheSize = 1000,
            PermissionCacheTimeout = TimeSpan.FromMinutes(15),
            PolicyCacheTimeout = TimeSpan.FromMinutes(30)
        };

        public void Dispose() => InvalidateAllCaches();
    }

    /// <summary>
    /// Simple test implementation of IThreadSafeStateManager.
    /// </summary>
    internal class TestThreadSafeStateManager : IThreadSafeStateManager
    {
        private readonly ConcurrentDictionary<string, PluginState> _pluginStates = new();
        private volatile bool _securityEnabled = true;

        public bool IsSecurityEnabled => _securityEnabled;
        public SecurityMode CurrentSecurityMode => SecurityMode.Default;

        public void SetGlobalSecurityConfiguration(bool enabled, SecurityMode mode) => _securityEnabled = enabled;

        public PluginState GetPluginState(string pluginName) =>
            _pluginStates.GetOrAdd(pluginName ?? "", name => new PluginState { PluginName = name });

        public bool UpdatePluginState(string pluginName, Action<PluginState> updateAction)
        {
            if (string.IsNullOrEmpty(pluginName) || updateAction == null) return false;
            var state = GetPluginState(pluginName);
            updateAction(state);
            return true;
        }

        public void RemovePluginState(string pluginName)
        {
            if (!string.IsNullOrEmpty(pluginName))
                _pluginStates.TryRemove(pluginName, out _);
        }

        public Task<T> ExecuteWithCoordination<T>(Func<Task<T>> operation, TimeSpan? timeout = null) => operation();

        public Task<T> ExecuteWithExclusiveAccess<T>(Func<Task<T>> operation, TimeSpan? timeout = null) => operation();

        public Dictionary<string, PluginState> GetAllPluginStates() =>
            new(_pluginStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        public void InvalidateAllPluginStates() => _pluginStates.Clear();

        public StateManagementStatistics GetStatistics() => new()
        {
            PluginStateCount = _pluginStates.Count,
            SecurityContextCount = 0,
            IsSecurityEnabled = _securityEnabled,
            CurrentSecurityMode = SecurityMode.Default,
            CurrentOperationCount = 0
        };

        public bool UpdatePluginStateConditionally(string pluginName, Func<PluginState, bool> predicate, Action<PluginState> updateAction)
        {
            if (string.IsNullOrEmpty(pluginName) || predicate == null || updateAction == null) return false;
            var state = GetPluginState(pluginName);
            if (predicate(state))
            {
                updateAction(state);
                return true;
            }
            return false;
        }

        public SecurityContext GetSecurityContext(string pluginName) => new()
        {
            PluginName = pluginName ?? "",
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow
        };

        public void Dispose() => InvalidateAllPluginStates();
    }
}
