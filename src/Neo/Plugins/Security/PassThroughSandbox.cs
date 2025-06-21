// Copyright (C) 2015-2025 The Neo Project.
//
// PassThroughSandbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// A sandbox implementation that provides no isolation (pass-through).
    /// Used for trusted plugins where security overhead is not desired.
    /// </summary>
    public class PassThroughSandbox : IPluginSandbox
    {
        private PluginSecurityPolicy _policy;
        private bool _disposed = false;

        // Resource tracking fields
        private DateTime _initializationTime;
        private long _baselineMemory;
        private TimeSpan _baselineCpuTime;
#if NET5_0_OR_GREATER
        private int _baselineThreadCount;
#endif
        private readonly object _resourceLock = new object();

        /// <inheritdoc />
        public SandboxType Type => SandboxType.PassThrough;

        /// <inheritdoc />
        public bool IsActive { get; private set; }

        /// <inheritdoc />
        public Task InitializeAsync(PluginSecurityPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));

            // Establish baseline resource measurements
            lock (_resourceLock)
            {
                _initializationTime = DateTime.UtcNow;
                _baselineMemory = GC.GetTotalMemory(false);

                try
                {
                    var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                    _baselineCpuTime = currentProcess.TotalProcessorTime;
                    currentProcess.Dispose();
                }
                catch
                {
                    _baselineCpuTime = TimeSpan.Zero;
                }

#if NET5_0_OR_GREATER
                try
                {
                    _baselineThreadCount = System.Threading.ThreadPool.ThreadCount;
                }
                catch
                {
                    _baselineThreadCount = 0;
                }
#endif
            }

            IsActive = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<object> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            TimeSpan startCpuTime = TimeSpan.Zero;
#if NET5_0_OR_GREATER
            int startThreads = 0;
#endif

            // Capture baseline metrics for this execution
            try
            {
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                startCpuTime = currentProcess.TotalProcessorTime;
                currentProcess.Dispose();
            }
            catch { }

#if NET5_0_OR_GREATER
            try
            {
                startThreads = System.Threading.ThreadPool.ThreadCount;
            }
            catch { }
