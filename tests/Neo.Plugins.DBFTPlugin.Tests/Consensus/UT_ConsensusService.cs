// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ConsensusService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Tests.TestUtils;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract;
using System.Reflection;

namespace Neo.Plugins.DBFTPlugin.Tests.Consensus
{
    [TestClass]
    public class UT_ConsensusService
    {
        private readonly UInt256 testBlockHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001");
        private readonly UInt256 testTxHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000002");

        [TestMethod]
        public void TestConsensusServiceProps()
        {
            var system = MockConsensusComponents.CreateTestSystem();
            var settings = new Settings(MockConsensusComponents.CreateMockConfig());
            var signer = MockConsensusComponents.CreateTestWallet();

            // Test the Props factory method
            var props = ConsensusService.Props(system, settings, signer);
            Assert.IsNotNull(props);
        }

        [TestMethod]
        public void TestInitializeConsensus()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            MethodInfo initializeConsensusMethod = typeof(ConsensusService).GetMethod(
                "InitializeConsensus",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Set up the context
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();

            // Call the method
            initializeConsensusMethod.Invoke(service, [(byte)0]);

            // Verify context was properly initialized
            Assert.AreEqual(0, context.ViewNumber);
            Assert.IsNotNull(context.Block);
            Assert.AreEqual(1u, context.Block.Index);
        }

        [TestMethod]
        public void TestOnStart()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private field and method via reflection
            var startedField = typeof(ConsensusService).GetField(
                "started",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var onStartMethod = typeof(ConsensusService).GetMethod(
                "OnStart",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Verify initial state
            Assert.IsFalse((bool)startedField.GetValue(service));

            // Call the method
            onStartMethod.Invoke(service, null);

            // Verify started was set to true
            Assert.IsTrue((bool)startedField.GetValue(service));
        }

        [TestMethod]
        public void TestRequestChangeView()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            var requestChangeViewMethod = typeof(ConsensusService).GetMethod(
                "RequestChangeView",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Set up the context
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();

            // Call the method for a timeout reason
            requestChangeViewMethod.Invoke(service, [ChangeViewReason.Timeout]);

            // Verify the view change request was created
            Assert.IsNotNull(context.ChangeViewPayloads[context.MyIndex]);

            // Extract message for verification
            var changeView = context.GetMessage<ChangeView>(context.ChangeViewPayloads[context.MyIndex]);
            Assert.IsNotNull(changeView);
            Assert.AreEqual(ChangeViewReason.Timeout, changeView.Reason);
            Assert.AreEqual(1, changeView.NewViewNumber);
        }

        [TestMethod]
        public void TestSendPrepareRequest()
        {
            var context = MockConsensusComponents.CreateConsensusContext(
                null, null, MockConsensusComponents.CreateTestWallet("123", 0), 0);
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            var sendPrepareRequestMethod = typeof(ConsensusService).GetMethod(
                "SendPrepareRequest",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Set up the context to be primary
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            Assert.IsTrue(context.IsPrimary); // Ensure we're the primary

            // Call the method
            sendPrepareRequestMethod.Invoke(service, null);

            // Verify a PrepareRequest was created and stored
            Assert.IsNotNull(context.PreparationPayloads[context.MyIndex]);

            // Extract message for verification
            var prepareRequest = context.GetMessage<PrepareRequest>(context.PreparationPayloads[context.MyIndex]);
            Assert.IsNotNull(prepareRequest);
            Assert.AreEqual(context.Block.Index, prepareRequest.BlockIndex);
            Assert.AreEqual(context.MyIndex, prepareRequest.ValidatorIndex);
            Assert.AreEqual(context.ViewNumber, prepareRequest.ViewNumber);
        }

        [TestMethod]
        public void TestOnPrepareRequestReceived()
        {
            var context = MockConsensusComponents.CreateConsensusContext(
                null, null, MockConsensusComponents.CreateTestWallet("123", 1), 1);
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            var onPrepareRequestReceivedMethod = typeof(ConsensusService).GetMethod(
                "OnPrepareRequestReceived",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Set up the context
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();

            // Create a valid PrepareRequest message
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 0, // Primary
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = []
            };

            // Create the payload
            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                Data = prepareRequest.ToArray(),
                ValidBlockStart = 0,
                ValidBlockEnd = 1000,
                Sender = Contract.CreateSignatureRedeemScript(context.Validators[0]).ToScriptHash()
            };

            // Call the method
            onPrepareRequestReceivedMethod.Invoke(service, [payload, prepareRequest]);

            // Verify the PrepareRequest was processed and stored
            Assert.IsNotNull(context.PreparationPayloads[0]);
            Assert.AreEqual(payload, context.PreparationPayloads[0]);

            // Verify block timestamp was updated to match the request
            Assert.AreEqual(prepareRequest.Timestamp, context.Block.Header.Timestamp);
            Assert.AreEqual(prepareRequest.Nonce, context.Block.Header.Nonce);
        }

        [TestMethod]
        public void TestOnPrepareResponseReceived()
        {
            var context = MockConsensusComponents.CreateConsensusContext(
                null, null, MockConsensusComponents.CreateTestWallet("123", 0), 0);
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            MethodInfo onPrepareResponseReceivedMethod = typeof(ConsensusService).GetMethod(
                "OnPrepareResponseReceived",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Set up the context
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();

            // Create a prepare request as primary
            var prepareRequestPayload = context.MakePrepareRequest();
            context.PreparationPayloads[context.MyIndex] = prepareRequestPayload;

            // Create a valid PrepareResponse message from backup validator
            var prepareResponse = new PrepareResponse
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 1, // Backup
                ViewNumber = 0,
                PreparationHash = prepareRequestPayload.Hash
            };

            // Create the payload
            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                Data = prepareResponse.ToArray(),
                ValidBlockStart = 0,
                ValidBlockEnd = 1000,
                Sender = Contract.CreateSignatureRedeemScript(context.Validators[1]).ToScriptHash()
            };

            // Call the method
            onPrepareResponseReceivedMethod.Invoke(service, new object[] { payload, prepareResponse });

            // Verify the PrepareResponse was processed and stored
            Assert.IsNotNull(context.PreparationPayloads[1]);
            Assert.AreEqual(payload, context.PreparationPayloads[1]);
        }
    }
}
