// Copyright (C) 2015-2025 The Neo Project.
//
// MockConsensusComponents.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence.Providers;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Neo.Plugins.DBFTPlugin.Tests.TestUtils
{
    public class MockTimeProvider : TimeProvider
    {
        private DateTime currentTime = DateTime.UtcNow;
        private readonly object _lock = new object();

        public override DateTime UtcNow
        {
            get
            {
                lock (_lock)
                {
                    return currentTime;
                }
            }
        }

        public void SetCurrentTime(DateTime time)
        {
            lock (_lock)
            {
                currentTime = time;
            }
        }

        public void AdvanceSeconds(int seconds)
        {
            lock (_lock)
            {
                currentTime = currentTime.AddSeconds(seconds);
            }
        }

        public void AdvanceMilliseconds(int milliseconds)
        {
            lock (_lock)
            {
                currentTime = currentTime.AddMilliseconds(milliseconds);
            }
        }
    }

    /// <summary>
    /// Provides mock components for DBFT consensus testing
    /// </summary>
    public static class MockConsensusComponents
    {
        private static readonly MockTimeProvider mockTimeProvider = new MockTimeProvider();
        // Default test timeout to prevent tests from hanging indefinitely
        public static readonly TimeSpan DefaultTestTimeout = TimeSpan.FromSeconds(30);

        static MockConsensusComponents()
        {
            // Use reflection to set the Current property since it's read-only
            typeof(TimeProvider)
                .GetField("_current", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, mockTimeProvider);
        }

        public static void AdvanceTime(int seconds)
        {
            mockTimeProvider.AdvanceSeconds(seconds);
        }

        public static void AdvanceTimeMilliseconds(int milliseconds)
        {
            mockTimeProvider.AdvanceMilliseconds(milliseconds);
        }

        public static void ResetTime()
        {
            mockTimeProvider.SetCurrentTime(DateTime.UtcNow);
        }

        /// <summary>
        /// Safely advances time until a condition is met or timeout is reached
        /// </summary>
        /// <param name="condition">The condition to check</param>
        /// <param name="maxAdvances">Maximum number of time advances</param>
        /// <param name="advanceInterval">Time to advance in milliseconds each step</param>
        /// <returns>True if condition was met, false if timeout occurred</returns>
        public static bool AdvanceTimeUntilCondition(Func<bool> condition, int maxAdvances = 100, int advanceInterval = 100)
        {
            for (int i = 0; i < maxAdvances; i++)
            {
                if (condition())
                    return true;

                AdvanceTimeMilliseconds(advanceInterval);
            }

            return false;
        }

        /// <summary>
        /// Creates mock configuration for testing
        /// </summary>
        public static IConfigurationSection CreateMockConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "PluginConfiguration:IgnoreRecoveryLogs", "true" },
                    { "PluginConfiguration:Network", "0x334F454E" },
                    { "PluginConfiguration:AutoStart", "false" },
                    { "PluginConfiguration:MaxBlockSize", "2000000" },
                    { "PluginConfiguration:MaxBlockSystemFee", "150000000000" },
                    { "PluginConfiguration:ExceptionPolicy", "2" }
                })
                .Build()
                .GetSection("PluginConfiguration");
        }

        public static readonly Settings SSettings = new Settings(CreateMockConfig());

        /// <summary>
        /// Creates a test NeoSystem with the specified protocol settings
        /// </summary>
        public static NeoSystem CreateTestSystem(ProtocolSettings settings = null)
        {
            settings ??= new ProtocolSettings
            {
                Network = 0x334F454E,
                AddressVersion = 0x35,
                MillisecondsPerBlock = 1000,
                ValidatorsCount = 4,
                StandbyCommittee = new ECPoint[]
                {
                    ECPoint.Parse("026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16", ECCurve.Secp256r1),
                    ECPoint.Parse("02550f471003f3df97c3df506ac797f6721fb1a1fb7b8f6f83d224498a65c88e24", ECCurve.Secp256r1),
                    ECPoint.Parse("02591ab771ebbcfd6d9cb9094d106528add1a69d44c2c1f627f089ec58b9c61adf", ECCurve.Secp256r1),
                    ECPoint.Parse("0273103ec30b3ccf57daae08e93534aef144a35940cf6bbba12a0cf7cbd5d65a64", ECCurve.Secp256r1)
                },
                SeedList = new string[] { "seed1.neo.org:10333" }
            };

            // Create a memory store for testing
            var store = new MemoryStoreProvider();

            return new NeoSystem(settings, store);
        }

        /// <summary>
        /// Creates a test wallet with an optional password
        /// </summary>
        public static Wallet CreateTestWallet(string password = "123", byte index = 0, ProtocolSettings settings = null)
        {
            // Ensure we have valid settings
            settings ??= CreateTestSystem().Settings;

            // Create a temporary wallet file path with .json extension
            string path = Path.ChangeExtension(Path.GetTempFileName(), ".json");
            string name = "TestWallet";

            // Create the wallet with required parameters
            var wallet = Wallet.Create(name, path, password, settings);
            if (wallet == null)
                throw new InvalidOperationException("Failed to create wallet. Make sure NEP6WalletFactory is registered.");

            // Create a valid private key (derived from index)
            byte[] privateKey = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            // Ensure first byte is the index for deterministic testing
            privateKey[0] = index;

            // Add account with the private key
            wallet.CreateAccount(privateKey);

            return wallet;
        }

        /// <summary>
        /// Creates a ConsensusContext for testing
        /// </summary>
        public static ConsensusContext CreateConsensusContext(
            NeoSystem system = null,
            ECPoint[] validators = null,
            Wallet wallet = null,
            byte myIndex = 0)
        {
            system ??= CreateTestSystem();

            // Create validators if not provided
            validators ??= Enumerable.Range(0, 4)
                .Select(i =>
                {
                    byte[] privateKey = new byte[32];
                    privateKey[0] = (byte)i;
                    return Neo.Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
                })
                .ToArray();

            wallet ??= CreateTestWallet(settings: system.Settings);

            // Create the context
            return new ConsensusContext(system, SSettings, wallet);
        }


        internal static ConsensusService CreateConsensusService(ConsensusContext context = null)
        {
            var system = MockConsensusComponents.CreateTestSystem();
            var settings = new Settings(MockConsensusComponents.CreateMockConfig());
            var wallet = MockConsensusComponents.CreateTestWallet(settings: system.Settings);
            context ??= MockConsensusComponents.CreateConsensusContext(system, null, wallet);

            // Use reflection to create instance without invoking the public constructor
            var constructor = typeof(ConsensusService).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(NeoSystem), typeof(Settings), typeof(ConsensusContext) },
                null);

            return constructor.Invoke(new object[] { system, settings, context }) as ConsensusService;
        }

        /// <summary>
        /// Creates a test block for consensus testing
        /// </summary>
        public static Block CreateTestBlock(uint index = 1)
        {
            var version = 0;
            var previousHash = UInt256.Zero;
            var merkleRoot = UInt256.Zero;
            var timestamp = (ulong)mockTimeProvider.UtcNow.ToTimestampMS();
            var nonce = 12345UL;
            var nextConsensus = UInt160.Zero;
            var witness = new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() };

            var transactions = new Transaction[]
            {
                new Transaction
                {
                    Version = 0,
                    Nonce = 12345,
                    SystemFee = 1000,
                    NetworkFee = 500,
                    ValidUntilBlock = 1000,
                    Attributes = Array.Empty<TransactionAttribute>(),
                    Signers = new Signer[] { new Signer { Account = UInt160.Zero, Scopes = WitnessScope.CalledByEntry } },
                    Witnesses = new Witness[] { witness },
                    Script = Array.Empty<byte>()
                }
            };

            return new Block
            {
                Header = new Header
                {
                    Version = (uint)version,
                    PrevHash = previousHash,
                    MerkleRoot = merkleRoot,
                    Timestamp = timestamp,
                    Index = index,
                    NextConsensus = nextConsensus,
                    Witness = witness,
                    Nonce = nonce
                },
                Transactions = transactions
            };
        }
    }
}
