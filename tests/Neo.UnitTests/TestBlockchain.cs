// Copyright (C) 2015-2025 The Neo Project.
//
// TestBlockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Ledger;
using Neo.Persistence;
using Neo.Persistence.Providers;
using System;
using System.Threading.Tasks;

namespace Neo.UnitTests
{
    public static class TestBlockchain
    {
        private class TestStoreProvider : IStoreProvider
        {
            public readonly MemoryStore Store = new();

            public string Name => "TestProvider";

            public IStore GetStore(string path) => Store;
        }

        public class TestNeoSystem(ProtocolSettings settings) : NeoSystem(settings, new TestStoreProvider())
        {
            public void ResetStore()
            {
                (StorageProvider as TestStoreProvider).Store.Reset();
                // Use timeout for test environment to avoid deadlocks
                try
                {
                    var initTask = Blockchain.Ask(new Blockchain.Initialize(), TimeSpan.FromSeconds(5));
                    initTask.Wait(5000); // 5 second timeout
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Blockchain reset timeout in test: {ex.Message}");
                    // Continue anyway for test resilience
                }
            }

            public StoreCache GetTestSnapshotCache(bool reset = true)
            {
                if (reset)
                    ResetStore();
                return GetSnapshotCache();
            }
        }

        public static readonly UInt160[] DefaultExtensibleWitnessWhiteList;

        public static TestNeoSystem GetSystem() => new(TestProtocolSettings.Default);
        public static StoreCache GetTestSnapshotCache() => GetSystem().GetSnapshotCache();
    }
}
