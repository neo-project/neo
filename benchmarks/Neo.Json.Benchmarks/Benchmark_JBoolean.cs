// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark_JBoolean.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Json;

namespace Neo.Json.Benchmarks
{
    [MemoryDiagnoser]
    [CsvMeasurementsExporter]
    [MarkdownExporter]
    public class Benchmark_JBoolean
    {
        private JBoolean jFalse;
        private JBoolean jTrue;

        [GlobalSetup]
        public void Setup()
        {
            jFalse = new JBoolean();
            jTrue = new JBoolean(true);
        }

        [Benchmark]
        public void TestAsNumber()
        {
            _ = jFalse.AsNumber();
            _ = jTrue.AsNumber();
        }

        [Benchmark]
        public void TestConversionToString()
        {
            _ = jTrue.ToString();
            _ = jFalse.ToString();
        }
    }

}

/// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
/// 13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
/// .NET SDK 9.0.101
///   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
/// DefaultJob: .NET 9.0.0(9.0.24.52809), X64 RyuJIT AVX2
/// 
/// | Method                 | Mean       | Error     | StdDev    | Median     | Gen0   | Allocated |
/// |----------------------- |-----------:|----------:|----------:|-----------:|-------:|----------:|
/// | TestAsNumber           |  0.0535 ns | 0.0233 ns | 0.0239 ns |  0.0427 ns |      - |         - |
/// | TestConversionToString | 17.8216 ns | 0.2321 ns | 0.1938 ns | 17.7613 ns | 0.0051 |      64 B |
