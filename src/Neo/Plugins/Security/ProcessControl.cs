// Copyright (C) 2015-2025 The Neo Project.
//
// ProcessControl.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Provides platform-specific process control capabilities.
    /// </summary>
    public static class ProcessControl
    {
        #region Windows P/Invoke declarations

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll")]
        private static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoType infoType,
            IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [Flags]
        private enum ThreadAccess : int
        {
            SuspendResume = 0x0002,
            GetContext = 0x0008,
            SetContext = 0x0010,
            QueryInformation = 0x0040
        }

        private enum JobObjectInfoType
        {
            ExtendedLimitInformation = 9,
            CpuRateControlInformation = 15
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        private const uint JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100;
        private const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x00000200;
        private const uint JOB_OBJECT_LIMIT_CPU = 0x00000004;
        private const uint JOB_OBJECT_LIMIT_PROCESS_TIME = 0x00000002;

        #endregion

        /// <summary>
        /// Suspends a process on the current platform.
        /// </summary>
        public static void SuspendProcess(Process process)
        {
            if (process == null || process.HasExited)
                throw new ArgumentException("Process is null or has exited");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SuspendProcessWindows(process);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                SuspendProcessUnix(process);
            }
            else
            {
                throw new PlatformNotSupportedException("Process suspension is not supported on this platform");
            }
        }

        /// <summary>
        /// Resumes a suspended process on the current platform.
        /// </summary>
        public static void ResumeProcess(Process process)
        {
            if (process == null || process.HasExited)
                throw new ArgumentException("Process is null or has exited");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ResumeProcessWindows(process);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ResumeProcessUnix(process);
            }
            else
            {
                throw new PlatformNotSupportedException("Process resumption is not supported on this platform");
            }
        }

        /// <summary>
        /// Applies resource limits to a process.
        /// </summary>
        public static void ApplyResourceLimits(Process process, PluginSecurityPolicy policy)
        {
            if (process == null || policy == null) return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ApplyWindowsResourceLimits(process, policy);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ApplyLinuxResourceLimits(process, policy);
            }
            // macOS resource limits are more limited
        }

        private static void SuspendProcessWindows(Process process)
        {
            var suspendedThreads = new List<IntPtr>();

            try
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    var hThread = OpenThread(ThreadAccess.SuspendResume, false, (uint)thread.Id);
                    if (hThread != IntPtr.Zero)
                    {
                        if (SuspendThread(hThread) != uint.MaxValue) // -1 indicates error
                        {
                            suspendedThreads.Add(hThread);
                        }
                        else
                        {
                            CloseHandle(hThread);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Resume any threads we managed to suspend before the error
                foreach (var hThread in suspendedThreads)
                {
                    try
                    {
                        ResumeThread(hThread);
                        CloseHandle(hThread);
                    }
                    catch { }
                }

                throw new InvalidOperationException($"Failed to suspend process: {ex.Message}", ex);
            }
            finally
            {
                // Close all thread handles
                foreach (var hThread in suspendedThreads)
                {
                    try { CloseHandle(hThread); } catch { }
                }
            }
        }

        private static void ResumeProcessWindows(Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var hThread = OpenThread(ThreadAccess.SuspendResume, false, (uint)thread.Id);
                if (hThread != IntPtr.Zero)
                {
                    try
                    {
                        ResumeThread(hThread);
                    }
                    finally
                    {
                        CloseHandle(hThread);
                    }
                }
            }
        }

        private static void SuspendProcessUnix(Process process)
        {
            // Send SIGSTOP signal
            using (var killProcess = new Process())
            {
                killProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "kill",
                    Arguments = $"-STOP {process.Id}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                killProcess.Start();
                killProcess.WaitForExit();

                if (killProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to suspend process {process.Id}");
                }
            }
        }

        private static void ResumeProcessUnix(Process process)
        {
            // Send SIGCONT signal
            using (var killProcess = new Process())
            {
                killProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = "kill",
                    Arguments = $"-CONT {process.Id}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                killProcess.Start();
                killProcess.WaitForExit();

                if (killProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to resume process {process.Id}");
                }
            }
        }

        private static void ApplyWindowsResourceLimits(Process process, PluginSecurityPolicy policy)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            try
            {
                // Create a job object to enforce limits
                var jobHandle = CreateJobObject(IntPtr.Zero, null);
                if (jobHandle == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                try
                {
                    // Assign the process to the job
                    if (!AssignProcessToJobObject(jobHandle, process.Handle))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    // Set memory limits
                    if (policy.MaxMemoryBytes > 0)
                    {
                        var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
                        {
                            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                            {
                                LimitFlags = JOB_OBJECT_LIMIT_PROCESS_MEMORY
                            },
                            ProcessMemoryLimit = new UIntPtr((ulong)policy.MaxMemoryBytes)
                        };

                        var size = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                        var infoPtr = Marshal.AllocHGlobal(size);
                        try
                        {
                            Marshal.StructureToPtr(extendedInfo, infoPtr, false);

                            if (!SetInformationJobObject(jobHandle, JobObjectInfoType.ExtendedLimitInformation,
                                infoPtr, (uint)size))
                            {
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(infoPtr);
                        }
                    }

                    // Set CPU limits (simplified - Windows job objects have limited CPU control)
                    if (policy.MaxCpuPercent > 0)
                    {
                        // CPU rate control requires Windows 8.1 or later
                        // For now, we'll use process priority as a simple control
                        try
                        {
                            if (policy.MaxCpuPercent < 25)
                                process.PriorityClass = ProcessPriorityClass.Idle;
                            else if (policy.MaxCpuPercent < 50)
                                process.PriorityClass = ProcessPriorityClass.BelowNormal;
                            else
                                process.PriorityClass = ProcessPriorityClass.Normal;
                        }
                        catch
                        {
                            // Ignore priority setting errors
                        }
                    }
                }
                catch
                {
                    CloseHandle(jobHandle);
                    throw;
                }

                // Note: Job handle is not closed - it will be cleaned up when process exits
            }
            catch (Exception ex)
            {
                Utility.Log("ProcessControl", LogLevel.Warning,
                    $"Failed to apply Windows resource limits: {ex.Message}");
            }
        }

        private static void ApplyLinuxResourceLimits(Process process, PluginSecurityPolicy policy)
        {
            try
            {
                // Use cgroups v2 if available
                var cgroupPath = $"/sys/fs/cgroup/neo-sandbox-{process.Id}";

                if (Directory.Exists("/sys/fs/cgroup"))
                {
                    // Create cgroup for this process
                    Directory.CreateDirectory(cgroupPath);

                    // Set memory limit
                    if (policy.MaxMemoryBytes > 0)
                    {
                        File.WriteAllText($"{cgroupPath}/memory.max", policy.MaxMemoryBytes.ToString());
                    }

                    // Set CPU limit (as percentage of one CPU)
                    if (policy.MaxCpuPercent > 0)
                    {
                        var cpuQuota = (int)(policy.MaxCpuPercent * 1000); // Convert percentage to microseconds
                        File.WriteAllText($"{cgroupPath}/cpu.max", $"{cpuQuota} 100000");
                    }

                    // Add process to cgroup
                    File.WriteAllText($"{cgroupPath}/cgroup.procs", process.Id.ToString());
                }
                else
                {
                    // Fallback to ulimit
                    using (var bashProcess = new Process())
                    {
                        bashProcess.StartInfo = new ProcessStartInfo
                        {
                            FileName = "bash",
                            Arguments = $"-c \"ulimit -v {policy.MaxMemoryBytes / 1024}; ulimit -t {policy.MaxCpuPercent}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        bashProcess.Start();
                        bashProcess.WaitForExit();
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log("ProcessControl", LogLevel.Warning,
                    $"Failed to apply Linux resource limits: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets detailed resource usage for a process.
        /// </summary>
        public static ProcessResourceUsage GetProcessResourceUsage(Process process)
        {
            if (process == null || process.HasExited)
                return new ProcessResourceUsage();

            try
            {
                process.Refresh();

                return new ProcessResourceUsage
                {
                    WorkingSetMemory = process.WorkingSet64,
                    VirtualMemory = process.VirtualMemorySize64,
                    TotalProcessorTime = process.TotalProcessorTime,
                    UserProcessorTime = process.UserProcessorTime,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount
                };
            }
            catch
            {
                return new ProcessResourceUsage();
            }
        }
    }

    /// <summary>
    /// Represents detailed process resource usage.
    /// </summary>
    public class ProcessResourceUsage
    {
        public long WorkingSetMemory { get; set; }
        public long VirtualMemory { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
        public TimeSpan UserProcessorTime { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
    }
}
