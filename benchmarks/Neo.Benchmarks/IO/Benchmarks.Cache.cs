// Copyright (C) 2015-2026 The Neo Project.
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
using System.Runtime.CompilerServices;

namespace Neo.Benchmarks.IO;

class BenchmarkFIFOCache : FIFOCache<long, long>
{
    public BenchmarkFIFOCache(int maxCapacity) : base(maxCapacity) { }

    protected override long GetKeyForItem(long item) => item;
}

class BenchmarkKeyedCollectionSlim : KeyedCollectionSlim<long, long>
{
    public BenchmarkKeyedCollectionSlim(int capacity) : base(capacity) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override long GetKeyForItem(long item) => item;
}

public class Benchmarks_Cache
{
    const int CacheSize = 1000;

    private readonly BenchmarkFIFOCache _cache = new(CacheSize);

    private readonly HashSetCache<long> _hashSetCache = new(CacheSize);

    private long[] _items = [];

    [Params(1000, 10000)]
    public int OperationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Initialize cache with some data
        for (int i = 0; i < CacheSize; i++)
        {
            _cache.Add(i);
        }
    }

    [Benchmark]
    public void FIFOCacheAdd()
    {
        for (int i = 0; i < OperationCount; i++)
        {
            _cache.Add(i);
        }
    }

    [Benchmark]
    public void FIFOCacheContains()
    {
        for (long i = 0; i < OperationCount; i++)
        {
            var ok = _cache.TryGet(i, out _);
            Debug.Assert(ok);
        }
    }

    [Benchmark]
    public void KeyedCollectionSlimAdd()
    {
        var keyed = new BenchmarkKeyedCollectionSlim(CacheSize);
        for (int i = 0; i < OperationCount; i++)
        {
            keyed.TryAdd(i);
        }
    }

    [Benchmark]
    public void KeyedCollectionSlimMixed()
    {
        var keyed = new BenchmarkKeyedCollectionSlim(CacheSize);
        for (long i = 0; i < OperationCount; i++)
        {
            keyed.TryAdd(i);

            var ok = keyed.Contains(i);
            Debug.Assert(ok);
        }

        for (long i = 0; i < OperationCount; i++)
        {
            var ok = keyed.Remove(i);
            Debug.Assert(ok);
        }
    }

    [GlobalSetup(Target = nameof(HashSetCache))]
    public void SetupHashSetCache()
    {
        _items = new long[OperationCount];
        for (int i = 0; i < OperationCount; i++)
        {
            _items[i] = i;
        }
    }

    [Benchmark]
    public void HashSetCache()
    {
        for (int i = 0; i < OperationCount; i++)
        {
            var ok = _hashSetCache.TryAdd(i);
            Debug.Assert(ok);
        }
        if (_hashSetCache.Count != CacheSize)
            throw new Exception($"HashSetCacheAdd: {_hashSetCache.Count}");

        _hashSetCache.ExceptWith(_items);
        if (_hashSetCache.Count > 0)
            throw new Exception($"HashSetCacheExceptWith: {_hashSetCache.Count}");
    }
}
