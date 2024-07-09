// Copyright (C) 2015-2024 The Neo Project.
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
using System;

namespace Neo.Plugins.OracleService.Tests
{
    public static class TestBlockchain
    {
        private static readonly NeoSystem s_theNeoSystem;
        private static readonly MemoryStore s_store = new();

        private class StoreProvider : IStoreProvider
        {
            public string Name => "TestProvider";

            public IStore GetStore(string path) => s_store;
        }

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            s_theNeoSystem = new NeoSystem(ProtocolSettings.Load("config.json"), new StoreProvider());
        }

        public static void InitializeMockNeoSystem()
        {
        }

        internal static void ResetStore()
        {
            s_store.Reset();
            s_theNeoSystem.Blockchain.Ask(new Blockchain.Initialize()).Wait();
        }

        internal static SnapshotCache GetTestSnapshotCache()
        {
            ResetStore();
            return s_theNeoSystem.GetSnapshot();
        }
    }
}
