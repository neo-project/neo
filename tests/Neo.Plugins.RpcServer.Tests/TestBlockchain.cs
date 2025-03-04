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
using Neo.UnitTests;
using System;

namespace Neo.Plugins.RpcServer.Tests
{
    public static class TestBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static readonly UInt160[] DefaultExtensibleWitnessWhiteList;
        private static readonly MemoryStore Store = new();

        internal class StoreProvider : IStoreProvider
        {
            public string Name => "TestProvider";

            public IStore GetStore(string path) => Store;
        }

        static TestBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem(TestProtocolSettings.Default, new StoreProvider());
        }

        internal static void ResetStore()
        {
            Store.Reset();
            TheNeoSystem.Blockchain.Ask(new Blockchain.Initialize()).Wait();
        }

        internal static DataCache GetTestSnapshot()
        {
            return TheNeoSystem.GetSnapshotCache().CloneCache();
        }
    }
}
