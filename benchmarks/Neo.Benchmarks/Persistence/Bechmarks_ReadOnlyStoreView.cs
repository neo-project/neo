// Copyright (C) 2015-2024 The Neo Project.
//
// Bechmarks_ReadOnlyStoreView.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


using BenchmarkDotNet.Attributes;
using Neo.Persistence;
using Neo.Plugins.Storage;
using Neo.SmartContract;
using System.Diagnostics;

namespace Neo.Benchmarks.Persistence.Benchmarks
{
    public class Bechmarks_ReadOnlyStoreView
    {
        private static StorageKey key1, key2;

        private static readonly byte[] value = new UInt256().GetSpan().ToArray();

        private const string PathLevelDB = "Data_LevelDB_Benchmarks";

        private static readonly LevelDBStore levelDb = new();

        private static IStore levelDbStore;


        [GlobalSetup]
        public void Setup()
        {
            if (Directory.Exists(PathLevelDB))
                Directory.Delete(PathLevelDB, true);

            key1 = new KeyBuilder(1, 1).Add(new UInt160());
            key2 = new KeyBuilder(2, 2).Add(new UInt160());

            levelDbStore = levelDb.GetStore(PathLevelDB);
            levelDbStore.Put(key1.ToArray(), value);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            levelDbStore.Dispose();

            if (Directory.Exists(PathLevelDB))
                Directory.Delete(PathLevelDB, true);
        }

        [Benchmark]
        public void ReadOnlyStoreView_LevelDB()
        {
            var view = new ReadOnlyStoreView(levelDbStore);
            var ok = view.TryGet(key1, out var _);
            Debug.Assert(ok);

            ok = view.TryGet(key2, out var _);
            Debug.Assert(!ok);
        }

        [Benchmark]
        public void SnapshotCache_LevelDB()
        {
            var snapshot = new SnapshotCache(levelDbStore);
            var ok = snapshot.TryGet(key1, out var _);
            Debug.Assert(ok);

            ok = snapshot.TryGet(key2, out var _);
            Debug.Assert(!ok);
        }
    }
}
