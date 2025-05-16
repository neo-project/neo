// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark_JNumber.cs file belongs to the neo project and is free
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
    public class Benchmark_JNumber
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private JNumber _maxInt;
        private JNumber _zero;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [GlobalSetup]
        public void Setup()
        {
            _maxInt = new JNumber(JNumber.MAX_SAFE_INTEGER);
            _zero = new JNumber(0);
        }

        [Benchmark]
        public void TestAsBoolean()
        {
            _ = _maxInt.AsBoolean();
            _ = _zero.AsBoolean();
        }

        [Benchmark]
        public void TestAsString()
        {
            _ = _maxInt.AsString();
        }
    }
}

/// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
/// 13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
/// .NET SDK 9.0.101
///   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
/// DefaultJob: .NET 9.0.0(9.0.24.52809), X64 RyuJIT AVX2
/// 
/// | Method        | Mean      | Error     | StdDev    | Gen0   | Allocated |
/// |-------------- |----------:|----------:|----------:|-------:|----------:|
/// | TestAsBoolean |  2.510 ns | 0.0603 ns | 0.0564 ns |      - |         - |
/// | TestAsString  | 87.000 ns | 1.2230 ns | 1.1440 ns | 0.0044 |      56 B |
