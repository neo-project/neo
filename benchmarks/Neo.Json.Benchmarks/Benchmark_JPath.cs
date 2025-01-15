// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark_JPath.cs file belongs to the neo project and is free
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
    public class Benchmark_JPath
    {
        private JObject json;

        [GlobalSetup]
        public void Setup()
        {
            json = new JObject
            {
                ["store"] = new JObject
                {
                    ["book"] = new JArray
                {
                    new JObject { ["title"] = "Book A", ["price"] = 10.99 },
                    new JObject { ["title"] = "Book B", ["price"] = 15.50 }
                }
                }
            };
        }

        [Benchmark]
        public void TestJsonPathQuery()
        {
            json.JsonPath("$.store.book[*].title");
        }
    }

}
