// Copyright (C) 2015-2025 The Neo Project.
//
// SecurityTestSetup.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests.Plugins.Security
{
    /// <summary>
    /// Assembly-level test initialization for security tests.
    /// </summary>
    [TestClass]
    public static class SecurityTestSetup
    {
        /// <summary>
        /// Sets up the test environment before any security tests run.
        /// </summary>
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            // Set environment variable to ensure test mode is detected
            Environment.SetEnvironmentVariable("DOTNET_TEST_MODE", "true");
            
            // Log test initialization
            context.WriteLine("Security test environment initialized");
            context.WriteLine($"Test mode environment variable set: {Environment.GetEnvironmentVariable("DOTNET_TEST_MODE")}");
            context.WriteLine($"Process name: {System.Diagnostics.Process.GetCurrentProcess().ProcessName}");
        }

        /// <summary>
        /// Cleans up after all security tests complete.
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            // Clear the test mode environment variable
            Environment.SetEnvironmentVariable("DOTNET_TEST_MODE", null);
        }
    }
}