// Copyright (C) 2015-2025 The Neo Project.
//
// IThreadSafeStateManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Interface for thread-safe state management to enable dependency injection and testing.
    /// </summary>
    public interface IThreadSafeStateManager : IDisposable
    {
        /// <summary>
        /// Gets whether security is globally enabled.
        /// </summary>
        bool IsSecurityEnabled { get; }

        /// <summary>
        /// Gets the current security mode.
        /// </summary>
        SecurityMode CurrentSecurityMode { get; }

        /// <summary>
        /// Sets the global security configuration thread-safely.
        /// </summary>
        /// <param name="enabled">Whether security should be enabled.</param>
        /// <param name="mode">The security mode to use.</param>
        void SetGlobalSecurityConfiguration(bool enabled, SecurityMode mode);

        /// <summary>
        /// Gets the current state for a plugin in a thread-safe manner.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <returns>The current plugin state.</returns>
        PluginState GetPluginState(string pluginName);

        /// <summary>
        /// Updates plugin state atomically.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="updateAction">Action to update the plugin state.</param>
        /// <returns>True if the update was successful.</returns>
        bool UpdatePluginState(string pluginName, Action<PluginState> updateAction);

        /// <summary>
        /// Removes a plugin state entirely.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        void RemovePluginState(string pluginName);

        /// <summary>
        /// Executes an operation with thread-safe coordination.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="timeout">Optional timeout for the operation.</param>
        /// <returns>The result of the operation.</returns>
        Task<T> ExecuteWithCoordination<T>(Func<Task<T>> operation, TimeSpan? timeout = null);

        /// <summary>
        /// Executes an operation with exclusive global state access.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="timeout">Optional timeout for the operation.</param>
        /// <returns>The result of the operation.</returns>
        Task<T> ExecuteWithExclusiveAccess<T>(Func<Task<T>> operation, TimeSpan? timeout = null);

        /// <summary>
        /// Gets all plugin states for monitoring purposes.
        /// </summary>
        /// <returns>Dictionary of plugin names and their states.</returns>
        Dictionary<string, PluginState> GetAllPluginStates();

        /// <summary>
        /// Invalidates all plugin states.
        /// </summary>
        void InvalidateAllPluginStates();

        /// <summary>
        /// Gets state management statistics for monitoring and debugging purposes.
        /// </summary>
        /// <returns>State management statistics.</returns>
        StateManagementStatistics GetStatistics();

        /// <summary>
        /// Updates plugin state conditionally based on current state.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <param name="predicate">Condition to check before updating.</param>
        /// <param name="updateAction">Action to update the plugin state.</param>
        /// <returns>True if the update was successful.</returns>
        bool UpdatePluginStateConditionally(string pluginName, Func<PluginState, bool> predicate, Action<PluginState> updateAction);

        /// <summary>
        /// Gets or creates a security context for a plugin.
        /// </summary>
        /// <param name="pluginName">The plugin name.</param>
        /// <returns>The security context.</returns>
        SecurityContext GetSecurityContext(string pluginName);
    }
}
