// Copyright (C) 2015-2025 The Neo Project.
//
// NativeBenchmarkStateFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Provides shared blockchain state for native contract benchmarks.
    /// </summary>
    internal static class NativeBenchmarkStateFactory
    {
        private sealed class BenchmarkStoreProvider : IStoreProvider
        {
            public readonly MemoryStore Store = new();

            public string Name => "BenchmarkMemoryStore";

            public IStore GetStore(string path) => Store;
        }

        private static readonly Lazy<(NeoSystem System, BenchmarkStoreProvider Provider)> s_system = new(() =>
        {
            var provider = new BenchmarkStoreProvider();
            var settings = BenchmarkProtocolSettings.ResolveSettings();
            var system = new NeoSystem(settings, provider);
            system.SuspendNodeStartup();
            return (system, provider);
        });

        public static StoreCache CreateSnapshot()
        {
            return s_system.Value.System.GetSnapshotCache();
        }

        public static BenchmarkApplicationEngine CreateEngine(IVerifiable? container = null)
        {
            return BenchmarkApplicationEngine.Create(snapshot: CreateSnapshot(), container: container);
        }
    }
}
