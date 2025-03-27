// Copyright (C) 2015-2025 The Neo Project.
//
// ConcurrentAccessMutations.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Provides concurrent access-specific mutation strategies for the mutation engine
    /// </summary>
    public class ConcurrentAccessMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;

        // Constants for concurrent access testing
        private const int MAX_CONCURRENT_OPERATIONS = 10;
        private const int MAX_SHARED_OBJECTS = 5;

        /// <summary>
        /// Initializes a new instance of the ConcurrentAccessMutations class
        /// </summary>
        public ConcurrentAccessMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Applies a concurrent access-specific mutation to the JSON
        /// </summary>
        public string ApplyConcurrentAccessMutation(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                // Select a mutation strategy
                int strategy = _random.Next(4);

                return strategy switch
                {
                    0 => GenerateSharedObjectStructure(token),
                    1 => GenerateRaceConditionTest(token),
                    2 => GenerateParallelProcessingTest(token),
                    _ => GenerateConcurrentModificationTest(token)
                };
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Generates a structure with shared objects for concurrent access testing
        /// </summary>
        private string GenerateSharedObjectStructure(JToken? token)
        {
            if (token == null) return "{}";

            // Create a structure with shared references
            JObject root = new JObject();

            // Create a set of shared objects
            int sharedCount = _random.Next(2, MAX_SHARED_OBJECTS + 1);
            List<JObject> sharedObjects = new List<JObject>();

            for (int i = 0; i < sharedCount; i++)
            {
                JObject shared = new JObject
                {
                    ["id"] = new JString($"shared_{i}"),
                    ["value"] = new JNumber(_random.Next(1000)),
                    ["timestamp"] = new JString(DateTime.UtcNow.ToString("o"))
                };

                sharedObjects.Add(shared);
            }

            // Create references to these shared objects
            JArray references = new JArray();

            for (int i = 0; i < _random.Next(5, 15); i++)
            {
                // Select a random shared object
                JObject shared = sharedObjects[_random.Next(sharedObjects.Count)];

                // Create a reference wrapper
                JObject reference = new JObject
                {
                    ["refId"] = new JString($"ref_{i}"),
                    ["target"] = shared
                };

                references.Add(reference);
            }

            root["sharedObjects"] = new JArray(sharedObjects);
            root["references"] = references;

            return root.ToString();
        }

        /// <summary>
        /// Generates a structure designed to test for race conditions
        /// </summary>
        private string GenerateRaceConditionTest(JToken? token)
        {
            if (token == null) return "{}";

            // Create a structure that might trigger race conditions
            JObject root = new JObject();

            // Add a counter object that might be accessed concurrently
            root["counter"] = new JObject
            {
                ["value"] = new JNumber(0),
                ["lastUpdated"] = new JString(DateTime.UtcNow.ToString("o")),
                ["updates"] = new JArray()
            };

            // Add a collection of operations
            JArray operations = new JArray();

            for (int i = 0; i < _random.Next(5, 20); i++)
            {
                operations.Add(new JObject
                {
                    ["id"] = new JString($"op_{i}"),
                    ["type"] = new JString(_random.Next(2) == 0 ? "increment" : "decrement"),
                    ["amount"] = new JNumber(_random.Next(1, 10)),
                    ["delay"] = new JNumber(_random.Next(10, 100))
                });
            }

            root["operations"] = operations;

            return root.ToString();
        }

        /// <summary>
        /// Generates a structure designed to test parallel processing
        /// </summary>
        private string GenerateParallelProcessingTest(JToken? token)
        {
            if (token == null) return "{}";

            // Create a structure for parallel processing tests
            JObject root = new JObject();

            // Create a set of independent tasks
            JArray tasks = new JArray();

            for (int i = 0; i < _random.Next(5, 20); i++)
            {
                tasks.Add(new JObject
                {
                    ["id"] = new JString($"task_{i}"),
                    ["priority"] = new JNumber(_random.Next(1, 10)),
                    ["data"] = new JString(GenerateRandomString(_random.Next(10, 100))),
                    ["processingTime"] = new JNumber(_random.Next(10, 500))
                });
            }

            root["tasks"] = tasks;

            // Add configuration for parallel processing
            root["parallelConfig"] = new JObject
            {
                ["maxThreads"] = new JNumber(_random.Next(2, 8)),
                ["timeout"] = new JNumber(_random.Next(1000, 5000)),
                ["retryCount"] = new JNumber(_random.Next(0, 3))
            };

            return root.ToString();
        }

        /// <summary>
        /// Generates a structure designed to test concurrent modifications
        /// </summary>
        private string GenerateConcurrentModificationTest(JToken? token)
        {
            if (token == null) return "{}";

            // Create a structure for testing concurrent modifications
            JObject root = new JObject();

            // Create a shared data structure
            JObject sharedData = new JObject();

            // Add some initial properties
            for (int i = 0; i < _random.Next(5, 15); i++)
            {
                sharedData[$"prop_{i}"] = new JString(GenerateRandomString(_random.Next(5, 20)));
            }

            root["sharedData"] = sharedData;

            // Create a set of modification operations
            JArray modifications = new JArray();

            for (int i = 0; i < _random.Next(10, 30); i++)
            {
                // Randomly select an operation type
                string opType = _random.Next(3) switch
                {
                    0 => "add",
                    1 => "update",
                    _ => "remove"
                };

                // Create the modification operation
                JObject operation = new JObject
                {
                    ["id"] = new JString($"mod_{i}"),
                    ["type"] = new JString(opType),
                    ["key"] = new JString($"prop_{_random.Next(20)}"), // Might conflict with existing or other operations
                    ["delay"] = new JNumber(_random.Next(10, 100))
                };

                if (opType != "remove")
                {
                    operation["value"] = new JString(GenerateRandomString(_random.Next(5, 20)));
                }

                modifications.Add(operation);
            }

            root["modifications"] = modifications;

            return root.ToString();
        }

        /// <summary>
        /// Simulates concurrent access to a JSON structure
        /// </summary>
        public async Task<string> SimulateConcurrentAccessAsync(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                if (token is JObject rootObj)
                {
                    // Create a set of concurrent operations
                    int operationCount = _random.Next(2, MAX_CONCURRENT_OPERATIONS + 1);
                    List<Task> tasks = new List<Task>();

                    // Shared result to track modifications
                    JArray results = new JArray();

                    // Create and start concurrent tasks
                    for (int i = 0; i < operationCount; i++)
                    {
                        int taskId = i;
                        tasks.Add(Task.Run(async () =>
                        {
                            // Simulate some processing time
                            await Task.Delay(_random.Next(10, 100));

                            // Perform a modification
                            string operationType = _random.Next(3) switch
                            {
                                0 => "read",
                                1 => "update",
                                _ => "add"
                            };

                            // Track the operation
                            JObject result = new JObject
                            {
                                ["taskId"] = new JString($"task_{taskId}"),
                                ["operation"] = new JString(operationType),
                                ["timestamp"] = new JString(DateTime.UtcNow.ToString("o"))
                            };

                            // Thread-safe add to results
                            lock (results)
                            {
                                results.Add(result);
                            }
                        }));
                    }

                    // Wait for all tasks to complete
                    await Task.WhenAll(tasks);

                    // Add the results to the original object
                    rootObj["concurrentResults"] = results;

                    return rootObj.ToString();
                }

                return json;
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        /// <summary>
        /// Generates a random string of specified length
        /// </summary>
        private string GenerateRandomString(int length)
        {
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                // Use ASCII printable characters
                char c = (char)_random.Next(32, 127);
                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a specialized test JSON for concurrent access scenarios
        /// </summary>
        public string GenerateSpecializedTestJson(string? testType = null)
        {
            return testType?.ToLowerInvariant() switch
            {
                "shared_objects" => GenerateSharedObjectStructure(null),
                "race_condition" => GenerateRaceConditionTest(null),
                "parallel_processing" => GenerateParallelProcessingTest(null),
                "concurrent_modification" => GenerateConcurrentModificationTest(null),
                _ => GenerateSharedObjectStructure(null) // Default to shared objects
            };
        }
    }
}
