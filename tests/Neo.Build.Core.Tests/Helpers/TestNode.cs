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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Neo.Build.Core.Tests.Helpers
{
    internal static class TestNode
    {
        public static readonly NeoSystem NeoSystem;
        public static readonly DevWallet Wallet;
        public static readonly ILoggerFactory FactoryLogger = LoggerFactory.Create(logging =>
        {
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.ColorBehavior = LoggerColorBehavior.Disabled;
            });
        });

        private static readonly FasterDbStore s_store = new(Path.GetRandomFileName());

        private class StoreProvider : IStoreProvider
        {
            public string Name => "NeoBuildTestProvider";

            public IStore GetStore(string path) => s_store;
        }

        static TestNode()
        {
            var walletModel = TestObjectHelper.CreateTestWalletModel();
            Wallet = new(walletModel);
            NeoSystem = new(
                Wallet.ProtocolSettings with
                {
                    StandbyCommittee = [.. Wallet.GetConsensusAccounts().Select(static s => s.GetKey().PublicKey)],
                    ValidatorsCount = 1,
                    Hardforks = new Dictionary<Hardfork, uint>()
                    {
                        { Hardfork.HF_Aspidochelone, 0u },
                        { Hardfork.HF_Basilisk, 0u },
                        { Hardfork.HF_Cockatrice, 0u },
                        { Hardfork.HF_Domovoi, 0u },
                        { Hardfork.HF_Echidna, 0u },
                    }.ToImmutableDictionary(),
                },
                new StoreProvider());
        }
    }
}
