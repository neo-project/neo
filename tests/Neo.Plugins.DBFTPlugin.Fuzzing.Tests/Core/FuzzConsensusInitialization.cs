// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensusInitialization.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.UnitTests.Persistence;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    public partial class FuzzConsensus
    {
        // Static constructor for one-time setup
        static FuzzConsensus()
        {
            var protocolSettings = ProtocolSettings.Default with
            {
                Network = 0x334F454Eu,
                ValidatorsCount = 7
            };

            // Use a persistent path for fuzzing artifacts if needed, otherwise TestMemoryStoreProvider is fine
            var memoryStore = new MemoryStore();
            var storeProvider = new TestMemoryStoreProvider(memoryStore);
            _system = new NeoSystem(protocolSettings, storeProvider);

            // Create a test wallet directly instead of using TestUtils
            JObject wallet = new JObject();
            wallet["name"] = "fuzz_wallet";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = ScryptParameters.Default.ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = null;
            _wallet = new NEP6Wallet(null, "password", protocolSettings, wallet);

            // Ensure the wallet has an account corresponding to a validator
            var privateKey = new byte[32];
            Array.Fill(privateKey, (byte)1); // Corresponds to the first validator
            _wallet.CreateAccount(privateKey);

            // Create a minimal configuration for Settings
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                {"DBFTPlugin:RecoveryLogs", "ConsensusState"},
                {"DBFTPlugin:IgnoreRecoveryLogs", "false"},
                {"DBFTPlugin:AutoStart", "false"},
                {"DBFTPlugin:MaxBlockSystemFee", "1000000000"}
            }).Build();

            _settings = new Settings(config.GetSection("DBFTPlugin"));
            InitializeContext();
        }

        // Helper to initialize or reset the context
        private static void InitializeContext()
        {
            // Create a new context for each run or reset carefully
            _context = new ConsensusContext(_system, _settings, _wallet);
            _context.Reset(0); // Initialize for block 0, view 0

            // Pre-populate required fields if necessary for message processing
            // Create a new block and assign it to the context
            _context.Block = new Block
            {
                Header = new Header
                {
                    Index = 0,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Timestamp = 0,
                    Nonce = 0,
                    NextConsensus = UInt160.Zero
                },
                Transactions = Array.Empty<Transaction>()
            };

            _context.TransactionHashes = Array.Empty<UInt256>();
            _context.Transactions = new Dictionary<UInt256, Transaction>();
        }

        // Helper method to simulate GetPrimaryIndex from ConsensusContext
        private static byte GetPrimaryIndex(ConsensusContext context, byte viewNumber)
        {
            return (byte)((context.Block.Index - viewNumber) % context.Validators.Length);
        }
    }
}
