// Copyright (C) 2015-2025 The Neo Project.
//
// TestNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Build.Core.Providers;
using Neo.Build.Core.Wallets;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System.Text.Json.Nodes;

namespace Neo.Build.Core.Tests.Helpers
{
    internal static class TestNode
    {
        public static readonly NeoSystem NeoSystem;
        public static readonly DevWallet Wallet;
        public static readonly NeoBuildSettings BuildSettings = new(JsonNode.Parse("{}")!);
        public static readonly ILoggerFactory FactoryLogger = LoggerFactory.Create(logging =>
        {
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled;
            });
        });

        private static readonly MemoryStore s_store = new();

        private class StoreProvider : IStoreProvider
        {
            public string Name => "NeoBuildProvider";

            public IStore GetStore(string path) => s_store;
        }

        static TestNode()
        {
            var walletModel = TestObjectHelper.CreateTestWalletModel();
            Wallet = new(walletModel, ((dynamic)walletModel.Extra!).ProtocolConfiguration.ToObject());
            ApplicationEngine.Provider = new TestApplicationEngineProvider(new(), FactoryLogger);
            NeoSystem = new(Wallet.ProtocolSettings, new StoreProvider());
        }
    }
}
