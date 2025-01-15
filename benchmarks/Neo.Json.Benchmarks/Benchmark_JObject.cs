// Copyright (C) 2015-2024 The Neo Project.
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
using Neo.Json;

namespace Neo.Json.Benchmarks
{
    [MemoryDiagnoser]
    [CsvMeasurementsExporter]
    [MarkdownExporter]
    public class Benchmark_JObject
    {
        private JObject alice;
        private JObject bob;

        [GlobalSetup]
        public void Setup()
        {
            alice = new JObject
            {
                ["name"] = "Alice",
                ["age"] = 30
            };

            bob = new JObject
            {
                ["name"] = "Bob",
                ["age"] = 40
            };
        }

        [Benchmark]
        public void TestAddProperty()
        {
            alice["city"] = "New York";
        }

        [Benchmark]
        public void TestClone()
        {
            var clone = alice.Clone();
        }

        [Benchmark]
        public void TestParse()
        {
            JObject.Parse("{\"name\":\"John\", \"age\":25}");
        }
    }

}
