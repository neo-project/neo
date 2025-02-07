// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks_DataCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Neo.Persistence;
using Neo.SmartContract;
using Perfolizer.Mathematics.OutlierDetection;
using System.Diagnostics;

namespace Neo.Benchmarks.IO.Caching
{
    // Result Exporters
    [MarkdownExporter]                                                  // Exporting results in Markdown format
    // Result Output
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]                    // Include these columns
    [Orderer(SummaryOrderPolicy.Declared, MethodOrderPolicy.Declared)]  // Keep in current order as declared in class
    // Job Configurations
    [SimpleJob(RuntimeMoniker.Net90)]
    [Outliers(OutlierMode.DontRemove)]
    [GcServer(true)]                                                    // GC server is enabled for GitHub builds in `neo` repo
    [GcConcurrent(true)]                                                // GC runs on it own thread
    [GcForce(false)]                                                    // DO NOT force full collection of data for each benchmark
    public class Benchmarks_DataCache
    {
        private readonly MemoryStore _memoryStore = new();
        private readonly OldDataCache _oldDataCache;
        private readonly DataCache _dataCache;

        private readonly StorageKey _key;
        private readonly StorageItem _value;

        public Benchmarks_DataCache()
        {
            _oldDataCache = new(_memoryStore);
            _dataCache = new SnapshotCache(_memoryStore);

            var data = new byte[1024];
            new Random(0xdead).NextBytes(data);

            _memoryStore.Put(data, data);
            _key = new(data);
            _value = new(data);
        }

        [Benchmark]
        public void TestOldDataCacheAdd()
        {
            Debug.Assert(_oldDataCache.GetOrAdd(_key, () => _value) != null);
        }

        [Benchmark]
        public void TestDataCacheAdd()
        {
            Debug.Assert(_dataCache.GetOrAdd(_key, () => _value) != null);
        }

        [Benchmark]
        public void TestOldDataCacheUpdate()
        {
            Debug.Assert(_oldDataCache.GetAndChange(_key, () => _value) != null);
        }

        [Benchmark]
        public void TestDataCacheUpdate()
        {
            Debug.Assert(_dataCache.GetAndChange(_key, () => _value) != null);
        }

        [Benchmark]
        public void TestOldDataCacheRemove()
        {
            _oldDataCache.Delete(_key);
        }

        [Benchmark]
        public void TestDataCacheRemove()
        {
            _dataCache.Delete(_key);
        }

        [Benchmark]
        public void TestOldDataCacheGet()
        {
            Debug.Assert(_oldDataCache.GetAndChange(_key) != null);
            Debug.Assert(_oldDataCache.Contains(_key));
            Debug.Assert(_oldDataCache[_key] != null);
        }

        [Benchmark]
        public void TestDataCacheGet()
        {
            Debug.Assert(_dataCache.GetAndChange(_key) != null);
            Debug.Assert(_dataCache.Contains(_key));
            Debug.Assert(_dataCache[_key] != null);
        }
    }
}
