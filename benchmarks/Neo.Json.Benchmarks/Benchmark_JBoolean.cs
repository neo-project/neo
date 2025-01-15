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
