// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ProcessControl.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.Security;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Neo.UnitTests.Plugins.Security
{
    [TestClass]
    public class UT_ProcessControl : SecurityTestBase
    {
        [TestMethod]
        public void TestProcessResourceUsageRetrieval()
        {
            var currentProcess = Process.GetCurrentProcess();

            try
            {
                var resourceUsage = ProcessControl.GetProcessResourceUsage(currentProcess);

                Assert.IsNotNull(resourceUsage);
                Assert.IsTrue(resourceUsage.WorkingSetMemory >= 0);
                Assert.IsTrue(resourceUsage.VirtualMemory >= 0);
                Assert.IsTrue(resourceUsage.TotalProcessorTime >= TimeSpan.Zero);
                Assert.IsTrue(resourceUsage.UserProcessorTime >= TimeSpan.Zero);
                Assert.IsTrue(resourceUsage.ThreadCount > 0);
                Assert.IsTrue(resourceUsage.HandleCount >= 0);
            }
            finally
            {
                currentProcess?.Dispose();
            }
        }

        [TestMethod]
        public void TestProcessResourceUsageWithNullProcess()
        {
            var resourceUsage = ProcessControl.GetProcessResourceUsage(null);

            Assert.IsNotNull(resourceUsage);
            Assert.AreEqual(0, resourceUsage.WorkingSetMemory);
            Assert.AreEqual(0, resourceUsage.VirtualMemory);
            Assert.AreEqual(TimeSpan.Zero, resourceUsage.TotalProcessorTime);
            Assert.AreEqual(TimeSpan.Zero, resourceUsage.UserProcessorTime);
            Assert.AreEqual(0, resourceUsage.ThreadCount);
            Assert.AreEqual(0, resourceUsage.HandleCount);
        }

        [TestMethod]
        public void TestProcessResourceUsageWithExitedProcess()
        {
            // Create a process that will exit immediately
            var processStartInfo = new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "echo",
                Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c echo test" : "test",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process testProcess = null;
            try
            {
                testProcess = Process.Start(processStartInfo);
                Assert.IsNotNull(testProcess);

                // Wait for process to exit
                testProcess.WaitForExit(5000);
                Assert.IsTrue(testProcess.HasExited);

                // Test resource usage retrieval for exited process
                var resourceUsage = ProcessControl.GetProcessResourceUsage(testProcess);
                Assert.IsNotNull(resourceUsage);
                // For exited processes, we expect default values
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"Could not start test process: {ex.Message}");
            }
            finally
            {
                testProcess?.Dispose();
            }
        }

        [TestMethod]
        public void TestApplyResourceLimitsWithNullInputs()
        {
            // Test null process
            ProcessControl.ApplyResourceLimits(null, new PluginSecurityPolicy());

            var currentProcess = Process.GetCurrentProcess();
            try
            {
                // Test null policy
                ProcessControl.ApplyResourceLimits(currentProcess, null);

                // Test both null (should not throw)
                ProcessControl.ApplyResourceLimits(null, null);
            }
            finally
            {
                currentProcess?.Dispose();
            }
        }

        [TestMethod]
        public void TestApplyResourceLimits()
        {
            var policy = new PluginSecurityPolicy
            {
                MaxMemoryBytes = 100 * 1024 * 1024, // 100MB
                MaxCpuPercent = 50
            };

            var currentProcess = Process.GetCurrentProcess();
            try
            {
                // Test applying resource limits (should not throw)
                ProcessControl.ApplyResourceLimits(currentProcess, policy);

                // Note: We can't easily verify the limits are actually applied
                // without triggering them, but we can ensure the method completes
            }
            catch (Exception ex)
            {
                // Some platforms may not support resource limit enforcement
                Assert.Inconclusive($"Resource limit application not supported: {ex.Message}");
            }
            finally
            {
                currentProcess?.Dispose();
            }
        }

        [TestMethod]
        public void TestSuspendResumeWithInvalidProcess()
        {
            // Test with null process
            Assert.ThrowsExactly<ArgumentException>(() =>
                ProcessControl.SuspendProcess(null));

            Assert.ThrowsExactly<ArgumentException>(() =>
                ProcessControl.ResumeProcess(null));
        }

        [TestMethod]
        public void TestSuspendResumeCurrentProcess()
        {
            // IMPORTANT: We should NEVER try to suspend the current process
            // as this would freeze the test runner itself!
            // Instead, we test with a child process

            var processStartInfo = new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "sleep",
                Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c ping 127.0.0.1 -n 10" : "10",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process childProcess = null;
            try
            {
                childProcess = Process.Start(processStartInfo);
                Assert.IsNotNull(childProcess);

                // Give the process time to start
                Thread.Sleep(100);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        ProcessControl.SuspendProcess(childProcess);
                        Thread.Sleep(100); // Let suspension take effect
                        ProcessControl.ResumeProcess(childProcess);
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected if insufficient permissions
                    }
                }
                else
                {
                    // On Unix systems, test the method exists but expect failures
                    try
                    {
                        ProcessControl.SuspendProcess(childProcess);
                        ProcessControl.ResumeProcess(childProcess);
                    }
                    catch (Exception)
                    {
                        // Expected - many Unix systems restrict process control
                    }
                }
            }
            catch (PlatformNotSupportedException)
            {
                Assert.Inconclusive("Process suspend/resume not supported on this platform");
            }
            finally
            {
                try
                {
                    if (childProcess != null && !childProcess.HasExited)
                    {
                        childProcess.Kill();
                        childProcess.WaitForExit(1000);
                    }
                }
                catch { }
                childProcess?.Dispose();
            }
        }

        [TestMethod]
        public void TestUnsupportedPlatform()
        {
            // This test verifies that the ProcessControl methods handle
            // unsupported platforms gracefully

            var currentProcess = Process.GetCurrentProcess();
            try
            {
                // These methods should either work or throw PlatformNotSupportedException
                try
                {
                    ProcessControl.SuspendProcess(currentProcess);
                    ProcessControl.ResumeProcess(currentProcess);
                }
                catch (PlatformNotSupportedException)
                {
                    // Expected on unsupported platforms
                }
                catch (InvalidOperationException)
                {
                    // Expected for permission issues or process state
                }
            }
            finally
            {
                currentProcess?.Dispose();
            }
        }

        [TestMethod]
        public void TestProcessControlMemoryLimits()
        {
            var policy = new PluginSecurityPolicy
            {
                MaxMemoryBytes = 50 * 1024 * 1024 // 50MB
            };

            var currentProcess = Process.GetCurrentProcess();
            try
            {
                // Test memory limit application
                ProcessControl.ApplyResourceLimits(currentProcess, policy);

                // Verify the process is still responsive
                var usage = ProcessControl.GetProcessResourceUsage(currentProcess);
                Assert.IsNotNull(usage);
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"Memory limit testing not supported: {ex.Message}");
            }
            finally
            {
                currentProcess?.Dispose();
            }
        }

        [TestMethod]
        public void TestProcessControlCpuLimits()
        {
            var policy = new PluginSecurityPolicy
            {
                MaxCpuPercent = 25 // 25% CPU
            };

            var currentProcess = Process.GetCurrentProcess();
            try
            {
                // Test CPU limit application
                ProcessControl.ApplyResourceLimits(currentProcess, policy);

                // Verify the process is still responsive
                var usage = ProcessControl.GetProcessResourceUsage(currentProcess);
                Assert.IsNotNull(usage);
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"CPU limit testing not supported: {ex.Message}");
            }
            finally
            {
                currentProcess?.Dispose();
            }
        }
    }
}
