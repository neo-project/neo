// Copyright (C) 2015-2024 The Neo Project.
//
// JsonArrayBenchmark.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using BenchmarkDotNet.Attributes;
using Neo.Json;

namespace Neo.Json.Benchmarks
{
    [MemoryDiagnoser]  // 用于统计内存使用
    [CsvMeasurementsExporter]  // CSV 格式导出
    [MarkdownExporter]         // Markdown 格式导出
    public class JsonArrayBenchmark
    {
        private JObject alice;
        private JObject bob;
        private JArray jArray;

        [GlobalSetup]
        public void Setup()
        {
            alice = new JObject
            {
                ["name"] = "alice",
                ["age"] = 30,
                ["score"] = 100.001,
                ["gender"] = "female",
                ["isMarried"] = true,
                ["pet"] = new JObject
                {
                    ["name"] = "Tom",
                    ["type"] = "cat"
                }
            };

            bob = new JObject
            {
                ["name"] = "bob",
                ["age"] = 100000,
                ["score"] = 0.001,
                ["gender"] = "male",
                ["isMarried"] = false,
                ["pet"] = new JObject
                {
                    ["name"] = "Paul",
                    ["type"] = "dog"
                }
            };

            jArray = new JArray();
        }

        [Benchmark]
        public void TestAdd()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Add(bob);
        }

        [Benchmark]
        public void TestSetItem()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray[0] = bob;
        }

        [Benchmark]
        public void TestClear()
        {
            jArray.Clear();
        }

        [Benchmark]
        public void TestContains()
        {
            jArray.Clear();
            jArray.Add(alice);
            var contains = jArray.Contains(alice);
        }

        [Benchmark]
        public void TestCopyTo()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Add(bob);

            JObject[] objects = new JObject[2];
            jArray.CopyTo(objects, 0);
        }

        [Benchmark]
        public void TestInsert()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Insert(0, bob);
        }

        [Benchmark]
        public void TestIndexOf()
        {
            jArray.Clear();
            jArray.Add(alice);
            int index = jArray.IndexOf(alice);
        }

        [Benchmark]
        public void TestRemove()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Remove(alice);
        }

        [Benchmark]
        public void TestRemoveAt()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Add(bob);
            jArray.RemoveAt(1);
        }

        [Benchmark]
        public void TestGetEnumerator()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Add(bob);
            foreach (var item in jArray)
            {
                // Do nothing, just enumerate
            }
        }

        [Benchmark]
        public void TestCount()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Add(bob);
            var count = jArray.Count;
        }

        [Benchmark]
        public void TestClone()
        {
            jArray.Clear();
            jArray.Add(alice);
            var clonedArray = (JArray)jArray.Clone();
        }

        [Benchmark]
        public void TestAddNull()
        {
            jArray.Clear();
            jArray.Add(null);
        }

        [Benchmark]
        public void TestSetNull()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray[0] = null;
        }

        [Benchmark]
        public void TestInsertNull()
        {
            jArray.Clear();
            jArray.Add(alice);
            jArray.Insert(0, null);
        }

        [Benchmark]
        public void TestRemoveNull()
        {
            jArray.Clear();
            jArray.Add(null);
            jArray.Remove(null);
        }
    }
}
