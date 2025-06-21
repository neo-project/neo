// Copyright (C) 2015-2025 The Neo Project.
//
// SecurePlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Base class for secure plugins that integrate with the plugin security framework.
    /// </summary>
    public abstract class SecurePlugin : Plugin
    {
        private IPluginSandbox _sandbox;
        private bool _isSecurityInitialized = false;

        /// <summary>
        /// Gets the required permissions for this plugin.
        /// Override this property to specify the minimum permissions needed.
        /// </summary>
        protected virtual PluginPermissions RequiredPermissions => PluginPermissions.ReadOnly;

        /// <summary>
        /// Gets the maximum permissions this plugin can request.
        /// Override this property to specify the upper limit of permissions.
        /// </summary>
        protected virtual PluginPermissions MaxPermissions => PluginPermissions.NetworkPlugin;

        /// <summary>
        /// Gets the custom security policy for this plugin.
        /// Override this property to provide plugin-specific security settings.
        /// </summary>
        protected virtual PluginSecurityPolicy SecurityPolicy => null;

        /// <summary>
        /// Gets the sandbox instance for this plugin.
        /// </summary>
        protected IPluginSandbox Sandbox => _sandbox;

        /// <summary>
        /// Initializes a new instance of the SecurePlugin class.
        /// </summary>
        protected SecurePlugin()
        {
            // Initialize security asynchronously
            _ = Task.Run(InitializeSecurityAsync);
        }

        /// <summary>
        /// Called when the system is loaded and security has been initialized.
        /// Override this method instead of OnSystemLoaded for secure plugins.
        /// </summary>
        /// <param name="system">The loaded NeoSystem.</param>
        protected virtual void OnSecureSystemLoaded(NeoSystem system) { }

        /// <summary>
        /// Executes an action within the security sandbox.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>The result of the sandboxed execution.</returns>
        protected async Task<SandboxResult> ExecuteSecureAsync(Func<object> action)
        {
            EnsureSecurityInitialized();

            if (_sandbox == null)
                throw new InvalidOperationException("Security sandbox is not available");

            return await _sandbox.ExecuteAsync(action);
        }

        /// <summary>
        /// Executes an asynchronous action within the security sandbox.
        /// </summary>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <returns>The result of the sandboxed execution.</returns>
        protected async Task<SandboxResult> ExecuteSecureAsync(Func<Task<object>> action)
        {
            EnsureSecurityInitialized();

            if (_sandbox == null)
                throw new InvalidOperationException("Security sandbox is not available");

            return await _sandbox.ExecuteAsync(action);
        }

        /// <summary>
        /// Validates if the plugin has the specified permission.
        /// </summary>
        /// <param name="permission">The permission to validate.</param>
        /// <returns>True if the permission is granted; otherwise, false.</returns>
        protected bool ValidatePermission(PluginPermissions permission)
        {
            if (!PluginSecurityManager.Instance.IsSecurityEnabled)
                return true;

            return PluginSecurityManager.Instance.ValidatePermission(Name, permission);
        }

        /// <summary>
        /// Validates if the plugin has the specified permission and throws an exception if not.
        /// </summary>
        /// <param name="permission">The permission to validate.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if the permission is not granted.</exception>
        protected void RequirePermission(PluginPermissions permission)
        {
            if (!ValidatePermission(permission))
            {
                throw new UnauthorizedAccessException($"Plugin '{Name}' does not have permission: {permission}");
            }
        }

        /// <summary>
        /// Gets the current resource usage of this plugin.
        /// </summary>
        /// <returns>Resource usage statistics.</returns>
        protected ResourceUsage GetResourceUsage()
        {
            return _sandbox?.GetResourceUsage() ?? new ResourceUsage();
        }

        /// <inheritdoc />
        protected internal sealed override void OnSystemLoaded(NeoSystem system)
        {
            // Ensure security is initialized before calling the secure version
            EnsureSecurityInitialized();

            // Validate required permissions
            try
            {
                RequirePermission(RequiredPermissions);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log($"Security validation failed: {ex.Message}", LogLevel.Error);
                return;
            }

            // Call the secure version
            try
            {
                OnSecureSystemLoaded(system);
            }
            catch (Exception ex)
            {
                Log($"Error in OnSecureSystemLoaded: {ex.Message}", LogLevel.Error);

                // Handle according to security policy
                if (_sandbox != null)
                {
                    var policy = PluginSecurityManager.Instance.GetPolicyForPlugin(Name);
                    if (policy.ViolationAction == ViolationAction.Terminate)
                    {
                        _sandbox.Terminate();
                    }
                }

                throw;
            }
        }

        private async Task InitializeSecurityAsync()
        {
            try
            {
                if (!PluginSecurityManager.Instance.IsSecurityEnabled)
                {
                    _isSecurityInitialized = true;
                    return;
                }

                // Set custom policy if provided
                var customPolicy = SecurityPolicy;
                if (customPolicy != null)
                {
                    PluginSecurityManager.Instance.SetPolicyForPlugin(Name, customPolicy);
                }

                // Create sandbox
                _sandbox = await PluginSecurityManager.Instance.CreateSandboxAsync(Name);
                _isSecurityInitialized = true;

                Log("Security framework initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize security: {ex.Message}", LogLevel.Error);
                _isSecurityInitialized = true; // Set to true to prevent infinite waiting
            }
        }

        private void EnsureSecurityInitialized()
        {
            // Wait for security initialization to complete
            var timeout = DateTime.UtcNow.AddSeconds(30);
            while (!_isSecurityInitialized && DateTime.UtcNow < timeout)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (!_isSecurityInitialized)
            {
                throw new TimeoutException("Security initialization timed out");
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            try
            {
                // Remove sandbox
                if (_sandbox != null)
                {
                    PluginSecurityManager.Instance.RemoveSandbox(Name);
                    _sandbox = null;
                }
            }
            catch
            {
                // Ignore disposal errors
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
