// Copyright (C) 2015-2025 The Neo Project.
//
// PluginSecurityManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Manages plugin security policies, sandboxes, and enforcement.
    /// </summary>
    public class PluginSecurityManager : IDisposable
    {
        private static PluginSecurityManager _instance;
        private static readonly object _lockObject = new object();

        private readonly ConcurrentDictionary<string, IPluginSandbox> _sandboxes;
        private readonly ConcurrentDictionary<string, PluginSecurityPolicy> _policies;
        private PluginSecurityPolicy _defaultPolicy;
        private bool _disposed = false;

        /// <summary>
        /// Gets the singleton instance of the PluginSecurityManager.
        /// </summary>
        public static PluginSecurityManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                            _instance = new PluginSecurityManager();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether security is enabled.
        /// </summary>
        public bool IsSecurityEnabled => ServiceLocator.ThreadSafeStateManager.IsSecurityEnabled;

        private PluginSecurityManager()
        {
            _sandboxes = new ConcurrentDictionary<string, IPluginSandbox>();
            _policies = new ConcurrentDictionary<string, PluginSecurityPolicy>();
            _defaultPolicy = PluginSecurityPolicy.CreateDefault();

            // Skip configuration loading in test environments for faster initialization
            if (!IsTestEnvironment())
            {
                LoadConfiguration();
            }
        }

        private static bool IsTestEnvironment()
        {
            try
            {
                var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                return processName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                       processName.Contains("vstest", StringComparison.OrdinalIgnoreCase) ||
                       Environment.GetEnvironmentVariable("DOTNET_TEST_MODE")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Initializes the security manager with the specified configuration.
        /// </summary>
        /// <param name="configPath">Path to the security configuration file.</param>
        public void Initialize(string configPath = null)
        {
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = Path.Combine(Plugin.PluginsDirectory, "security.json");
            }

            LoadConfiguration(configPath);

            // Update global security state
            ServiceLocator.ThreadSafeStateManager.SetGlobalSecurityConfiguration(true, SecurityMode.Default);
        }

        /// <summary>
        /// Creates a sandbox for the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="policy">The security policy to apply. If null, uses default policy.</param>
        /// <returns>The created sandbox.</returns>
        public async Task<IPluginSandbox> CreateSandboxAsync(string pluginName, PluginSecurityPolicy policy = null)
        {
            if (string.IsNullOrEmpty(pluginName))
                throw new ArgumentNullException(nameof(pluginName));

            return await ServiceLocator.ThreadSafeStateManager.ExecuteWithCoordination(async () =>
            {
                // Update plugin state to Loading
                ServiceLocator.ThreadSafeStateManager.UpdatePluginState(pluginName, state =>
                {
                    state.Status = PluginStatus.Loading;
                });

                try
                {
                    policy ??= GetPolicyForPlugin(pluginName);

                    // Validate the policy before creating sandbox
                    ValidateSecurityPolicy(policy);

                    // Create performance-optimized sandbox
                    IPluginSandbox sandbox = OptimizedSandboxFactory.CreateAutoOptimized(policy);

                    await sandbox.InitializeAsync(policy);
                    _sandboxes.TryAdd(pluginName, sandbox);

                    // Start resource monitoring for this plugin (skip in test mode to avoid circular dependency)
                    if (!IsTestEnvironment())
                    {
                        PluginResourceMonitor.Instance.StartMonitoring(pluginName, policy);
                    }

                    // Update plugin state to Running
                    ServiceLocator.ThreadSafeStateManager.UpdatePluginState(pluginName, state =>
                    {
                        state.Status = PluginStatus.Running;
                    });

                    return sandbox;
                }
                catch (Exception)
                {
                    // Update plugin state to Error
                    ServiceLocator.ThreadSafeStateManager.UpdatePluginState(pluginName, state =>
                    {
                        state.Status = PluginStatus.Error;
                    });
                    throw;
                }
            });
        }

        /// <summary>
        /// Gets the sandbox for the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <returns>The sandbox if it exists; otherwise, null.</returns>
        public IPluginSandbox GetSandbox(string pluginName)
        {
            _sandboxes.TryGetValue(pluginName, out var sandbox);
            return sandbox;
        }

        /// <summary>
        /// Removes and disposes the sandbox for the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        public void RemoveSandbox(string pluginName)
        {
            // Update plugin state to Stopped
            ServiceLocator.ThreadSafeStateManager.UpdatePluginState(pluginName, state =>
            {
                state.Status = PluginStatus.Stopped;
            });

            if (_sandboxes.TryRemove(pluginName, out var sandbox))
            {
                sandbox.Dispose();
            }

            // Stop resource monitoring for this plugin (skip in test mode to avoid circular dependency)
            if (!IsTestEnvironment())
            {
                PluginResourceMonitor.Instance.StopMonitoring(pluginName);
            }

            // Invalidate cache for this plugin
            ServiceLocator.PermissionCacheManager.InvalidatePluginCache(pluginName);

            // Remove plugin state after cleanup
            ServiceLocator.ThreadSafeStateManager.RemovePluginState(pluginName);
        }

        /// <summary>
        /// Gets the security policy for the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <returns>The security policy.</returns>
        public PluginSecurityPolicy GetPolicyForPlugin(string pluginName)
        {
            // Check cache first
            var cachedPolicy = ServiceLocator.PermissionCacheManager.GetCachedPolicy(pluginName);
            if (cachedPolicy != null)
                return cachedPolicy;

            PluginSecurityPolicy policy;
            if (_policies.TryGetValue(pluginName, out policy))
            {
                // Cache the policy for future use
                ServiceLocator.PermissionCacheManager.CachePolicy(pluginName, policy);
                return policy;
            }

            // Cache and return default policy
            ServiceLocator.PermissionCacheManager.CachePolicy(pluginName, _defaultPolicy);
            return _defaultPolicy;
        }

        /// <summary>
        /// Sets a custom security policy for the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="policy">The security policy to set.</param>
        public void SetPolicyForPlugin(string pluginName, PluginSecurityPolicy policy)
        {
            if (string.IsNullOrEmpty(pluginName))
                throw new ArgumentNullException(nameof(pluginName));
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            // Validate the policy before setting it
            ValidateSecurityPolicy(policy);

            _policies.AddOrUpdate(pluginName, policy, (key, oldValue) => policy);

            // Invalidate cache for this plugin since policy changed
            ServiceLocator.PermissionCacheManager.InvalidatePluginCache(pluginName);

            Utility.Log("PluginSecurityManager", LogLevel.Info,
                $"Updated security policy for plugin: {pluginName}");
        }

        /// <summary>
        /// Validates if a plugin has the specified permission.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <param name="permission">The permission to validate.</param>
        /// <returns>True if the permission is granted; otherwise, false.</returns>
        public bool ValidatePermission(string pluginName, PluginPermissions permission)
        {
            if (!IsSecurityEnabled)
                return true;

            // Check cache first for performance
            var cachedResult = ServiceLocator.PermissionCacheManager.GetCachedPermissionResult(pluginName, permission);
            if (cachedResult.HasValue)
                return cachedResult.Value;

            bool result;
            var sandbox = GetSandbox(pluginName);
            if (sandbox != null)
            {
                result = sandbox.ValidatePermission(permission);
            }
            else
            {
                // Fallback to policy check
                var policy = GetPolicyForPlugin(pluginName);
                result = (policy.MaxPermissions & permission) == permission;
            }

            // Cache the result for future use
            ServiceLocator.PermissionCacheManager.CachePermissionResult(pluginName, permission, result);

            return result;
        }

        /// <summary>
        /// Gets resource usage information for all sandboxes.
        /// </summary>
        /// <returns>A dictionary of plugin names and their resource usage.</returns>
        public Dictionary<string, ResourceUsage> GetAllResourceUsage()
        {
            var result = new Dictionary<string, ResourceUsage>();

            foreach (var kvp in _sandboxes)
            {
                try
                {
                    result[kvp.Key] = kvp.Value.GetResourceUsage();
                }
                catch
                {
                    // Ignore errors when getting resource usage
                }
            }

            return result;
        }

        /// <summary>
        /// Suspends the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to suspend.</param>
        public void SuspendPlugin(string pluginName)
        {
            var sandbox = GetSandbox(pluginName);
            if (sandbox != null)
            {
                sandbox.Suspend();

                // Update plugin state to Suspended
                ServiceLocator.ThreadSafeStateManager.UpdatePluginState(pluginName, state =>
                {
                    state.Status = PluginStatus.Suspended;
                });
            }
        }

        /// <summary>
        /// Resumes the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to resume.</param>
        public void ResumePlugin(string pluginName)
        {
            var sandbox = GetSandbox(pluginName);
            if (sandbox != null)
            {
                sandbox.Resume();

                // Update plugin state to Running
                ServiceLocator.ThreadSafeStateManager.UpdatePluginState(pluginName, state =>
                {
                    state.Status = PluginStatus.Running;
                });
            }
        }

        /// <summary>
        /// Terminates the specified plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to terminate.</param>
        public void TerminatePlugin(string pluginName)
        {
            var sandbox = GetSandbox(pluginName);
            sandbox?.Terminate();
        }

        /// <summary>
        /// Invalidates the permission cache for a specific plugin.
        /// Useful when external factors affect plugin permissions.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        public void InvalidatePluginPermissionCache(string pluginName)
        {
            ServiceLocator.PermissionCacheManager.InvalidatePluginCache(pluginName);
        }

        /// <summary>
        /// Invalidates the permission cache for a specific permission type across all plugins.
        /// Useful when global permission rules change.
        /// </summary>
        /// <param name="permission">The permission type to invalidate.</param>
        public void InvalidatePermissionCache(PluginPermissions permission)
        {
            ServiceLocator.PermissionCacheManager.InvalidatePermissionCache(permission);
        }

        /// <summary>
        /// Clears all permission and policy caches.
        /// Should be used when major security configuration changes occur.
        /// </summary>
        public void ClearAllCaches()
        {
            ServiceLocator.PermissionCacheManager.InvalidateAllCaches();
            Utility.Log("PluginSecurityManager", LogLevel.Info, "Invalidated all permission caches");
        }

        /// <summary>
        /// Gets cache statistics for monitoring and debugging purposes.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        public CacheStatistics GetCacheStatistics()
        {
            return ServiceLocator.PermissionCacheManager.GetCacheStatistics();
        }

        /// <summary>
        /// Gets state management statistics for monitoring and debugging purposes.
        /// </summary>
        /// <returns>State management statistics.</returns>
        public StateManagementStatistics GetStateManagementStatistics()
        {
            return ServiceLocator.ThreadSafeStateManager.GetStatistics();
        }

        /// <summary>
        /// Gets all plugin states for monitoring purposes.
        /// </summary>
        /// <returns>Dictionary of plugin names and their states.</returns>
        public Dictionary<string, PluginState> GetAllPluginStates()
        {
            return ServiceLocator.ThreadSafeStateManager.GetAllPluginStates();
        }

        /// <summary>
        /// Validates a security policy to ensure it has valid settings.
        /// </summary>
        /// <param name="policy">The policy to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the policy is invalid.</exception>
        private void ValidateSecurityPolicy(PluginSecurityPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            var errors = new List<string>();

            // Validate memory limits
            if (policy.MaxMemoryBytes < 0)
                errors.Add("MaxMemoryBytes cannot be negative");
            if (policy.MaxMemoryBytes > 0 && policy.MaxMemoryBytes < 1024 * 1024) // Minimum 1MB
                errors.Add("MaxMemoryBytes must be at least 1MB if specified");

            // Validate CPU limits
            if (policy.MaxCpuPercent < 0 || policy.MaxCpuPercent > 100)
                errors.Add("MaxCpuPercent must be between 0 and 100");

            // Validate thread limits
            if (policy.MaxThreads < 0)
                errors.Add("MaxThreads cannot be negative");
            if (policy.MaxThreads > 0 && policy.MaxThreads < 1)
                errors.Add("MaxThreads must be at least 1 if specified");

            // Validate execution time limits
            if (policy.MaxExecutionTimeSeconds < 0)
                errors.Add("MaxExecutionTimeSeconds cannot be negative");
            if (policy.MaxExecutionTimeSeconds > 0 && policy.MaxExecutionTimeSeconds < 1)
                errors.Add("MaxExecutionTimeSeconds must be at least 1 second if specified");

            // Validate permissions consistency
            if ((policy.DefaultPermissions & policy.MaxPermissions) != policy.DefaultPermissions)
                errors.Add("DefaultPermissions must be a subset of MaxPermissions");

            // Validate file system policy
            if (policy.FileSystemAccess != null)
            {
                if (policy.FileSystemAccess.MaxFileSize < 0)
                    errors.Add("FileSystemAccess.MaxFileSize cannot be negative");
                if (policy.FileSystemAccess.MaxTotalFiles < 0)
                    errors.Add("FileSystemAccess.MaxTotalFiles cannot be negative");
            }

            // Validate network policy
            if (policy.NetworkAccess != null)
            {
                if (policy.NetworkAccess.MaxConnections < 0)
                    errors.Add("NetworkAccess.MaxConnections cannot be negative");

                // Validate port numbers
                foreach (var port in policy.NetworkAccess.AllowedPorts)
                {
                    if (port < 1 || port > 65535)
                        errors.Add($"Invalid port number: {port}");
                }
            }

            // Validate resource monitoring policy
            if (policy.ResourceMonitoring != null)
            {
                if (policy.ResourceMonitoring.CheckInterval < 100)
                    errors.Add("ResourceMonitoring.CheckInterval must be at least 100ms");
                if (policy.ResourceMonitoring.MemoryWarningThreshold < 0 || policy.ResourceMonitoring.MemoryWarningThreshold > 1)
                    errors.Add("ResourceMonitoring.MemoryWarningThreshold must be between 0 and 1");
                if (policy.ResourceMonitoring.CpuWarningThreshold < 0 || policy.ResourceMonitoring.CpuWarningThreshold > 1)
                    errors.Add("ResourceMonitoring.CpuWarningThreshold must be between 0 and 1");
            }

            if (errors.Count > 0)
            {
                var errorMessage = $"Invalid security policy: {string.Join("; ", errors)}";
                Utility.Log("PluginSecurityManager", LogLevel.Error, errorMessage);
                throw new ArgumentException(errorMessage, nameof(policy));
            }
        }

        private void LoadConfiguration(string configPath = null)
        {
            var configurationErrors = new List<string>();
            bool configurationChanged = false;

            try
            {
                if (!string.IsNullOrEmpty(configPath))
                {
                    if (!File.Exists(configPath))
                    {
                        configurationErrors.Add($"Configuration file not found: {configPath}");
                    }
                    else
                    {
                        // Validate JSON format first
                        var jsonContent = File.ReadAllText(configPath);
                        if (string.IsNullOrWhiteSpace(jsonContent))
                        {
                            configurationErrors.Add("Configuration file is empty");
                        }
                        else
                        {
                            try
                            {
                                var config = new ConfigurationBuilder()
                                    .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                                    .Build();

                                LoadPolicyFromConfiguration(config, configurationErrors);
                                configurationChanged = true;
                            }
                            catch (Exception ex)
                            {
                                configurationErrors.Add($"Failed to parse JSON configuration: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                configurationErrors.Add($"Failed to load security configuration: {ex.Message}");
            }

            // Log configuration errors but continue with default policy
            if (configurationErrors.Count > 0)
            {
                var errorMessage = $"Security configuration errors: {string.Join("; ", configurationErrors)}";
                Utility.Log("PluginSecurityManager", LogLevel.Warning, errorMessage);

                // Use default policy on configuration load failure
                _defaultPolicy = PluginSecurityPolicy.CreateDefault();
                Utility.Log("PluginSecurityManager", LogLevel.Info, "Using default security policy due to configuration errors");
                configurationChanged = true;
            }
            else if (_defaultPolicy == null)
            {
                // No configuration loaded, use default
                _defaultPolicy = PluginSecurityPolicy.CreateDefault();
                Utility.Log("PluginSecurityManager", LogLevel.Info, "No security configuration specified, using default policy");
                configurationChanged = true;
            }

            // Invalidate all caches when configuration changes
            if (configurationChanged)
            {
                ServiceLocator.PermissionCacheManager.InvalidateAllCaches();
                ServiceLocator.ThreadSafeStateManager.InvalidateAllPluginStates();
                Utility.Log("PluginSecurityManager", LogLevel.Info, "Invalidated all caches and plugin states due to configuration change");
            }
        }

        private void LoadPolicyFromConfiguration(IConfiguration config, List<string> errors)
        {
            var securitySection = config.GetSection("PluginConfiguration:Security");
            if (!securitySection.Exists())
            {
                errors.Add("PluginConfiguration:Security section not found in configuration");
                return;
            }

            // Load default policy
            var defaultSection = securitySection.GetSection("DefaultPolicy");
            if (defaultSection.Exists())
            {
                try
                {
                    var policy = CreatePolicyFromConfiguration(defaultSection);
                    ValidateSecurityPolicy(policy);
                    _defaultPolicy = policy;
                }
                catch (Exception ex)
                {
                    errors.Add($"Invalid default policy configuration: {ex.Message}");
                }
            }

            // Load plugin-specific overrides
            var overridesSection = securitySection.GetSection("PluginOverrides");
            if (overridesSection.Exists())
            {
                foreach (var pluginSection in overridesSection.GetChildren())
                {
                    try
                    {
                        var policy = CreatePolicyFromConfiguration(pluginSection);
                        ValidateSecurityPolicy(policy);
                        _policies.TryAdd(pluginSection.Key, policy);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Invalid policy configuration for plugin '{pluginSection.Key}': {ex.Message}");
                    }
                }
            }
        }

        private PluginSecurityPolicy CreatePolicyFromConfiguration(IConfigurationSection section)
        {
            var policy = new PluginSecurityPolicy();

            // Parse basic settings
            if (Enum.TryParse<PluginPermissions>(section["DefaultPermissions"], out var defaultPerms))
                policy.DefaultPermissions = defaultPerms;

            if (Enum.TryParse<PluginPermissions>(section["MaxPermissions"], out var maxPerms))
                policy.MaxPermissions = maxPerms;

            if (long.TryParse(section["MaxMemoryBytes"], out var maxMemory))
                policy.MaxMemoryBytes = maxMemory;

            if (double.TryParse(section["MaxCpuPercent"], out var maxCpu))
                policy.MaxCpuPercent = maxCpu;

            if (int.TryParse(section["MaxThreads"], out var maxThreads))
                policy.MaxThreads = maxThreads;

            if (int.TryParse(section["MaxExecutionTimeSeconds"], out var maxExecTime))
                policy.MaxExecutionTimeSeconds = maxExecTime;

            if (Enum.TryParse<ViolationAction>(section["ViolationAction"], out var violationAction))
                policy.ViolationAction = violationAction;

            if (bool.TryParse(section["StrictMode"], out var strictMode))
                policy.StrictMode = strictMode;

            if (Enum.TryParse<SandboxType>(section["SandboxType"], out var sandboxType))
                policy.SandboxType = sandboxType;

            return policy;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose all sandboxes
                foreach (var sandbox in _sandboxes.Values)
                {
                    try
                    {
                        sandbox.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }

                _sandboxes.Clear();
                _policies.Clear();

                // Note: ServiceLocator handles disposal of singletons

                _disposed = true;
            }
        }
    }
}
