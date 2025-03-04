// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ConsensusContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Persistence;
using System;
using System.Collections.Generic;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_ConsensusContext
    {
        static readonly ProtocolSettings ProtocolSettings = ProtocolSettings.Default with
        {
            Network = 0x334F454Eu,
            StandbyCommittee =
           [
                // private key: [0] => 0x01 * 32, [1] => 0x02 * 32, [2] => 0x03 * 32, [3] => 0x04 * 32
                ECPoint.Parse("026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16", ECCurve.Secp256r1),
                ECPoint.Parse("02550f471003f3df97c3df506ac797f6721fb1a1fb7b8f6f83d224498a65c88e24", ECCurve.Secp256r1),
                ECPoint.Parse("02591ab771ebbcfd6d9cb9094d106528add1a69d44c2c1f627f089ec58b9c61adf", ECCurve.Secp256r1),
                ECPoint.Parse("0273103ec30b3ccf57daae08e93534aef144a35940cf6bbba12a0cf7cbd5d65a64", ECCurve.Secp256r1),
            ],
            ValidatorsCount = 4,
            SeedList = ["seed1.neo.org:10333"],
        };

        private static IConfigurationSection MockConfig()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "PluginConfiguration:IgnoreRecoveryLogs", "true" },
                    { "PluginConfiguration:Network", "0x334F454E" },
                })
                .Build()
                .GetSection("PluginConfiguration");
        }

        [TestMethod]
        public void TestReset()
        {
            var config = MockConfig();
            var wallet = TestUtils.GenerateTestWallet("123");
            var system = new NeoSystem(ProtocolSettings, new TestMemoryStoreProvider(new()));
            var context = new ConsensusContext(system, new Settings(config), wallet);
            context.Reset(0);
            Assert.AreEqual(-1, context.MyIndex);

            var validators = NativeContract.NEO.GetNextBlockValidators(system.GetSnapshotCache(), 4);
            Assert.AreEqual(4, validators.Length);

            var privateKey = new byte[32];
            Array.Fill(privateKey, (byte)1);
            wallet.CreateAccount(privateKey);

            context = new ConsensusContext(system, new Settings(config), wallet);
            context.Reset(0);
            Assert.AreEqual(2, context.MyIndex);
        }

        [TestMethod]
        public void TestMakeCommit()
        {
            var config = MockConfig();
            var wallet = TestUtils.GenerateTestWallet("123");
            var system = new NeoSystem(ProtocolSettings, new TestMemoryStoreProvider(new()));

            var privateKey = new byte[32];
            Array.Fill(privateKey, (byte)1);
            wallet.CreateAccount(privateKey);

            var context = new ConsensusContext(system, new Settings(config), wallet);
            context.Reset(0);

            context.Block = new()
            {
                Header = new() { PrevHash = UInt256.Zero, Index = 1, NextConsensus = UInt160.Zero },
                Transactions = []
            };
            context.TransactionHashes = [];

            var payload = context.MakeCommit();
            Assert.IsNotNull(payload);
            Assert.IsTrue(ReferenceEquals(payload, context.MakeCommit()));
            Assert.IsNotNull(payload.Witness);

            var data = context.CommitPayloads[context.MyIndex].Data;
            var commit = new Commit();
            var reader = new MemoryReader(data);
            ((ISerializable)commit).Deserialize(ref reader);
            Assert.AreEqual(1u, commit.BlockIndex);
            Assert.AreEqual(2, commit.ValidatorIndex);
            Assert.AreEqual(0, commit.ViewNumber);
            Assert.AreEqual(64, commit.Signature.Length);

            var signData = context.EnsureHeader().GetSignData(ProtocolSettings.Network);
            Assert.IsTrue(Crypto.VerifySignature(signData, commit.Signature.Span, context.Validators[context.MyIndex]));
        }
    }
}
