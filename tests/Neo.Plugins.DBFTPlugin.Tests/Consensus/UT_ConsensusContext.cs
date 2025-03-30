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
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Tests.TestUtils;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public partial class UT_ConsensusContext
    {
        private static readonly ProtocolSettings s_protocolSettings = new ProtocolSettings()
        {
            Network = 0x334F454E,
            AddressVersion = 0x35,
            MillisecondsPerBlock = 1000,
            ValidatorsCount = 4,
            StandbyCommittee = new ECPoint[]
            {
                // private key: [0] => 0x01 * 32, [1] => 0x02 * 32, [2] => 0x03 * 32, [3] => 0x04 * 32
                ECPoint.Parse("026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16", ECCurve.Secp256r1),
                ECPoint.Parse("02550f471003f3df97c3df506ac797f6721fb1a1fb7b8f6f83d224498a65c88e24", ECCurve.Secp256r1),
                ECPoint.Parse("02591ab771ebbcfd6d9cb9094d106528add1a69d44c2c1f627f089ec58b9c61adf", ECCurve.Secp256r1),
                ECPoint.Parse("0273103ec30b3ccf57daae08e93534aef144a35940cf6bbba12a0cf7cbd5d65a64", ECCurve.Secp256r1)
            },
            SeedList = new string[] { "seed1.neo.org:10333" }
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
            var wallet = MockConsensusComponents.CreateTestWallet(settings: s_protocolSettings);
            var system = MockConsensusComponents.CreateTestSystem(s_protocolSettings);
            var context = new ConsensusContext(system, MockConsensusComponents.SSettings, wallet);

            context.Reset(0);
            Assert.AreEqual(0, context.MyIndex);

            var validators = s_protocolSettings.StandbyCommittee;
            Assert.AreEqual(4, validators.ToArray().Length);
        }

        [TestMethod]
        public void TestMakeCommit()
        {
            var wallet = MockConsensusComponents.CreateTestWallet(settings: s_protocolSettings);
            var system = MockConsensusComponents.CreateTestSystem(s_protocolSettings);
            var context = new ConsensusContext(system, MockConsensusComponents.SSettings, wallet);

            context.Reset(0);

            context.Block = new Block
            {
                Header = new Header { PrevHash = UInt256.Zero, Index = 1, NextConsensus = UInt160.Zero },
                Transactions = new Transaction[0]
            };
            context.TransactionHashes = new UInt256[0];

            var payload = context.MakeCommit();
            Assert.IsNotNull(payload);
            Assert.IsTrue(ReferenceEquals(payload, context.MakeCommit()));
            Assert.IsNotNull(payload.Witness);

            var data = context.CommitPayloads[context.MyIndex].Data;
            var commit = new Commit();
            var reader = new MemoryReader(data);
            ((ISerializable)commit).Deserialize(ref reader);
            Assert.AreEqual(1u, commit.BlockIndex);
            Assert.AreEqual(0, commit.ValidatorIndex);
            Assert.AreEqual(0, commit.ViewNumber);
            Assert.AreEqual(64, commit.Signature.Length);

            var signData = context.EnsureHeader().GetSignData(s_protocolSettings.Network);
            Assert.IsTrue(Crypto.VerifySignature(signData, commit.Signature.Span, context.Validators[context.MyIndex]));
        }

        [TestMethod]
        public void TestMakePrepareRequest()
        {
            var system = MockConsensusComponents.CreateTestSystem();
            var wallet = MockConsensusComponents.CreateTestWallet("123", 0);
            var context = MockConsensusComponents.CreateConsensusContext(system, null, wallet, 0);

            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.TransactionHashes = new UInt256[0];

            // Ensure we are the primary
            Assert.IsTrue(context.IsPrimary);

            // Make PrepareRequest
            var payload = context.MakePrepareRequest();
            Assert.IsNotNull(payload);
            Assert.IsTrue(ReferenceEquals(payload, context.MakePrepareRequest())); // Should cache result

            // Extract the PrepareRequest from the payload
            var data = payload.Data;
            var prepareRequest = new PrepareRequest();
            var reader = new MemoryReader(data);
            ((ISerializable)prepareRequest).Deserialize(ref reader);

            // Verify fields
            Assert.AreEqual(1u, prepareRequest.BlockIndex);
            Assert.AreEqual(0, prepareRequest.ValidatorIndex);
            Assert.AreEqual(0, prepareRequest.ViewNumber);
            Assert.AreEqual(UInt256.Zero, prepareRequest.PrevHash);
            Assert.AreEqual(0, prepareRequest.TransactionHashes.Length);
        }

        [TestMethod]
        public void TestMakePrepareResponse()
        {
            var system = MockConsensusComponents.CreateTestSystem();
            var wallet = MockConsensusComponents.CreateTestWallet("123", 1);
            var context = MockConsensusComponents.CreateConsensusContext(system, null, wallet, 1);

            context.Reset(0);

            // Create a PrepareRequest payload for primary and add it to context
            var primaryWallet = MockConsensusComponents.CreateTestWallet("123", 0);
            var primaryContext = MockConsensusComponents.CreateConsensusContext(system, null, primaryWallet, 0);
            primaryContext.Reset(0);
            primaryContext.Block = MockConsensusComponents.CreateTestBlock();
            primaryContext.TransactionHashes = new UInt256[0];

            var prepareRequestPayload = primaryContext.MakePrepareRequest();
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.TransactionHashes = new UInt256[0];
            context.PreparationPayloads[0] = prepareRequestPayload;

            // Make PrepareResponse
            var payload = context.MakePrepareResponse();
            Assert.IsNotNull(payload);

            // Extract the PrepareResponse from the payload
            var data = payload.Data;
            var prepareResponse = new PrepareResponse();
            var reader = new MemoryReader(data);
            ((ISerializable)prepareResponse).Deserialize(ref reader);

            // Verify fields
            Assert.AreEqual(1u, prepareResponse.BlockIndex);
            Assert.AreEqual(1, prepareResponse.ValidatorIndex);
            Assert.AreEqual(0, prepareResponse.ViewNumber);
            Assert.AreEqual(prepareRequestPayload.Hash, prepareResponse.PreparationHash);
        }

        [TestMethod]
        public void TestMakeChangeView()
        {
            var system = MockConsensusComponents.CreateTestSystem();
            var wallet = MockConsensusComponents.CreateTestWallet("123", 1);
            var context = MockConsensusComponents.CreateConsensusContext(system, null, wallet, 1);

            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.TransactionHashes = new UInt256[0];

            // Make ChangeView for reason Timeout
            var payload = context.MakeChangeView(ChangeViewReason.Timeout);
            Assert.IsNotNull(payload);

            // Extract the ChangeView from the payload
            var data = payload.Data;
            var changeView = new ChangeView();
            var reader = new MemoryReader(data);
            ((ISerializable)changeView).Deserialize(ref reader);

            // Verify fields
            Assert.AreEqual(1u, changeView.BlockIndex);
            Assert.AreEqual(1, changeView.ValidatorIndex);
            Assert.AreEqual(0, changeView.ViewNumber);
            Assert.AreEqual(1, changeView.NewViewNumber);
            Assert.AreEqual(ChangeViewReason.Timeout, changeView.Reason);
        }

        [TestMethod]
        public void TestGetMessage()
        {
            var system = MockConsensusComponents.CreateTestSystem();
            var wallet = MockConsensusComponents.CreateTestWallet("123", 0);
            var context = MockConsensusComponents.CreateConsensusContext(system, null, wallet, 0);

            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.TransactionHashes = new UInt256[0];

            // Test with PrepareRequest message
            var prepareRequestPayload = context.MakePrepareRequest();
            var prepareRequest = context.GetMessage<PrepareRequest>(prepareRequestPayload);
            Assert.IsNotNull(prepareRequest);
            Assert.AreEqual(0, prepareRequest.ValidatorIndex);
            Assert.AreEqual(1u, prepareRequest.BlockIndex);

            // Test with ChangeView message
            var changeViewPayload = context.MakeChangeView(ChangeViewReason.Timeout);
            var changeView = context.GetMessage<ChangeView>(changeViewPayload);
            Assert.IsNotNull(changeView);
            Assert.AreEqual(0, changeView.ValidatorIndex);
            Assert.AreEqual(1u, changeView.BlockIndex);
            Assert.AreEqual(ChangeViewReason.Timeout, changeView.Reason);
        }
    }
}
