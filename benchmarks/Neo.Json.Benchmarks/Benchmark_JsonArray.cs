// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmark_JsonArray.cs file belongs to the neo project and is free
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
    [MemoryDiagnoser]  // 用于统计内存使用
    [CsvMeasurementsExporter]  // CSV 格式导出
    [MarkdownExporter]         // Markdown 格式导出
    public class Benchmark_JsonArray
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private JObject _alice;
        private JObject _bob;
        private JArray _jArray;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        [GlobalSetup]
        public void Setup()
        {
            _alice = new JObject
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

            _bob = new JObject
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

            _jArray = new JArray();
        }

        [Benchmark]
        public void TestAdd()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Add(_bob);
        }

        [Benchmark]
        public void TestSetItem()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray[0] = _bob;
        }

        [Benchmark]
        public void TestClear()
        {
            _jArray.Clear();
        }

        [Benchmark]
        public void TestContains()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _ = _jArray.Contains(_alice);
        }

        [Benchmark]
        public void TestCopyTo()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Add(_bob);

            var objects = new JObject[2];
            _jArray.CopyTo(objects, 0);
        }

        [Benchmark]
        public void TestInsert()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Insert(0, _bob);
        }

        [Benchmark]
        public void TestIndexOf()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _ = _jArray.IndexOf(_alice);
        }

        [Benchmark]
        public void TestRemove()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Remove(_alice);
        }

        [Benchmark]
        public void TestRemoveAt()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Add(_bob);
            _jArray.RemoveAt(1);
        }

        [Benchmark]
        public void TestGetEnumerator()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Add(_bob);
            foreach (var item in _jArray)
            {
                // Do nothing, just enumerate
            }
        }

        [Benchmark]
        public void TestCount()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Add(_bob);
            _ = _jArray.Count;
        }

        [Benchmark]
        public void TestClone()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _ = (JArray)_jArray.Clone();
        }

        [Benchmark]
        public void TestAddNull()
        {
            _jArray.Clear();
            _jArray.Add(null);
        }

        [Benchmark]
        public void TestSetNull()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray[0] = null;
        }

        [Benchmark]
        public void TestInsertNull()
        {
            _jArray.Clear();
            _jArray.Add(_alice);
            _jArray.Insert(0, null);
        }

        [Benchmark]
        public void TestRemoveNull()
        {
            _jArray.Clear();
            _jArray.Add(null);
            _jArray.Remove(null);
        }
    }
}

/// BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2605)
/// 13th Gen Intel Core i9-13900H, 1 CPU, 20 logical and 14 physical cores
/// .NET SDK 9.0.101
///   [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2 [AttachedDebugger]
///   DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
/// 
/// | Method            | Mean        | Error     | StdDev    | Gen0   | Gen1   | Allocated |
/// |------------------ |------------:|----------:|----------:|-------:|-------:|----------:|
/// | TestAdd           |  10.8580 ns | 0.1532 ns | 0.1433 ns |      - |      - |         - |
/// | TestSetItem       |  10.8238 ns | 0.1747 ns | 0.1459 ns |      - |      - |         - |
/// | TestClear         |   0.0663 ns | 0.0139 ns | 0.0116 ns |      - |      - |         - |
/// | TestContains      |   9.3212 ns | 0.1277 ns | 0.1195 ns |      - |      - |         - |
/// | TestCopyTo        |  18.6370 ns | 0.3341 ns | 0.2790 ns | 0.0032 |      - |      40 B |
/// | TestInsert        |  12.3404 ns | 0.1256 ns | 0.1175 ns |      - |      - |         - |
/// | TestIndexOf       |   9.2549 ns | 0.1196 ns | 0.1119 ns |      - |      - |         - |
/// | TestRemove        |   8.6535 ns | 0.1912 ns | 0.2276 ns |      - |      - |         - |
/// | TestRemoveAt      |  11.2368 ns | 0.0703 ns | 0.0549 ns |      - |      - |         - |
/// | TestGetEnumerator |  17.5149 ns | 0.1480 ns | 0.1384 ns | 0.0032 |      - |      40 B |
/// | TestCount         |   9.4478 ns | 0.1740 ns | 0.1627 ns |      - |      - |         - |
/// | TestClone         | 442.3215 ns | 6.7589 ns | 6.3223 ns | 0.1464 | 0.0005 |    1840 B |
/// | TestAddNull       |   2.1299 ns | 0.0309 ns | 0.0289 ns |      - |      - |         - |
/// | TestSetNull       |   6.2627 ns | 0.0706 ns | 0.0661 ns |      - |      - |         - |
/// | TestInsertNull    |   8.9616 ns | 0.0868 ns | 0.0812 ns |      - |      - |         - |
/// | TestRemoveNull    |   5.2719 ns | 0.0489 ns | 0.0457 ns |      - |      - |         - |
