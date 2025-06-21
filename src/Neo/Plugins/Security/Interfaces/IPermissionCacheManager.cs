// Copyright (C) 2015-2025 The Neo Project.
//
// IPermissionCacheManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Interface for permission cache management to enable dependency injection and testing.
    /// </summary>
    public interface IPermissionCacheManager : IDisposable
    {
        /// <summary>
        /// Gets a cached permission result if available and not expired.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="permission">The permission to check.</param>
        /// <returns>The cached result or null if not found/expired.</returns>
        bool? GetCachedPermissionResult(string pluginName, PluginPermissions permission);

        /// <summary>
        /// Caches a permission result for the specified plugin and permission.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="permission">The permission.</param>
        /// <param name="result">The permission check result.</param>
        void CachePermissionResult(string pluginName, PluginPermissions permission, bool result);

        /// <summary>
        /// Gets a cached policy if available and not expired.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <returns>The cached policy or null if not found/expired.</returns>
        PluginSecurityPolicy GetCachedPolicy(string pluginName);

        /// <summary>
        /// Caches a policy for the specified plugin.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="policy">The policy to cache.</param>
        void CachePolicy(string pluginName, PluginSecurityPolicy policy);

        /// <summary>
        /// Invalidates all cached permissions for a specific plugin.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        void InvalidatePluginCache(string pluginName);

        /// <summary>
        /// Invalidates all cached permissions for a specific permission type across all plugins.
        /// </summary>
        /// <param name="permission">The permission type to invalidate.</param>
        void InvalidatePermissionCache(PluginPermissions permission);

        /// <summary>
        /// Clears all cached permissions and policies.
        /// </summary>
        void InvalidateAllCaches();

        /// <summary>
        /// Gets cache statistics for monitoring purposes.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        CacheStatistics GetCacheStatistics();
    }
}