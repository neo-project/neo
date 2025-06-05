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
using Microsoft.Extensions.Logging.Console;
using Neo.Build.Core.Storage;
using Neo.Build.Core.Wallets;
using Neo.Persistence;
using System.IO;

namespace Neo.Build.Core.Tests.Helpers
{
    internal static class TestNode
    {
        public static readonly NeoSystem NeoSystem;
        public static readonly DevWallet Wallet;
        public static readonly ILoggerFactory FactoryLogger = LoggerFactory.Create(logging =>
        {
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.ColorBehavior = LoggerColorBehavior.Disabled;
            });
        });

        private static readonly FasterDbStore s_store = new(Path.GetRandomFileName());

        private class StoreProvider : IStoreProvider
        {
            public string Name => "NeoBuildProvider";

            public IStore GetStore(string path) => s_store;
        }

        static TestNode()
        {
            var walletModel = TestObjectHelper.CreateTestWalletModel();
            Wallet = new(walletModel);
            NeoSystem = new(Wallet.ProtocolSettings, new StoreProvider());
        }
    }
}
