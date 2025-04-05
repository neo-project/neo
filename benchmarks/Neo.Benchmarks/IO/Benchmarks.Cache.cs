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
using System.Diagnostics;


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
    }
}
