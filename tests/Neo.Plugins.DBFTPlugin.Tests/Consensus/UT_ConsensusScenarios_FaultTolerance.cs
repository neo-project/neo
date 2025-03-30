// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ConsensusScenarios_FaultTolerance.cs file belongs to the neo project and is free
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.Plugins.DBFTPlugin.Tests.Consensus
{
    [TestClass]
    public class UT_ConsensusScenarios_FaultTolerance : ConsensusTestBase
    {
        private void InvokeOnReceive(ConsensusService service, object message)
        {
            // Use reflection to invoke the OnReceive method
            var onReceiveMethod = typeof(ConsensusService).GetMethod(
                "OnReceive",
                BindingFlags.NonPublic | BindingFlags.Instance);

            onReceiveMethod.Invoke(service, new[] { message });
        }

        private ExtensiblePayload CreateConsensusPayload(ConsensusMessage message, ConsensusContext context, byte validatorIndex)
        {
            // Create the payload
            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                Data = message.ToArray(),
                ValidBlockStart = 0,
                ValidBlockEnd = context.Block.Index + 1,
                Sender = Contract.CreateSignatureRedeemScript(context.Validators[validatorIndex]).ToScriptHash(),
                // No need for actual signature in tests
                Witness = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            };

            return payload;
        }

        [TestMethod]
        [Timeout(5000)] // Reduce test timeout to 5 seconds
        [TestCategory("Consensus")]
        [TestCategory("FaultTolerance")]
        public void TestPrimaryNodeFailure()
        {
            // Create contexts for multiple validators
            var system = MockConsensusComponents.CreateTestSystem();
            var validators = Enumerable.Range(0, 4)
                .Select(i =>
                {
                    var privateKey = new byte[32];
                    privateKey[0] = (byte)i;
                    return Neo.Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
                })
                .ToArray();

            var backupWallet = MockConsensusComponents.CreateTestWallet("123", 1);
            var backupContext = MockConsensusComponents.CreateConsensusContext(system, validators, backupWallet, 1);
            var backupService = MockConsensusComponents.CreateConsensusService(backupContext);

            // Store in base class variables for cleanup
            context = backupContext;
            service = backupService;
            this.system = system;

            // Reset context for testing
            backupContext.Reset(0);
            backupContext.Block = MockConsensusComponents.CreateTestBlock();
            backupContext.ViewNumber = 0;

            // Advance time to simulate timeout
            MockConsensusComponents.AdvanceTime(1);

            // Simulate primary node failure by timeout at backup node
            var changeView = new ChangeView
            {
                BlockIndex = backupContext.Block.Index,
                ValidatorIndex = 1, // From backup
                ViewNumber = 0,
                Reason = ChangeViewReason.Timeout,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS()
            };

            var changeViewPayload = CreateConsensusPayload(changeView, backupContext, 1);
            backupContext.ChangeViewPayloads[1] = changeViewPayload;

            // Simulate other nodes also sending ChangeView (meeting consensus threshold)
            for (int i = 2; i < validators.Length; i++)
            {
                var otherChangeView = new ChangeView
                {
                    BlockIndex = backupContext.Block.Index,
                    ValidatorIndex = (byte)i,
                    ViewNumber = 0,
                    Reason = ChangeViewReason.Timeout,
                    Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS()
                };

                var otherPayload = CreateConsensusPayload(otherChangeView, backupContext, (byte)i);
                backupContext.ChangeViewPayloads[i] = otherPayload;
            }

            // Call private method to check for view change with timeout protection
            var checkExpectedViewMethod = typeof(ConsensusService).GetMethod(
                "CheckExpectedView",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(byte) },
                null);

            var result = checkExpectedViewMethod.Invoke(backupService, new object[] { 1 });

            // Should have changed to view 1
            Assert.IsTrue((bool)result);
            Assert.AreEqual(1, backupContext.ViewNumber);

            // Verify that validator 1 becomes the new primary in view 1
            Assert.AreEqual(1, backupContext.GetPrimaryIndex(backupContext.ViewNumber));
        }

        [TestMethod]
        [Timeout(5000)] // Reduce test timeout to 5 seconds
        [TestCategory("Consensus")]
        [TestCategory("FaultTolerance")]
        public void TestPartialNodeFailure()
        {
            // Create contexts for multiple validators
            var system = MockConsensusComponents.CreateTestSystem();
            var validators = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var privateKey = new byte[32];
                    privateKey[0] = (byte)i;
                    return Neo.Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
                })
                .ToArray();

            var primaryWallet = MockConsensusComponents.CreateTestWallet("123", 0);
            var primaryContext = MockConsensusComponents.CreateConsensusContext(system, validators, primaryWallet, 0);
            var primaryService = MockConsensusComponents.CreateConsensusService(primaryContext);

            // Store in base class variables for cleanup
            context = primaryContext;
            service = primaryService;
            this.system = system;

            // Reset context for testing
            primaryContext.Reset(0);
            primaryContext.Block = MockConsensusComponents.CreateTestBlock();
            primaryContext.ViewNumber = 0;
            primaryContext.TransactionHashes = new UInt256[0];

            // Primary sends PrepareRequest
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = primaryContext.Block.Index,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, primaryContext, 0);
            primaryContext.PreparationPayloads[0] = prepareRequestPayload;

            // Simulate only some backup nodes responding (nodes 1, 2, 3 - enough for consensus)
            for (int i = 1; i <= 3; i++)
            {
                var prepareResponse = new PrepareResponse
                {
                    BlockIndex = primaryContext.Block.Index,
                    ValidatorIndex = (byte)i,
                    ViewNumber = 0,
                    PreparationHash = prepareRequestPayload.Hash
                };

                var prepareResponsePayload = CreateConsensusPayload(prepareResponse, primaryContext, (byte)i);
                primaryContext.PreparationPayloads[i] = prepareResponsePayload;
            }

            // Call private method to check if preparations are complete
            var checkPreparationsMethod = typeof(ConsensusService).GetMethod(
                "CheckPreparations",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var result = checkPreparationsMethod.Invoke(primaryService, null);

            // Should have enough preparations for consensus
            Assert.IsTrue((bool)result);

            // Verify we can move to commit phase with partial node failures
            primaryContext.CommitPayloads = new ExtensiblePayload[validators.Length];

            // Simulate commits from the active nodes
            for (int i = 0; i <= 3; i++)
            {
                var commit = new Commit
                {
                    BlockIndex = primaryContext.Block.Index,
                    ValidatorIndex = (byte)i,
                    ViewNumber = 0,
                    Signature = new byte[64] // Dummy signature
                };

                var commitPayload = CreateConsensusPayload(commit, primaryContext, (byte)i);
                primaryContext.CommitPayloads[i] = commitPayload;
            }

            // Call method to check commits
            var checkCommitsMethod = typeof(ConsensusService).GetMethod(
                "CheckCommits",
                BindingFlags.NonPublic | BindingFlags.Instance);

            result = checkCommitsMethod.Invoke(primaryService, null);

            // Should have enough commits for consensus
            Assert.IsTrue((bool)result);
        }

        [TestMethod]
        [Timeout(5000)] // Reduce test timeout to 5 seconds
        [TestCategory("Consensus")]
        [TestCategory("FaultTolerance")]
        public void TestMultipleViewChanges()
        {
            // Create contexts for multiple validators
            var system = MockConsensusComponents.CreateTestSystem();
            var validators = Enumerable.Range(0, 4)
                .Select(i =>
                {
                    byte[] privateKey = new byte[32];
                    privateKey[0] = (byte)i;
                    return Neo.Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
                })
                .ToArray();

            var backupWallet = MockConsensusComponents.CreateTestWallet("123", 1);
            var backupContext = MockConsensusComponents.CreateConsensusContext(system, validators, backupWallet, 1);
            var backupService = MockConsensusComponents.CreateConsensusService(backupContext);

            // Store in base class variables for cleanup
            context = backupContext;
            service = backupService;
            this.system = system;

            // Reset context for testing
            backupContext.Reset(0);
            backupContext.Block = MockConsensusComponents.CreateTestBlock();
            backupContext.ViewNumber = 0;

            // Add a timeout to ensure we don't get stuck in a loop
            int maxViews = 2;
            int currentView = 0;

            // Simulate view changes through multiple views (0->1->2)
            for (byte viewNumber = 0; viewNumber < maxViews; viewNumber++)
            {
                // Clear previous ChangeView payloads
                backupContext.ChangeViewPayloads = new ExtensiblePayload[validators.Length];

                // Simulate nodes sending ChangeView for view viewNumber+1
                for (int i = 0; i < validators.Length; i++)
                {
                    var changeView = new ChangeView
                    {
                        BlockIndex = backupContext.Block.Index,
                        ValidatorIndex = (byte)i,
                        ViewNumber = viewNumber,
                        Reason = ChangeViewReason.Timeout,
                        Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS()
                    };

                    var changeViewPayload = CreateConsensusPayload(changeView, backupContext, (byte)i);
                    backupContext.ChangeViewPayloads[i] = changeViewPayload;
                }

                // Call method to check for view change
                var checkExpectedViewMethod = typeof(ConsensusService).GetMethod(
                    "CheckExpectedView",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(byte) },
                    null);

                var result = checkExpectedViewMethod.Invoke(backupService, new object[] { (byte)(viewNumber + 1) });

                // Should have changed to next view
                Assert.IsTrue((bool)result);
                Assert.AreEqual(viewNumber + 1, backupContext.ViewNumber);

                // Verify the primary index for this view
                var expectedPrimary = (backupContext.Block.Index - backupContext.ViewNumber) % (uint)validators.Length;
                Assert.AreEqual(expectedPrimary, backupContext.GetPrimaryIndex(backupContext.ViewNumber));

                currentView++;
                if (currentView >= maxViews) break; // Safety check
            }
        }

        [TestMethod]
        [Timeout(5000)] // Reduce test timeout to 5 seconds
        [TestCategory("Consensus")]
        [TestCategory("FaultTolerance")]
        public void TestRecoveryMessageProcessing()
        {
            // Create contexts for multiple validators
            var system = MockConsensusComponents.CreateTestSystem();
            var validators = Enumerable.Range(0, 4)
                .Select(i =>
                {
                    byte[] privateKey = new byte[32];
                    privateKey[0] = (byte)i;
                    return Neo.Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
                })
                .ToArray();

            // Create context for a node that needs to catch up
            var recoveringWallet = MockConsensusComponents.CreateTestWallet("123", 3);
            var recoveringContext = MockConsensusComponents.CreateConsensusContext(system, validators, recoveringWallet, 3);
            var recoveringService = MockConsensusComponents.CreateConsensusService(recoveringContext);

            // Store in base class variables for cleanup
            context = recoveringContext;
            service = recoveringService;
            this.system = system;

            // Reset context for testing
            recoveringContext.Reset(0);
            recoveringContext.Block = MockConsensusComponents.CreateTestBlock();
            recoveringContext.ViewNumber = 0;

            // Simulate a node that already has some consensus data
            var primaryWallet = MockConsensusComponents.CreateTestWallet("123", 0);
            var primaryContext = MockConsensusComponents.CreateConsensusContext(system, validators, primaryWallet, 0);

            primaryContext.Reset(0);
            primaryContext.Block = MockConsensusComponents.CreateTestBlock();
            primaryContext.ViewNumber = 1; // Already in view 1

            // Create prepare request from primary in view 1
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = primaryContext.Block.Index,
                ValidatorIndex = 1, // Primary for view 1
                ViewNumber = 1,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, primaryContext, 1);

            // Simulate multiple nodes' payloads for a recovery message
            var preparationPayloads = new ExtensiblePayload[validators.Length];
            preparationPayloads[1] = prepareRequestPayload; // Primary's PrepareRequest

            // Create RecoveryMessage
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = primaryContext.Block.Index,
                ValidatorIndex = 0, // From validator 0
                ViewNumber = 1,
                PreparationHash = prepareRequestPayload.Hash,
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>
                {
                    [1] = new RecoveryMessage.PreparationPayloadCompact
                    {
                        ValidatorIndex = 1,
                        InvocationScript = Array.Empty<byte>()
                    }
                },
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>(),
                PrepareRequestMessage = prepareRequest
            };

            // Create payload
            var recoveryPayload = CreateConsensusPayload(recoveryMessage, primaryContext, 0);

            // Call OnReceive to process recovery message
            InvokeOnReceive(recoveringService, recoveryPayload);

            // Verify the recovering node updated its state  
            Assert.AreEqual(1, recoveringContext.ViewNumber);
            Assert.IsNotNull(recoveringContext.PreparationPayloads[1]);
        }
    }
}
