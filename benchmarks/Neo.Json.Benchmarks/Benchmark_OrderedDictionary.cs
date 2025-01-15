// Copyright (C) 2015-2024 The Neo Project.
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
using Neo.Json;
using System.Collections.Generic;

namespace Neo.Json.Benchmarks
{

    [MemoryDiagnoser]
    [CsvMeasurementsExporter]
    [MarkdownExporter]
    public class Benchmark_OrderedDictionary
    {
        private OrderedDictionary<string, uint> od;

        [GlobalSetup]
        public void Setup()
        {
            od = new OrderedDictionary<string, uint>
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };
        }

        [Benchmark]
        public void TestClear()
        {
            od.Clear();
        }

        [Benchmark]
        public void TestCount()
        {
            var count = od.Count;
        }

        [Benchmark]
        public void TestAdd()
        {
            od.Add("d", 4);
        }

        [Benchmark]
        public void TestRemove()
        {
            od.Remove("a");
        }

        [Benchmark]
        public void TestTryGetValue()
        {
            od.TryGetValue("a", out uint value);
        }
    }

}
