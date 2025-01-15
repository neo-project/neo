// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark_JString.cs file belongs to the neo project and is free
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
    public class Benchmark_JString
    {
        private JString testString;

        [GlobalSetup]
        public void Setup()
        {
            testString = new JString("hello world");
        }

        [Benchmark]
        public void TestLength()
        {
            var length = testString.Value.Length;
        }

        [Benchmark]
        public void TestConversionToString()
        {
            _ = testString.ToString();
        }

        [Benchmark]
        public void TestClone()
        {
            var clone = testString.Clone();
        }
    }

}