#endif

            var result = new SandboxResult
            {
                ResourceUsage = new ResourceUsage()
            };

            try
            {
                var actionResult = action();
                result.Success = true;
                result.Result = actionResult;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var endMemory = GC.GetTotalMemory(false);

                // Calculate resource usage for this execution
                result.ResourceUsage.ExecutionTime = (long)(endTime - startTime).TotalMilliseconds;
                result.ResourceUsage.MemoryUsed = Math.Max(0, endMemory - startMemory);

                try
                {
                    var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                    var endCpuTime = currentProcess.TotalProcessorTime;
                    result.ResourceUsage.CpuTimeUsed = (long)(endCpuTime - startCpuTime).TotalMilliseconds;
                    currentProcess.Dispose();
                }
                catch
                {
                    // Estimate CPU time if we can't measure it
                    result.ResourceUsage.CpuTimeUsed = result.ResourceUsage.ExecutionTime / Environment.ProcessorCount;
                }

#if NET5_0_OR_GREATER
                try
                {
                    var endThreads = System.Threading.ThreadPool.ThreadCount;
                    result.ResourceUsage.ThreadsCreated = Math.Max(0, endThreads - startThreads);
                }
                catch
                {
                    result.ResourceUsage.ThreadsCreated = 0;
                }
#else
                result.ResourceUsage.ThreadsCreated = 0;
#endif
            }

            return await Task.FromResult(result);
        }

        /// <inheritdoc />
        public async Task<SandboxResult> ExecuteAsync(Func<Task<object>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var startTime = DateTime.UtcNow;
            var startMemory = GC.GetTotalMemory(false);
            TimeSpan startCpuTime = TimeSpan.Zero;
#if NET5_0_OR_GREATER
            int startThreads = 0;
#endif

            // Capture baseline metrics for this execution
            try
            {
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                startCpuTime = currentProcess.TotalProcessorTime;
                currentProcess.Dispose();
            }
            catch { }

#if NET5_0_OR_GREATER
            try
            {
                startThreads = System.Threading.ThreadPool.ThreadCount;
            }
            catch { }
#endif

            var result = new SandboxResult
            {
                ResourceUsage = new ResourceUsage()
            };

            try
            {
                var actionResult = await action();
                result.Success = true;
                result.Result = actionResult;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var endMemory = GC.GetTotalMemory(false);

                // Calculate resource usage for this execution
                result.ResourceUsage.ExecutionTime = (long)(endTime - startTime).TotalMilliseconds;
                result.ResourceUsage.MemoryUsed = Math.Max(0, endMemory - startMemory);

                try
                {
                    var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                    var endCpuTime = currentProcess.TotalProcessorTime;
                    result.ResourceUsage.CpuTimeUsed = (long)(endCpuTime - startCpuTime).TotalMilliseconds;
                    currentProcess.Dispose();
                }
                catch
                {
                    // Estimate CPU time if we can't measure it
                    result.ResourceUsage.CpuTimeUsed = result.ResourceUsage.ExecutionTime / Environment.ProcessorCount;
                }

#if NET5_0_OR_GREATER
                try
                {
                    var endThreads = System.Threading.ThreadPool.ThreadCount;
                    result.ResourceUsage.ThreadsCreated = Math.Max(0, endThreads - startThreads);
                }
                catch
                {
                    result.ResourceUsage.ThreadsCreated = 0;
                }
#else
                result.ResourceUsage.ThreadsCreated = 0;
#endif
            }

            return result;
        }

        /// <inheritdoc />
        public bool ValidatePermission(PluginPermissions permission)
        {
            // PassThrough sandbox still respects permission boundaries
            if (_policy == null)
                return false;

            // Always check against max permissions regardless of StrictMode
            return (_policy.MaxPermissions & permission) == permission;
        }

        /// <inheritdoc />
        public ResourceUsage GetResourceUsage()
        {
            lock (_resourceLock)
            {
                var currentTime = DateTime.UtcNow;
                var currentMemory = GC.GetTotalMemory(false);

                // Calculate memory usage since initialization (approximation)
                var memoryUsed = Math.Max(0, currentMemory - _baselineMemory);

                // Calculate execution time since initialization
                var executionTime = (long)(currentTime - _initializationTime).TotalMilliseconds;

                // Calculate CPU time usage (approximation)
                long cpuTimeUsed = 0;
                try
                {
                    var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                    var currentCpuTime = currentProcess.TotalProcessorTime;
                    cpuTimeUsed = (long)(currentCpuTime - _baselineCpuTime).TotalMilliseconds;
                    currentProcess.Dispose();
                }
                catch
                {
                    // If we can't get CPU time, estimate based on execution time
                    cpuTimeUsed = executionTime / Environment.ProcessorCount;
                }

                // Calculate thread count (approximation)
                int currentThreads = 0;
#if NET5_0_OR_GREATER
                try
                {
                    var totalThreads = System.Threading.ThreadPool.ThreadCount;
                    currentThreads = Math.Max(0, totalThreads - _baselineThreadCount);
                }
                catch
                {
                    currentThreads = 1; // Assume at least one thread for the plugin
                }
#else
                currentThreads = 1; // Assume at least one thread for the plugin
#endif

                return new ResourceUsage
                {
                    MemoryUsed = memoryUsed,
                    CpuTimeUsed = cpuTimeUsed,
                    ThreadsCreated = currentThreads,
                    ExecutionTime = executionTime
                };
            }
        }

        /// <inheritdoc />
        public void Suspend()
        {
            // PassThrough sandbox doesn't support suspension
            // This is a no-op for compatibility
        }

        /// <inheritdoc />
        public void Resume()
        {
            // PassThrough sandbox doesn't support suspension/resumption
            // This is a no-op for compatibility
        }

        /// <inheritdoc />
        public void Terminate()
        {
            IsActive = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                IsActive = false;
                _disposed = true;
            }
        }
    }
}
