// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark_OrderedDictionary.cs file belongs to the neo project and is free
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
    public class Benchmark_OrderedDictionary
    {
        private OrderedDictionary<string, uint> _od = [];

        [GlobalSetup]
        public void Setup()
        {
            _od = new OrderedDictionary<string, uint>
            {
                { "a", 1 },
                { "b", 2 },
                { "c", 3 }
            };
        }

        [Benchmark]
        public void TestClear()
        {
            _od.Clear();
        }

        [Benchmark]
        public void TestCount()
        {
            _ = _od.Count;
        }

        [Benchmark]
        public void TestRemove()
        {
            _od.Remove("a");
        }

        [Benchmark]
        public void TestTryGetValue()
        {
            _od.TryGetValue("a", out _);
        }
    }
}

/// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
/// 13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
/// .NET SDK 9.0.101
///   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
/// DefaultJob: .NET 9.0.0(9.0.24.52809), X64 RyuJIT AVX2
/// 
/// | Method          | Mean      | Error     | StdDev    | Median    | Allocated |
/// |---------------- |----------:|----------:|----------:|----------:|----------:|
/// | TestClear       | 0.3808 ns | 0.0216 ns | 0.0202 ns | 0.3774 ns |         - |
/// | TestCount       | 0.0056 ns | 0.0085 ns | 0.0079 ns | 0.0021 ns |         - |
/// | TestRemove      | 3.7165 ns | 0.0573 ns | 0.0479 ns | 3.7112 ns |         - |
/// | TestTryGetValue | 5.4563 ns | 0.0918 ns | 0.0717 ns | 5.4239 ns |         - |
