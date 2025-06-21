// Copyright (C) 2015-2025 The Neo Project.
//
// IPluginSandbox.cs file belongs to the neo project and is free
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
    /// Represents the result of a sandboxed operation.
    /// </summary>
    public class SandboxResult
    {
        /// <summary>
        /// Indicates if the operation completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The result value if the operation succeeded.
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// The exception if the operation failed.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Resource usage statistics for the operation.
        /// </summary>
        public ResourceUsage ResourceUsage { get; set; }
    }

    /// <summary>
    /// Represents resource usage statistics.
    /// </summary>
    public class ResourceUsage
    {
        /// <summary>
        /// Memory usage in bytes.
        /// </summary>
        public long MemoryUsed { get; set; }

        /// <summary>
        /// CPU time used in milliseconds.
        /// </summary>
        public long CpuTimeUsed { get; set; }

        /// <summary>
        /// Number of threads created.
        /// </summary>
        public int ThreadsCreated { get; set; }

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public long ExecutionTime { get; set; }
    }

    /// <summary>
    /// Defines the interface for plugin sandboxes that provide security isolation.
    /// </summary>
    public interface IPluginSandbox : IDisposable
    {
        /// <summary>
        /// Gets the type of sandbox implementation.
        /// </summary>
        SandboxType Type { get; }

        /// <summary>
        /// Gets a value indicating whether the sandbox is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Initializes the sandbox with the specified security policy.
        /// </summary>
        /// <param name="policy">The security policy to apply.</param>
        /// <returns>A task representing the initialization operation.</returns>
        Task InitializeAsync(PluginSecurityPolicy policy);

        /// <summary>
        /// Executes an action within the sandbox.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>A task representing the sandboxed execution.</returns>
        Task<SandboxResult> ExecuteAsync(Func<object> action);

        /// <summary>
        /// Executes an asynchronous action within the sandbox.
        /// </summary>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <returns>A task representing the sandboxed execution.</returns>
        Task<SandboxResult> ExecuteAsync(Func<Task<object>> action);

        /// <summary>
        /// Validates if a specific permission is allowed.
        /// </summary>
        /// <param name="permission">The permission to validate.</param>
        /// <returns>True if the permission is allowed; otherwise, false.</returns>
        bool ValidatePermission(PluginPermissions permission);

        /// <summary>
        /// Gets the current resource usage of the sandbox.
        /// </summary>
        /// <returns>The current resource usage statistics.</returns>
        ResourceUsage GetResourceUsage();

        /// <summary>
        /// Suspends the sandbox execution.
        /// </summary>
        void Suspend();

        /// <summary>
        /// Resumes the sandbox execution.
        /// </summary>
        void Resume();

        /// <summary>
        /// Terminates the sandbox forcefully.
        /// </summary>
        void Terminate();
    }

    /// <summary>
    /// Defines the types of sandbox implementations available.
    /// </summary>
    public enum SandboxType
    {
        /// <summary>
        /// No isolation - passes through all operations.
        /// </summary>
        PassThrough,

        /// <summary>
        /// .NET Core/5+ AssemblyLoadContext isolation.
        /// </summary>
        AssemblyLoadContext,

        /// <summary>
        /// OS process-level isolation.
        /// </summary>
        Process,

        /// <summary>
        /// Container-based isolation using Docker/OCI.
        /// </summary>
        Container
    }
}
