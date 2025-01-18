// Copyright (C) 2015-2025 The Neo Project.
//
// Bechmarks_LevelDB.cs file belongs to the neo project and is free
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
    public class Bechmarks_LevelDB
    {
        // avoid allocations in benchmarks
        private static StorageKey key1;
        private static readonly byte[] value = new UInt256().GetSpan().ToArray();

        private const string PathLevelDB = "Data_LevelDB_Benchmarks";

        private static readonly LevelDBStore levelDb = new();
        private static ISnapshot snapshot;

        [GlobalSetup]
        public void Setup()
        {
            if (Directory.Exists(PathLevelDB))
                Directory.Delete(PathLevelDB, true);

            key1 = new KeyBuilder(1, 1).Add(new UInt160());

            var levelDbStore = levelDb.GetStore(PathLevelDB);
            snapshot = levelDbStore.GetSnapshot();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            snapshot.Dispose();
            levelDb.Dispose();
            if (Directory.Exists(PathLevelDB))
                Directory.Delete(PathLevelDB, true);
        }

        [Benchmark]
        public void LevelDBSnapshotWrites()
        {
            snapshot.Put(key1.ToArray(), value);
            snapshot.Delete(key1.ToArray());
        }
    }
}
