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
    class BecnmarkFIFOCache : FIFOCache<long, long>
    {
        public BecnmarkFIFOCache(int maxCapacity) : base(maxCapacity) { }

        protected override long GetKeyForItem(long item) => item;
    }

    public class Benchmarks_Cache
    {
        private readonly BecnmarkFIFOCache _cache = new(100);

        [Benchmark]
        public void FIFOCacheAdd()
        {
            for (int i = 0; i < 1000; i++)
            {
                _cache.Add(i);
            }
        }

        [Benchmark]
        public void FIFOCacheContains()
        {
            for (long i = 0; i < 1000; i++)
            {
                var ok = _cache.TryGet(i, out _);
                Debug.Assert(ok);
            }
        }
    }
}