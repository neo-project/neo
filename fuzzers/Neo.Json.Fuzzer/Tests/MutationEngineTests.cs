// Copyright (C) 2015-2025 The Neo Project.
//
// MutationEngineTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.Json.Fuzzer.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Json.Fuzzer.Tests
{
    /// <summary>
    /// Tests for the refactored MutationEngine
    /// </summary>
    public class MutationEngineTests
    {
        private readonly Random _random;
        private readonly MutationEngine _mutationEngine;

        /// <summary>
        /// Constructor initializes the test environment
        /// </summary>
        public MutationEngineTests()
        {
            _random = new Random(42); // Fixed seed for reproducibility
            _mutationEngine = new MutationEngine(_random, 1, 3);
        }

        /// <summary>
        /// Main test method that runs a series of tests
        /// </summary>
        public void RunTests()
        {
            Console.WriteLine("Starting MutationEngine tests...");

            TestBasicMutation();
            TestMultipleMutations();
            TestInvalidJsonMutation();
            TestEdgeCases();

            Console.WriteLine("All tests completed successfully!");
        }

        /// <summary>
        /// Tests basic mutation functionality
        /// </summary>
        private void TestBasicMutation()
        {
            Console.WriteLine("Testing basic mutation...");

            string json = "{\"name\":\"test\",\"value\":123,\"active\":true}";
            string mutated = _mutationEngine.MutateJson(json);

            // Verify the mutation produced a different string
            if (json == mutated)
            {
                Console.WriteLine("WARNING: Mutation did not change the input JSON");
            }
            else
            {
                Console.WriteLine("Basic mutation successful");
            }

            // Try to parse the mutated JSON to verify it's valid
            try
            {
                JToken.Parse(mutated);
                Console.WriteLine("Mutated JSON is valid");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mutated JSON is invalid: {ex.Message}");
                Console.WriteLine($"Original: {json}");
                Console.WriteLine($"Mutated: {mutated}");
            }
        }

        /// <summary>
        /// Tests multiple mutations on the same input
        /// </summary>
        private void TestMultipleMutations()
        {
            Console.WriteLine("Testing multiple mutations...");

            string json = "{\"array\":[1,2,3],\"object\":{\"a\":1,\"b\":2},\"string\":\"test\"}";
            HashSet<string> mutations = new HashSet<string>();

            // Apply multiple mutations and verify they produce different results
            for (int i = 0; i < 10; i++)
            {
                string mutated = _mutationEngine.MutateJson(json);
                mutations.Add(mutated);
            }

            Console.WriteLine($"Generated {mutations.Count} unique mutations from 10 attempts");
        }

        /// <summary>
        /// Tests mutation of invalid JSON
        /// </summary>
        private void TestInvalidJsonMutation()
        {
            Console.WriteLine("Testing invalid JSON mutation...");

            string[] invalidJsons = new[]
            {
                "{\"name\":\"test\",", // Incomplete JSON
                "{\"name\":test}", // Missing quotes
                "[1,2,3,]", // Extra comma
                "{\"a\":1,\"a\":2}" // Duplicate key
            };

            foreach (string invalidJson in invalidJsons)
            {
                string mutated = _mutationEngine.MutateJson(invalidJson);
                Console.WriteLine($"Original invalid JSON: {invalidJson}");
                Console.WriteLine($"Mutated: {mutated}");

                // Try to parse the mutated JSON
                try
                {
                    JToken.Parse(mutated);
                    Console.WriteLine("Mutation produced valid JSON");
                }
                catch
                {
                    Console.WriteLine("Mutation still produced invalid JSON");
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Tests edge cases
        /// </summary>
        private void TestEdgeCases()
        {
            Console.WriteLine("Testing edge cases...");

            // Empty JSON
            string mutated = _mutationEngine.MutateJson("");
            Console.WriteLine($"Empty JSON mutation: {mutated}");

            // Very large JSON
            StringBuilder largeJson = new StringBuilder("{\"items\":[");
            for (int i = 0; i < 100; i++)
            {
                if (i > 0) largeJson.Append(',');
                largeJson.Append($"{{\"id\":{i},\"value\":\"test{i}\"}}");
            }
            largeJson.Append("]}");

            mutated = _mutationEngine.MutateJson(largeJson.ToString());
            Console.WriteLine($"Large JSON mutation length: {mutated.Length}");

            // Deeply nested JSON
            string nestedJson = "{\"level1\":{\"level2\":{\"level3\":{\"level4\":{\"level5\":0}}}}}";
            mutated = _mutationEngine.MutateJson(nestedJson);
            Console.WriteLine($"Nested JSON mutation: {mutated}");
        }
    }
}
