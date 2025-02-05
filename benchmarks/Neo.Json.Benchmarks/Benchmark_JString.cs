// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark_JString.cs file belongs to the neo project and is free
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
    public class Benchmark_JString
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private JString _testString;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [GlobalSetup]
        public void Setup()
        {
            _testString = new JString("hello world");
        }

        [Benchmark]
        public void TestLength()
        {
            _ = _testString.Value.Length;
        }

        [Benchmark]
        public void TestConversionToString()
        {
            _ = _testString.ToString();
        }

        [Benchmark]
        public void TestClone()
        {
            _ = _testString.Clone();
        }
    }
}

///BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
///13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
///.NET SDK 9.0.101
///  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
///  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
///
///| Method                 | Mean       | Error     | StdDev    | Gen0   | Gen1   | Allocated |
///|----------------------- |-----------:|----------:|----------:|-------:|-------:|----------:|
///| TestLength             |  0.0050 ns | 0.0044 ns | 0.0041 ns |      - |      - |         - |
///| TestConversionToString | 76.8631 ns | 1.0699 ns | 1.2737 ns | 0.0695 | 0.0001 |     872 B |
///| TestClone              |  0.0233 ns | 0.0104 ns | 0.0087 ns |      - |      - |         - |
