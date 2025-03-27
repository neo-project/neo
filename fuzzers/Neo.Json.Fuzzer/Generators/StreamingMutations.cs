// Copyright (C) 2015-2025 The Neo Project.
//
// StreamingMutations.cs file belongs to the neo project and is free
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

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Provides streaming-specific mutation strategies for the mutation engine
    /// </summary>
    public class StreamingMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;

        // Constants for streaming mutations
        private const int MAX_CHUNK_SIZE = 1024;
        private const int MAX_ARRAY_ELEMENTS = 100;
        private const int MAX_OBJECT_PROPERTIES = 50;

        /// <summary>
        /// Initializes a new instance of the StreamingMutations class
        /// </summary>
        public StreamingMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Applies a streaming-specific mutation to the JSON
        /// </summary>
        public string ApplyStreamingMutation(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                // Select a mutation strategy
                int strategy = _random.Next(5);

                return strategy switch
                {
                    0 => GenerateLargeArray(token),
                    1 => GenerateLargeObject(token),
                    2 => GenerateDeepNesting(),
                    3 => GenerateChunkedStructure(token),
                    _ => GenerateStreamingFriendlyStructure(token)
                };
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Generates a large array structure for streaming testing
        /// </summary>
        private string GenerateLargeArray(JToken? token)
        {
            if (token == null) return "[]";

            // Create a large array with many elements
            int elementCount = _random.Next(10, MAX_ARRAY_ELEMENTS);
            JArray array = new JArray();

            // Determine element type
            int elementType = _random.Next(3);

            for (int i = 0; i < elementCount; i++)
            {
                JToken element = elementType switch
                {
                    0 => new JString(GenerateRandomString(_random.Next(10, 100))),
                    1 => new JNumber(_random.Next(-1000, 1000)),
                    _ => i % 5 == 0 ? CreateNestedObject(3) : new JBoolean(_random.Next(2) == 0)
                };

                array.Add(element);
            }

            return array.ToString();
        }

        /// <summary>
        /// Generates a large object structure for streaming testing
        /// </summary>
        private string GenerateLargeObject(JToken? token)
        {
            if (token == null) return "{}";

            // Create a large object with many properties
            int propertyCount = _random.Next(10, MAX_OBJECT_PROPERTIES);
            JObject obj = new JObject();

            for (int i = 0; i < propertyCount; i++)
            {
                string key = $"property_{i}_{GenerateRandomString(5)}";

                JToken value = (i % 4) switch
                {
                    0 => new JString(GenerateRandomString(_random.Next(10, 100))),
                    1 => new JNumber(_random.Next(-1000, 1000)),
                    2 => new JBoolean(_random.Next(2) == 0),
                    _ => i % 8 == 0 ? CreateNestedArray(2) : JToken.Null
                };

                obj[key] = value;
            }

            return obj.ToString();
        }

        /// <summary>
        /// Generates a deeply nested structure for streaming testing
        /// </summary>
        private string GenerateDeepNesting()
        {
            // Create a deeply nested structure (alternating objects and arrays)
            int depth = _random.Next(5, 15);
            JToken result = CreateNestedStructure(depth);

            return result?.ToString() ?? "{}";
        }

        /// <summary>
        /// Generates a structure with multiple chunks for streaming testing
        /// </summary>
        private string GenerateChunkedStructure(JToken? token)
        {
            if (token == null) return "[]";

            // Create a structure that would likely be processed in chunks
            int chunkCount = _random.Next(3, 8);
            JArray array = new JArray();

            for (int i = 0; i < chunkCount; i++)
            {
                // Each chunk is either a large string or a complex object
                if (_random.Next(2) == 0)
                {
                    // Large string chunk
                    int chunkSize = _random.Next(MAX_CHUNK_SIZE / 2, MAX_CHUNK_SIZE);
                    array.Add(new JString(GenerateRandomString(chunkSize)));
                }
                else
                {
                    // Complex object chunk
                    JObject obj = new JObject();
                    int propertyCount = _random.Next(5, 20);

                    for (int j = 0; j < propertyCount; j++)
                    {
                        string key = $"chunk_{i}_prop_{j}";
                        JToken value = (j % 3) switch
                        {
                            0 => new JString(GenerateRandomString(_random.Next(10, 50))),
                            1 => new JNumber(_random.Next(-100, 100)),
                            _ => new JBoolean(_random.Next(2) == 0)
                        };

                        obj[key] = value;
                    }

                    array.Add(obj);
                }
            }

            return array.ToString();
        }

        /// <summary>
        /// Generates a structure specifically designed to test streaming parsers
        /// </summary>
        private string GenerateStreamingFriendlyStructure(JToken? token)
        {
            if (token == null) return "{}";

            // Create a structure with characteristics that test streaming parsers
            JObject root = new JObject();

            // 1. Add a property with a very long string value
            root["longString"] = new JString(GenerateRandomString(_random.Next(500, 1000)));

            // 2. Add a large array of simple values
            JArray simpleArray = new JArray();
            int arraySize = _random.Next(50, 200);

            for (int i = 0; i < arraySize; i++)
            {
                simpleArray.Add(new JNumber(i));
            }

            root["simpleArray"] = simpleArray;

            // 3. Add a property with a deeply nested structure
            root["nestedStructure"] = CreateNestedStructure(_random.Next(5, 10));

            // 4. Add a property with a mixed-type array
            JArray mixedArray = new JArray();
            int mixedSize = _random.Next(20, 50);

            for (int i = 0; i < mixedSize; i++)
            {
                JToken element = (i % 5) switch
                {
                    0 => new JString(GenerateRandomString(_random.Next(5, 20))),
                    1 => new JNumber(_random.Next(-100, 100)),
                    2 => new JBoolean(_random.Next(2) == 0),
                    3 => JToken.Null,
                    _ => new JObject { ["index"] = new JNumber(i) }
                };

                mixedArray.Add(element);
            }

            root["mixedArray"] = mixedArray;

            // 5. Add a property with whitespace-heavy content
            StringBuilder whitespaceBuilder = new StringBuilder();
            whitespaceBuilder.Append("\"");

            for (int i = 0; i < 100; i++)
            {
                whitespaceBuilder.Append(i % 10);
                whitespaceBuilder.Append("    \t\r\n");
            }

            whitespaceBuilder.Append("\"");

            root["whitespaceHeavy"] = JToken.Parse(whitespaceBuilder.ToString());

            return root.ToString();
        }

        /// <summary>
        /// Creates a nested structure with alternating objects and arrays
        /// </summary>
        private JToken CreateNestedStructure(int depth)
        {
            if (depth <= 0)
            {
                // Base case: return a simple value
                int valueType = _random.Next(3);

                return valueType switch
                {
                    0 => new JString(GenerateRandomString(_random.Next(5, 20))),
                    1 => new JNumber(_random.Next(-100, 100)),
                    _ => new JBoolean(_random.Next(2) == 0)
                };
            }

            // Alternate between objects and arrays
            if (depth % 2 == 0)
            {
                // Create a nested object
                JObject obj = new JObject();
                int propertyCount = _random.Next(2, 5);

                for (int i = 0; i < propertyCount; i++)
                {
                    string key = $"level_{depth}_prop_{i}";
                    obj[key] = CreateNestedStructure(depth - 1);
                }

                return obj;
            }
            else
            {
                // Create a nested array
                JArray array = new JArray();
                int elementCount = _random.Next(2, 5);

                for (int i = 0; i < elementCount; i++)
                {
                    array.Add(CreateNestedStructure(depth - 1));
                }

                return array;
            }
        }

        /// <summary>
        /// Creates a nested object with the specified depth
        /// </summary>
        private JObject CreateNestedObject(int depth)
        {
            JObject obj = new JObject();

            if (depth <= 0)
            {
                // Base case: add a few simple properties
                obj["name"] = new JString(GenerateRandomString(_random.Next(5, 10)));
                obj["value"] = new JNumber(_random.Next(100));
                obj["active"] = new JBoolean(_random.Next(2) == 0);

                return obj;
            }

            // Add some properties at this level
            int propertyCount = _random.Next(2, 5);

            for (int i = 0; i < propertyCount; i++)
            {
                string key = $"prop_{i}";

                if (i == 0 && depth > 1)
                {
                    // Add a nested object
                    obj[key] = CreateNestedObject(depth - 1);
                }
                else if (i == 1 && depth > 1)
                {
                    // Add a nested array
                    obj[key] = CreateNestedArray(depth - 1);
                }
                else
                {
                    // Add a simple value
                    int valueType = _random.Next(3);

                    JToken value = valueType switch
                    {
                        0 => new JString(GenerateRandomString(_random.Next(5, 10))),
                        1 => new JNumber(_random.Next(-100, 100)),
                        _ => new JBoolean(_random.Next(2) == 0)
                    };

                    obj[key] = value;
                }
            }

            return obj;
        }

        /// <summary>
        /// Creates a nested array with the specified depth
        /// </summary>
        private JArray CreateNestedArray(int depth)
        {
            JArray array = new JArray();

            if (depth <= 0)
            {
                // Base case: add a few simple elements
                int elementCount = _random.Next(2, 5);

                for (int i = 0; i < elementCount; i++)
                {
                    int valueType = _random.Next(3);

                    JToken value = valueType switch
                    {
                        0 => new JString(GenerateRandomString(_random.Next(5, 10))),
                        1 => new JNumber(_random.Next(-100, 100)),
                        _ => new JBoolean(_random.Next(2) == 0)
                    };

                    array.Add(value);
                }

                return array;
            }

            // Add some elements at this level
            int count = _random.Next(2, 5);

            for (int i = 0; i < count; i++)
            {
                if (i == 0 && depth > 1)
                {
                    // Add a nested object
                    array.Add(CreateNestedObject(depth - 1));
                }
                else if (i == 1 && depth > 1)
                {
                    // Add a nested array
                    array.Add(CreateNestedArray(depth - 1));
                }
                else
                {
                    // Add a simple value
                    int valueType = _random.Next(3);

                    JToken value = valueType switch
                    {
                        0 => new JString(GenerateRandomString(_random.Next(5, 10))),
                        1 => new JNumber(_random.Next(-100, 100)),
                        _ => new JBoolean(_random.Next(2) == 0)
                    };

                    array.Add(value);
                }
            }

            return array;
        }

        /// <summary>
        /// Generates a random string of the specified length
        /// </summary>
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            char[] stringChars = new char[length];

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[_random.Next(chars.Length)];
            }

            return new string(stringChars);
        }

        /// <summary>
        /// Generates a JSON structure specifically designed for streaming parser testing
        /// </summary>
        public string GenerateStreamingTestJson(string? type = null)
        {
            type = type?.ToLowerInvariant();

            return type switch
            {
                "large_array" => GenerateLargeArray(null),
                "large_object" => GenerateLargeObject(null),
                "deep_nesting" => GenerateDeepNesting(),
                "chunked" => GenerateChunkedStructure(null),
                "streaming_friendly" => GenerateStreamingFriendlyStructure(null),
                _ => GenerateStreamingFriendlyStructure(null)
            };
        }

        /// <summary>
        /// Generates a specialized test JSON for streaming testing
        /// </summary>
        public string GenerateSpecializedTestJson(string? testType = null)
        {
            JObject obj = new JObject();

            switch (testType?.ToLowerInvariant())
            {
                case "large":
                    // Generate a large JSON object for streaming tests
                    return GenerateLargeStreamingJson();

                case "nested":
                    // Generate a deeply nested JSON structure for streaming tests
                    return GenerateNestedStreamingJson();

                case "chunked":
                    // Generate a JSON structure with many small chunks
                    return GenerateChunkedStreamingJson();

                default:
                    // Generate a standard streaming test JSON
                    obj["type"] = "streaming_test";
                    obj["timestamp"] = DateTime.UtcNow.ToString("o");
                    obj["data"] = GenerateStreamingData();
                    return obj.ToString();
            }
        }

        /// <summary>
        /// Generates streaming test data
        /// </summary>
        private JArray GenerateStreamingData()
        {
            JArray array = new JArray();
            int count = _random.Next(5, 20);

            for (int i = 0; i < count; i++)
            {
                JObject item = new JObject();
                item["id"] = i;
                item["value"] = _random.Next(1000);
                item["name"] = $"Item_{i}";
                array.Add(item);
            }

            return array;
        }

        /// <summary>
        /// Generates a large JSON object for streaming tests
        /// </summary>
        private string GenerateLargeStreamingJson()
        {
            JObject root = new JObject();
            root["type"] = "large_streaming_test";

            JArray items = new JArray();
            int count = _random.Next(100, 500);

            for (int i = 0; i < count; i++)
            {
                JObject item = new JObject();
                item["id"] = i;
                item["guid"] = Guid.NewGuid().ToString();
                item["isActive"] = _random.Next(2) == 1;
                item["balance"] = _random.NextDouble() * 10000;
                item["picture"] = $"https://example.com/images/{i}.jpg";
                item["age"] = _random.Next(18, 90);
                item["name"] = $"Person {i}";
                item["company"] = $"Company {_random.Next(50)}";
                item["email"] = $"person{i}@example.com";
                item["phone"] = $"+1 ({_random.Next(100, 999)}) {_random.Next(100, 999)}-{_random.Next(1000, 9999)}";
                item["address"] = $"{_random.Next(1000)} Main St, City {_random.Next(100)}, State {_random.Next(50)}";

                JArray tags = new JArray();
                int tagCount = _random.Next(1, 8);
                for (int t = 0; t < tagCount; t++)
                {
                    tags.Add($"tag{_random.Next(100)}");
                }
                item["tags"] = tags;

                items.Add(item);
            }

            root["items"] = items;
            return root.ToString();
        }

        /// <summary>
        /// Generates a deeply nested JSON structure for streaming tests
        /// </summary>
        private string GenerateNestedStreamingJson()
        {
            JObject root = new JObject();
            root["type"] = "nested_streaming_test";

            // Create a nested structure
            JObject current = root;
            int depth = _random.Next(10, 30);

            for (int i = 0; i < depth; i++)
            {
                JObject next = new JObject();
                next["level"] = i + 1;
                next["value"] = _random.Next(1000);

                current["next"] = next;
                current = next;
            }

            // Add a final value
            current["final"] = true;

            return root.ToString();
        }

        /// <summary>
        /// Generates a JSON structure with many small chunks
        /// </summary>
        private string GenerateChunkedStreamingJson()
        {
            JArray root = new JArray();
            int chunkCount = _random.Next(50, 200);

            for (int i = 0; i < chunkCount; i++)
            {
                JObject chunk = new JObject();
                chunk["chunk_id"] = i;
                chunk["data"] = new string('X', _random.Next(10, 100));
                root.Add(chunk);
            }

            return root.ToString();
        }
    }
}
