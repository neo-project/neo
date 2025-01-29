// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.StoreCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Collections.Caching;
using Neo.Persistence;
using Neo.SmartContract;
using System.Diagnostics;

namespace Neo.Benchmark.Collections.Caching
{
    [MemoryDiagnoser]  // Enabling Memory Diagnostics
    [CsvMeasurementsExporter]  // Export results in CSV format
    [MarkdownExporter]  // Exporting results in Markdown format
    public class Benchmarks_StoreCache
    {
        private static readonly MemoryStore s_memoryStore = new();
        private static readonly StoreCache<StorageKey, StorageItem> s_storeCache = new(s_memoryStore);
        private static readonly DataCache s_dataCache = new SnapshotCache(s_memoryStore);

        private static byte[] s_data = [];
        private static StorageKey s_key;
        private static StorageItem s_value;

        [GlobalSetup]
        public void Setup()
        {
            if (s_data.Length == 0)
            {
                s_data = new byte[4096];
                Random.Shared.NextBytes(s_data);
                s_key = new StorageKey(s_data);
                s_value = new StorageItem(s_data);
            }
        }

        [Benchmark]
        public void TestStoreCacheAddAndUpdate()
        {
            s_storeCache.Add(s_key, s_value);
            s_storeCache[s_key] = new(s_data);
        }

        [Benchmark]
        public void TestStoreCacheRemove()
        {
            s_storeCache.Add(s_key, s_value);
            Debug.Assert(s_storeCache.Remove(s_key, out _));
        }

        [Benchmark]
        public void TestStoreCacheGetAlreadyCachedData()
        {
            s_storeCache.Add(s_key, s_value);
            Debug.Assert(s_storeCache.TryGetValue(s_key, out _));
            Debug.Assert(s_storeCache.ContainsKey(s_key));
            _ = s_storeCache[s_key];
        }

        [Benchmark]
        public void TestStoreCacheGetNonCachedData()
        {
            s_memoryStore.Put(s_key.ToArray(), s_key.ToArray());
            Debug.Assert(s_storeCache.TryGetValue(s_key, out _));
            Debug.Assert(s_storeCache.ContainsKey(s_key));
            _ = s_storeCache[s_key];
        }

        [Benchmark]
        public void TestDataCacheAddAndUpdate()
        {
            _ = s_dataCache.GetOrAdd(s_key, () => s_value);
            _ = s_dataCache.GetAndChange(s_key, () => new(s_data));
        }


        [Benchmark]
        public void TestDataCacheRemove()
        {
            _ = s_dataCache.GetOrAdd(s_key, () => s_value);
            s_dataCache.Delete(s_key);
        }

        [Benchmark]
        public void TestDataCacheGetAlreadyCachedData()
        {
            _ = s_dataCache.GetOrAdd(s_key, () => s_value);
            _ = s_dataCache.GetAndChange(s_key);
            Debug.Assert(s_dataCache.Contains(s_key));
            _ = s_dataCache[s_key];
        }

        [Benchmark]
        public void TestDataCacheGetNonCachedData()
        {
            s_memoryStore.Put(s_key.ToArray(), s_key.ToArray());
            _ = s_dataCache.GetAndChange(s_key);
            Debug.Assert(s_dataCache.Contains(s_key));
            _ = s_dataCache[s_key];
        }
    }
}
