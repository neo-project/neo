// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark_JsonDeserialize.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace Neo.Json.Benchmarks
{
    [MemoryDiagnoser]  // Enabling Memory Diagnostics
    [CsvMeasurementsExporter]  // Export results in CSV format
    [MarkdownExporter]  // Exporting results in Markdown format
    public class Benchmark_JsonDeserialize
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private string _jsonString;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [GlobalSetup]
        public void Setup()
        {
            // Reading JSON files
            _jsonString = File.ReadAllText("Data/RpcTestCases.json");
        }

        /// <summary>
        /// Deserialization with Newtonsoft.Json
        /// </summary>
        [Benchmark]
        public List<RpcTestCaseN>? Newtonsoft_Deserialize()
        {
            return JsonConvert.DeserializeObject<List<RpcTestCaseN>>(_jsonString);
        }

        /// <summary>
        /// Deserialization with Neo.Json (supports nested parsing)
        /// </summary>
        [Benchmark]
        public List<RpcTestCase> NeoJson_Deserialize()
        {
            // Parses into JArray
            if (JToken.Parse(_jsonString) is not JArray neoJsonObject)
                return [];

            var result = new List<RpcTestCase>();

            foreach (var item in neoJsonObject)
            {
                var testCase = new RpcTestCase
                {
                    Name = item?["Name"]?.GetString(),
                    Request = new RpcRequest
                    {
                        JsonRpc = item?["Request"]?["jsonrpc"]?.GetString(),
                        Method = item?["Request"]?["method"]?.GetString(),
                        Params = ConvertToJTokenArray(item?["Request"]?["params"]),
                        Id = item?["Request"]?["id"]?.GetNumber()
                    },
                    Response = new RpcResponse
                    {
                        JsonRpc = item?["Response"]?["jsonrpc"]?.GetString(),
                        Id = item?["Response"]?["id"]?.GetNumber(),
                        Result = item?["Response"]?["result"]
                    }
                };
                result.Add(testCase);
            }
            return result;
        }

        /// <summary>
        /// Recursively parsing params and stack arrays
        /// </summary>
        private List<object?> ParseParams(JToken? token)
        {
            var result = new List<object?>();

            if (token is JArray array)
            {
                // Parsing JArray correctly with foreach
                foreach (var item in array)
                {
                    result.Add(ParseParams(item));
                }
            }
            else if (token is JObject obj)
            {
                // Properties traversal with Neo.Json.JObject
                var dict = new Dictionary<string, object?>();
                foreach (var property in obj.Properties)
                {
                    dict[property.Key] = property.Value?.GetString();
                }
                result.Add(dict);
            }
            else
            {
                // If it's a normal value, it's straightforward to add
                result.Add(token?.GetString());
            }

            return result;
        }

        /// <summary>
        /// Parses any type of JSON into a JToken[] (for nested structures)
        /// </summary>
        private JToken[] ConvertToJTokenArray(JToken? token)
        {
            var result = new List<JToken>();

            if (token is JArray array)
            {
                // If it's a JArray, parse it one by one and add it to the result
                foreach (var item in array)
                {
                    result.AddRange(ConvertToJTokenArray(item));
                }
            }
            else if (token is JObject obj)
            {
                // Convert JObject to JToken (Dictionary type)
                var newObj = new JObject();
                foreach (var property in obj.Properties)
                    newObj[property.Key] = property.Value as JString;
                result.Add(newObj);
            }
            else if (token is not null)
            {
                // Add the base type JToken directly
                result.Add(token);
            }

            return [.. result];  // Converting a List to an Array of JTokens
        }
    }
}

/// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
/// 13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
/// .NET SDK 9.0.101
///   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
/// DefaultJob: .NET 9.0.0(9.0.24.52809), X64 RyuJIT AVX2
/// 
/// | Method                 | Mean     | Error    | StdDev    | Gen0    | Gen1    | Gen2    | Allocated |
/// |----------------------- |---------:|---------:|----------:|--------:|--------:|--------:|----------:|
/// | Newtonsoft_Deserialize | 627.4 us |  9.10 us |   8.07 us | 79.1016 | 53.7109 |       - | 978.52 KB |
/// | NeoJson_Deserialize    | 635.8 us | 41.54 us | 122.49 us | 73.2422 | 36.1328 | 36.1328 | 919.45 KB |

/// | Method                 | Mean     | Error   | StdDev   | Gen0    | Gen1    | Gen2    | Allocated |
/// |----------------------- |---------:|--------:|---------:|--------:|--------:|--------:|----------:|
/// | Newtonsoft_Deserialize | 627.8 us | 7.35 us | 10.54 us | 79.1016 | 53.7109 |       - | 978.52 KB |
/// | NeoJson_Deserialize    | 497.8 us | 8.37 us |  7.42 us | 73.2422 | 36.1328 | 36.1328 | 919.45 KB |

/// | Method                 | Mean     | Error   | StdDev   | Gen0    | Gen1    | Gen2    | Allocated |
/// |----------------------- |---------:|--------:|---------:|--------:|--------:|--------:|----------:|
/// | Newtonsoft_Deserialize | 634.6 us | 7.48 us |  7.00 us | 79.1016 | 53.7109 |       - | 978.52 KB |
/// | NeoJson_Deserialize    | 484.5 us | 9.49 us | 10.93 us | 73.7305 | 36.6211 | 36.6211 | 919.45 KB |
