// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.DataCacheSeek.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System.Buffers.Binary;

namespace Neo.Benchmark.Persistence
{
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class Benchmarks_DataCacheSeek
    {
        [Params(1_000, 10_000, 100_000)]
        public int EntryCount { get; set; }

        [Params(SeekDirection.Forward, SeekDirection.Backward)]
        public SeekDirection Direction { get; set; }

        private MemoryStore _store = null!;
        private StoreCache _cache = null!;
        private byte[] _start = null!;

        [GlobalSetup]
        public void Setup()
        {
            _store = new MemoryStore();
            _cache = new StoreCache(_store, false);
            var order = Enumerable.Range(0, EntryCount).ToArray();
            new Random(123).Shuffle(order);
            foreach (int i in order)
            {
                var key = new byte[sizeof(int)];
                BinaryPrimitives.WriteInt32BigEndian(key, i);
                _cache.Add(new StorageKey { Id = 1, Key = key }, new StorageItem(new byte[] { 0 }));
            }

            _start = StorageKey.CreateSearchPrefix(1,
                Direction == SeekDirection.Forward ? Array.Empty<byte>() : new byte[] { 255, 255, 255, 255 });
            // Warm the serialized keys; the backing store is empty to isolate cache work.
            foreach (var entry in _cache.Seek(_start, Direction))
                GC.KeepAlive(entry.Key);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _cache.Dispose();
            _store.Dispose();
        }

        [Benchmark]
        public StorageItem SeekFirst()
        {
            using var iterator = _cache.Seek(_start, Direction).GetEnumerator();
            return iterator.MoveNext() ? iterator.Current.Value : throw new InvalidOperationException();
        }
    }
}
