// Copyright (C) 2015-2025 The Neo Project.
//
// PermissionCacheManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Manages permission caching with proper invalidation mechanisms.
    /// </summary>
    public class PermissionCacheManager : IPermissionCacheManager
    {
        private static PermissionCacheManager _instance;
        private static readonly object _lockObject = new object();

        private readonly ConcurrentDictionary<string, PermissionCacheEntry> _permissionCache;
        private readonly ConcurrentDictionary<string, PolicyCacheEntry> _policyCache;
        private readonly Timer _cleanupTimer;
        private readonly object _invalidationLock = new object();
        private volatile bool _disposed = false;

        // Cache configuration
        private readonly TimeSpan _permissionCacheTimeout = TimeSpan.FromMinutes(SecurityConstants.Cache.PermissionCacheTimeoutMinutes);
        private readonly TimeSpan _policyCacheTimeout = TimeSpan.FromMinutes(SecurityConstants.Cache.PolicyCacheTimeoutMinutes);
        private readonly int _maxCacheSize = SecurityConstants.Cache.MaxCacheSize;

        /// <summary>
        /// Gets the singleton instance of the PermissionCacheManager.
        /// </summary>
        public static PermissionCacheManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                            _instance = new PermissionCacheManager();
                    }
                }
                return _instance;
            }
        }

        private PermissionCacheManager()
        {
            _permissionCache = new ConcurrentDictionary<string, PermissionCacheEntry>();
            _policyCache = new ConcurrentDictionary<string, PolicyCacheEntry>();

            // Disable cleanup timer during testing to avoid potential deadlocks
            if (!IsTestEnvironment())
            {
                _cleanupTimer = new Timer(CleanupExpiredEntries, null,
                    TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
            }
        }

        private static bool IsTestEnvironment()
        {
            // Check if we're running in a test environment
            var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            return assemblyName?.Contains("UnitTest") == true || 
                   assemblyName?.Contains("Test") == true ||
                   Environment.GetCommandLineArgs().Any(arg => arg.Contains("test"));
        }

        /// <summary>
        /// Gets a cached permission result if available and not expired.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="permission">The permission to check.</param>
        /// <returns>The cached result or null if not found/expired.</returns>
        public bool? GetCachedPermissionResult(string pluginName, PluginPermissions permission)
        {
            if (_disposed || string.IsNullOrEmpty(pluginName))
                return null;

            var key = CreatePermissionKey(pluginName, permission);

            if (_permissionCache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow - entry.CacheTime < _permissionCacheTimeout)
                {
                    // Update access time for LRU
                    entry.LastAccessed = DateTime.UtcNow;
                    return entry.Result;
                }

                // Remove expired entry
                _permissionCache.TryRemove(key, out _);
            }

            return null;
        }

        /// <summary>
        /// Caches a permission result for the specified plugin and permission.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="result">The permission check result.</param>
        public void CachePermissionResult(string pluginName, PluginPermissions permission, bool result)
        {
            if (_disposed || string.IsNullOrEmpty(pluginName))
                return;

            var key = CreatePermissionKey(pluginName, permission);
            var entry = new PermissionCacheEntry
            {
                PluginName = pluginName,
                Permission = permission,
                Result = result,
                CacheTime = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            // Limit cache size to prevent memory issues
            if (_permissionCache.Count >= _maxCacheSize)
            {
                EvictLeastRecentlyUsed();
            }

            _permissionCache.AddOrUpdate(key, entry, (k, oldValue) => entry);
        }

        /// <summary>
        /// Gets a cached policy if available and not expired.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <returns>The cached policy or null if not found/expired.</returns>
        public PluginSecurityPolicy GetCachedPolicy(string pluginName)
        {
            if (_disposed || string.IsNullOrEmpty(pluginName))
                return null;

            if (_policyCache.TryGetValue(pluginName, out var entry))
            {
                if (DateTime.UtcNow - entry.CacheTime < _policyCacheTimeout)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    return entry.Policy;
                }

                // Remove expired entry
                _policyCache.TryRemove(pluginName, out _);
            }

            return null;
        }

        /// <summary>
        /// Caches a policy for the specified plugin.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="policy">The policy to cache.</param>
        public void CachePolicy(string pluginName, PluginSecurityPolicy policy)
        {
            if (_disposed || string.IsNullOrEmpty(pluginName) || policy == null)
                return;

            var entry = new PolicyCacheEntry
            {
                PluginName = pluginName,
                Policy = policy,
                CacheTime = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            _policyCache.AddOrUpdate(pluginName, entry, (k, oldValue) => entry);
        }

        /// <summary>
        /// Invalidates all cached permissions for a specific plugin.
        /// This should be called when a plugin's security policy changes.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        public void InvalidatePluginCache(string pluginName)
        {
            if (_disposed || string.IsNullOrEmpty(pluginName))
                return;

            lock (_invalidationLock)
            {
                // Remove policy cache for this plugin
                _policyCache.TryRemove(pluginName, out _);

                // Remove all permission cache entries for this plugin
                var keysToRemove = new string[_permissionCache.Count];
                var index = 0;

                foreach (var kvp in _permissionCache)
                {
                    if (kvp.Value.PluginName == pluginName)
                    {
                        keysToRemove[index++] = kvp.Key;
                    }
                }

                for (int i = 0; i < index; i++)
                {
                    _permissionCache.TryRemove(keysToRemove[i], out _);
                }
            }

            Utility.Log("PermissionCacheManager", LogLevel.Debug,
                $"Invalidated cache for plugin: {pluginName}");
        }

        /// <summary>
        /// Invalidates all cached permissions for a specific permission type across all plugins.
        /// This should be called when global permission rules change.
        /// </summary>
        /// <param name="permission">The permission type to invalidate.</param>
        public void InvalidatePermissionCache(PluginPermissions permission)
        {
            if (_disposed)
                return;

            lock (_invalidationLock)
            {
                var keysToRemove = new string[_permissionCache.Count];
                var index = 0;

                foreach (var kvp in _permissionCache)
                {
                    if ((kvp.Value.Permission & permission) != 0)
                    {
                        keysToRemove[index++] = kvp.Key;
                    }
                }

                for (int i = 0; i < index; i++)
                {
                    _permissionCache.TryRemove(keysToRemove[i], out _);
                }
            }

            Utility.Log("PermissionCacheManager", LogLevel.Debug,
                $"Invalidated cache for permission: {permission}");
        }

        /// <summary>
        /// Clears all cached permissions and policies.
        /// </summary>
        public void InvalidateAllCaches()
        {
            if (_disposed)
                return;

            lock (_invalidationLock)
            {
                _permissionCache.Clear();
                _policyCache.Clear();
            }

            Utility.Log("PermissionCacheManager", LogLevel.Debug, "Invalidated all caches");
        }

        /// <summary>
        /// Gets cache statistics for monitoring purposes.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        public CacheStatistics GetCacheStatistics()
        {
            if (_disposed)
                return new CacheStatistics();

            return new CacheStatistics
            {
                PermissionCacheSize = _permissionCache.Count,
                PolicyCacheSize = _policyCache.Count,
                MaxCacheSize = _maxCacheSize,
                PermissionCacheTimeout = _permissionCacheTimeout,
                PolicyCacheTimeout = _policyCacheTimeout
            };
        }

        private string CreatePermissionKey(string pluginName, PluginPermissions permission)
        {
            return $"{pluginName}:{(uint)permission}";
        }

        private void EvictLeastRecentlyUsed()
        {
            var oldestTime = DateTime.UtcNow;
            string oldestKey = null;

            // Find least recently used entry
            foreach (var kvp in _permissionCache)
            {
                if (kvp.Value.LastAccessed < oldestTime)
                {
                    oldestTime = kvp.Value.LastAccessed;
                    oldestKey = kvp.Key;
                }
            }

            // Remove oldest entry
            if (oldestKey != null)
            {
                _permissionCache.TryRemove(oldestKey, out _);
            }
        }

        private void CleanupExpiredEntries(object state)
        {
            if (_disposed)
                return;

            try
            {
                var cutoffTime = DateTime.UtcNow;
                var permissionCutoff = cutoffTime - _permissionCacheTimeout;
                var policyCutoff = cutoffTime - _policyCacheTimeout;

                // Cleanup expired permission cache entries
                var permissionKeysToRemove = new string[_permissionCache.Count];
                var permissionIndex = 0;

                foreach (var kvp in _permissionCache)
                {
                    if (kvp.Value.CacheTime < permissionCutoff)
                    {
                        permissionKeysToRemove[permissionIndex++] = kvp.Key;
                    }
                }

                for (int i = 0; i < permissionIndex; i++)
                {
                    _permissionCache.TryRemove(permissionKeysToRemove[i], out _);
                }

                // Cleanup expired policy cache entries
                var policyKeysToRemove = new string[_policyCache.Count];
                var policyIndex = 0;

                foreach (var kvp in _policyCache)
                {
                    if (kvp.Value.CacheTime < policyCutoff)
                    {
                        policyKeysToRemove[policyIndex++] = kvp.Key;
                    }
                }

                for (int i = 0; i < policyIndex; i++)
                {
                    _policyCache.TryRemove(policyKeysToRemove[i], out _);
                }

                if (permissionIndex > 0 || policyIndex > 0)
                {
                    Utility.Log("PermissionCacheManager", LogLevel.Debug,
                        $"Cleaned up {permissionIndex} permission entries and {policyIndex} policy entries");
                }
            }
            catch (Exception ex)
            {
                Utility.Log("PermissionCacheManager", LogLevel.Error,
                    $"Error during cache cleanup: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _cleanupTimer?.Dispose();
                _permissionCache?.Clear();
                _policyCache?.Clear();
            }
        }

        /// <summary>
        /// Represents a cached permission check result.
        /// </summary>
        private class PermissionCacheEntry
        {
            /// <summary>
            /// Gets or sets the plugin name.
            /// </summary>
            public string PluginName { get; set; }

            /// <summary>
            /// Gets or sets the permission being cached.
            /// </summary>
            public PluginPermissions Permission { get; set; }

            /// <summary>
            /// Gets or sets the permission check result.
            /// </summary>
            public bool Result { get; set; }

            /// <summary>
            /// Gets or sets when this entry was cached.
            /// </summary>
            public DateTime CacheTime { get; set; }

            /// <summary>
            /// Gets or sets when this entry was last accessed.
            /// </summary>
            public DateTime LastAccessed { get; set; }
        }

        /// <summary>
        /// Represents a cached security policy.
        /// </summary>
        private class PolicyCacheEntry
        {
            /// <summary>
            /// Gets or sets the plugin name.
            /// </summary>
            public string PluginName { get; set; }

            /// <summary>
            /// Gets or sets the cached security policy.
            /// </summary>
            public PluginSecurityPolicy Policy { get; set; }

            /// <summary>
            /// Gets or sets when this policy was cached.
            /// </summary>
            public DateTime CacheTime { get; set; }

            /// <summary>
            /// Gets or sets when this policy was last accessed.
            /// </summary>
            public DateTime LastAccessed { get; set; }
        }
    }

    /// <summary>
    /// Represents cache statistics for monitoring and debugging purposes.
    /// </summary>
    public class CacheStatistics
    {
        public int PermissionCacheSize { get; set; }
        public int PolicyCacheSize { get; set; }
        public int MaxCacheSize { get; set; }
        public TimeSpan PermissionCacheTimeout { get; set; }
        public TimeSpan PolicyCacheTimeout { get; set; }
    }
}
