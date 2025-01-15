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
using Neo.Json;

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
