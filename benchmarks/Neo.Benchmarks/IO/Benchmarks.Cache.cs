// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.Cache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.IO.Caching;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Neo.Benchmarks
{
    class BenchmarkFIFOCache : FIFOCache<long, long>
    {
        public BenchmarkFIFOCache(int maxCapacity) : base(maxCapacity) { }

        protected override long GetKeyForItem(long item) => item;
    }

    public class Benchmarks_Cache
    {
        private readonly BenchmarkFIFOCache _cache = new(100);
        private readonly int _iterationCount = 1000;
        private readonly int _cacheSize = 100;

        [Params(1000, 10000)]
        public int OperationCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Initialize cache with some data
            for (int i = 0; i < _cacheSize; i++)
            {
                _cache.Add(i);
            }
        }

        [Benchmark]
        public void FIFOCacheAdd()
        {
            for (int i = 0; i < _iterationCount; i++)
            {
                _cache.Add(i);
            }
        }

        [Benchmark]
        public void FIFOCacheContains()
        {
            for (long i = 0; i < _iterationCount; i++)
            {
                var ok = _cache.TryGet(i % _cacheSize, out _);
                Debug.Assert(ok);
            }
        }

        [Benchmark]
        public void CachePerformanceWithoutParallel()
        {
            // Simulating the optimized version (current implementation)
            var dictionary = GetSampleDictionary(OperationCount);
            var removedCount = dictionary.Count - _cacheSize + 1;

            foreach (var toDelete in dictionary.Values.OrderBy(p => p.Time).Take(removedCount))
            {
                // Simulate removal
                _ = toDelete;
            }
        }

        [Benchmark]
        public void CachePerformanceWithParallel()
        {
            // Simulating the previous version with AsParallel
            var dictionary = GetSampleDictionary(OperationCount);

            foreach (var item_del in dictionary.Values.AsParallel().OrderBy(p => p.Time).Take(dictionary.Count - _cacheSize + 1))
            {
                // Simulate removal
                _ = item_del;
            }
        }

        private Dictionary<long, CacheItem> GetSampleDictionary(int count)
        {
            var dictionary = new Dictionary<long, CacheItem>();
            for (long i = 0; i < count; i++)
            {
                dictionary[i] = new CacheItem(i, i);
            }
            return dictionary;
        }

        // Sample class to simulate the CacheItem for benchmarking
        private class CacheItem(long key, long value)
        {
            public readonly long Key = key;
            public readonly long Value = value;
            public readonly System.DateTime Time = System.DateTime.UtcNow.AddMilliseconds(-key); // Staggered times
        }
    }
}
