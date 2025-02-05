// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark_JPath.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.Json.Benchmarks
{
    [MemoryDiagnoser]
    [CsvMeasurementsExporter]
    [MarkdownExporter]
    public class Benchmark_JPath
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private JObject _json;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [GlobalSetup]
        public void Setup()
        {
            _json = new JObject
            {
                ["store"] = new JObject
                {
                    ["book"] = new JArray
                {
                    new JObject { ["title"] = "Book A", ["price"] = 10.99 },
                    new JObject { ["title"] = "Book B", ["price"] = 15.50 }
                }
                }
            };
        }

        [Benchmark]
        public void TestJsonPathQuery()
        {
            _json.JsonPath("$.store.book[*].title");
        }
    }
}

/// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
/// 13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
/// .NET SDK 9.0.101
///   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
///   DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
/// 
/// | Method            | Mean     | Error    | StdDev  | Gen0   | Allocated |
/// |------------------ |---------:|---------:|--------:|-------:|----------:|
/// | TestJsonPathQuery | 679.7 ns | 11.84 ns | 9.89 ns | 0.1869 |    2.3 KB |
