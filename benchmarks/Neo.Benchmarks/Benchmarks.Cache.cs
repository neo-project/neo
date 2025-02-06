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
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Numerics;

namespace Neo.Benchmark
{
    public class Benchmarks_Cache
    {
        // 256 KiB
        readonly MemoryStore _store;
        readonly SnapshotCache _snapshot;

        public Benchmarks_Cache()
        {
            _store = new MemoryStore();
            _snapshot = new(_store.GetSnapshot());

            // Ledger.CurrentIndex

            _snapshot.GetAndChange(new KeyBuilder(NativeContract.Ledger.Id, 12), () => new StorageItem(new HashIndexState() { Hash = UInt256.Zero, Index = 2 }));

            // Gas Per block

            _snapshot.GetAndChange(new KeyBuilder(NativeContract.NEO.Id, 29).AddBigEndian(0), () => new StorageItem(0));
            _snapshot.GetAndChange(new KeyBuilder(NativeContract.NEO.Id, 29).AddBigEndian(1), () => new StorageItem(1));
            _snapshot.GetAndChange(new KeyBuilder(NativeContract.NEO.Id, 29).AddBigEndian(2), () => new StorageItem(2));
        }

        [Benchmark]
        public void WithCache()
        {
            for (var x = 0; x < 1_000; x++)
            {
                var ret = NativeContract.NEO.GetGasPerBlock(_snapshot);
                if (ret != 2) throw new Exception("Test error");
            }
        }

        [Benchmark]
        public void WithoutCache()
        {
            for (var x = 0; x < 1_000; x++)
            {
                var ret = OldCode();
                if (ret != 2) throw new Exception("Test error");
            }
        }

        private BigInteger OldCode()
        {
            var end = NativeContract.Ledger.CurrentIndex(_snapshot) + 1;
            var last = NativeContract.NEO.GetSortedGasRecords(_snapshot, end).First();
            return last.GasPerBlock;
        }
    }
}
