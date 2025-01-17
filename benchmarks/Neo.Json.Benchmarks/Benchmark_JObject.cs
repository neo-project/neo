// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark_JObject.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.Json.Benchmarks
{
    [MemoryDiagnoser]
    [CsvMeasurementsExporter]
    [MarkdownExporter]
    public class Benchmark_JObject
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private JObject _alice;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [GlobalSetup]
        public void Setup()
        {
            _alice = new JObject
            {
                ["name"] = "Alice",
                ["age"] = 30
            };
        }

        [Benchmark]
        public void TestAddProperty()
        {
            _alice["city"] = "New York";
        }

        [Benchmark]
        public void TestClone()
        {
            _ = _alice.Clone();
        }

        [Benchmark]
        public void TestParse()
        {
            JObject.Parse("{\"name\":\"John\", \"age\":25}");
        }
    }
}

/// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
/// 13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
/// .NET SDK 9.0.101
///   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
/// DefaultJob: .NET 9.0.0(9.0.24.52809), X64 RyuJIT AVX2
/// 
/// | Method          | Mean      | Error    | StdDev   | Gen0   | Allocated |
/// |---------------- |----------:|---------:|---------:|-------:|----------:|
/// | TestAddProperty |  11.35 ns | 0.135 ns | 0.119 ns | 0.0019 |      24 B |
/// | TestClone       | 123.72 ns | 1.898 ns | 1.585 ns | 0.0503 |     632 B |
/// | TestParse       | 240.81 ns | 2.974 ns | 2.322 ns | 0.0577 |     728 B |
