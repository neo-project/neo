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
using Microsoft.Extensions.Configuration;
using Neo.Ledger;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.Plugins.DBFTPlugin;
using Neo.UnitTests;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    public static class MockBlockchain
    {
        public static readonly NeoSystem TheNeoSystem;
        public static readonly UInt160[] DefaultExtensibleWitnessWhiteList;
        private static readonly MemoryStore Store = new();

        internal class StoreProvider : IStoreProvider
        {
            public string Name => "TestProvider";

            public IStore GetStore(string path) => Store;
        }

        static MockBlockchain()
        {
            Console.WriteLine("initialize NeoSystem");
            TheNeoSystem = new NeoSystem(MockProtocolSettings.Default, new StoreProvider());
        }

        internal static void ResetStore()
        {
            Store.Reset();
            TheNeoSystem.Blockchain.Ask(new Blockchain.Initialize()).Wait();
        }

        internal static Settings CreateDefaultSettings()
        {
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ApplicationConfiguration:DBFTPlugin:RecoveryLogs"] = "ConsensusState",
                    ["ApplicationConfiguration:DBFTPlugin:IgnoreRecoveryLogs"] = "false",
                    ["ApplicationConfiguration:DBFTPlugin:AutoStart"] = "false",
                    ["ApplicationConfiguration:DBFTPlugin:Network"] = "5195086",
                    ["ApplicationConfiguration:DBFTPlugin:MaxBlockSize"] = "262144",
                    ["ApplicationConfiguration:DBFTPlugin:MaxBlockSystemFee"] = "150000000000"
                })
                .Build();

            return new Settings(config.GetSection("ApplicationConfiguration:DBFTPlugin"));
        }

        internal static DataCache GetTestSnapshot()
        {
            return TheNeoSystem.GetSnapshotCache().CloneCache();
        }
    }
}
