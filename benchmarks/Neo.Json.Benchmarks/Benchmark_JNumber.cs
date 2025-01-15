// Copyright (C) 2015-2024 The Neo Project.
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
        private JNumber maxInt;
        private JNumber minInt;
        private JNumber zero;

        [GlobalSetup]
        public void Setup()
        {
            maxInt = new JNumber(JNumber.MAX_SAFE_INTEGER);
            minInt = new JNumber(JNumber.MIN_SAFE_INTEGER);
            zero = new JNumber(0);
        }

        [Benchmark]
        public void TestAsBoolean()
        {
            _ = maxInt.AsBoolean();
            _ = zero.AsBoolean();
        }

        [Benchmark]
        public void TestAsString()
        {
            _ = maxInt.AsString();
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
