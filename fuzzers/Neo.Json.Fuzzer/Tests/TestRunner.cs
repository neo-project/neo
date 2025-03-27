// Copyright (C) 2015-2025 The Neo Project.
//
// TestRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Json.Fuzzer.Tests
{
    /// <summary>
    /// Runner for executing tests on the refactored MutationEngine
    /// </summary>
    public class TestRunner
    {
        /// <summary>
        /// Runs all tests for the refactored MutationEngine
        /// </summary>
        public static void RunTests()
        {
            Console.WriteLine("=== Neo.Json.Fuzzer Test Runner ===");
            Console.WriteLine("Testing the refactored MutationEngine components");
            Console.WriteLine();
            
            try
            {
                // Run the MutationEngine tests
                var mutationEngineTests = new MutationEngineTests();
                mutationEngineTests.RunTests();
                
                Console.WriteLine();
                Console.WriteLine("All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Test execution failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
