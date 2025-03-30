// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ConsensusMessageProcessing.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Tests.TestUtils;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract;
using System;
using System.Reflection;

namespace Neo.Plugins.DBFTPlugin.Tests.Consensus
{
    [TestClass]
    public class UT_ConsensusMessageProcessing
    {


        private ExtensiblePayload CreateConsensusPayload(ConsensusMessage message, ConsensusContext context, byte[] privateKey = null)
        {
            // If no private key provided, use a default one
            privateKey ??= new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };

            // Create the payload
            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                Data = message.ToArray(),
                ValidBlockStart = 0,
                ValidBlockEnd = context.Block.Index + 1,
                Sender = Contract.CreateSignatureRedeemScript(ECCurve.Secp256r1.G * privateKey).ToScriptHash(),
                // No need for actual signature in tests
                Witness = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            };

            return payload;
        }

        private void InvokeOnReceive(ConsensusService service, object message)
        {
            // Use reflection to invoke the OnReceive method
            var onReceiveMethod = typeof(ConsensusService).GetMethod(
                "OnReceive",
                BindingFlags.NonPublic | BindingFlags.Instance);

            onReceiveMethod.Invoke(service, new object[] { message });
        }

        [TestMethod]
        public void TestOnConsensusPayload()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Create PrepareRequest message
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            // Create payload
            var payload = CreateConsensusPayload(prepareRequest, context);

            // Invoke OnReceive
            InvokeOnReceive(service, payload);

            // Verify the PrepareRequest was processed
            Assert.IsNotNull(context.PreparationPayloads[0]);
            Assert.AreEqual(payload, context.PreparationPayloads[0]);
        }

        [TestMethod]
        public void TestInvalidConsensusPayload()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Create an invalid PrepareRequest (wrong block index)
            var invalidPrepareRequest = new PrepareRequest
            {
                BlockIndex = context.Block.Index + 1, // Invalid block index
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            // Create payload
            var payload = CreateConsensusPayload(invalidPrepareRequest, context);

            // Should not throw exception, but payload should be ignored
            InvokeOnReceive(service, payload);

            // Verify no payloads were stored
            Assert.IsNull(context.PreparationPayloads[0]);
        }

        [TestMethod]
        public void TestOnChangeViewReceived()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Create ChangeView message
            var changeView = new ChangeView
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 1, // From another validator
                ViewNumber = 0,
                Reason = ChangeViewReason.Timeout,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS()
            };

            // Create payload
            var payload = CreateConsensusPayload(changeView, context);

            // Invoke OnReceive
            InvokeOnReceive(service, payload);

            // Verify the ChangeView was processed
            Assert.IsNotNull(context.ChangeViewPayloads[1]);
            Assert.AreEqual(payload, context.ChangeViewPayloads[1]);
        }

        [TestMethod]
        public void TestOnCommitReceived()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Create Commit message
            var commit = new Commit
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 1, // From another validator
                ViewNumber = 0,
                Signature = new byte[64] // Dummy signature
            };

            // Create payload
            var payload = CreateConsensusPayload(commit, context);

            // Invoke OnReceive
            InvokeOnReceive(service, payload);

            // Verify the Commit was processed
            Assert.IsNotNull(context.CommitPayloads[1]);
            Assert.AreEqual(payload, context.CommitPayloads[1]);
        }

        [TestMethod]
        public void TestOnRecoveryRequestReceived()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Create a PrepareRequest and store it
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, context);
            context.PreparationPayloads[0] = prepareRequestPayload;

            // Create RecoveryRequest message
            var recoveryRequest = new RecoveryRequest
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 1, // From another validator
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS()
            };

            // Create payload
            var payload = CreateConsensusPayload(recoveryRequest, context);

            // Invoke OnReceive
            InvokeOnReceive(service, payload);

            // We can't easily verify this without mocking network messages,
            // but the method should complete without exceptions
        }

        [TestMethod]
        public void TestOnRecoveryMessageReceived()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Create preparation payloads for recovery message
            var preparationPayloads = new ExtensiblePayload[context.Validators.Length];
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            preparationPayloads[0] = CreateConsensusPayload(prepareRequest, context);

            // Create RecoveryMessage
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 1, // From another validator
                ViewNumber = 0,
                PreparationHash = preparationPayloads[0].Hash,
            };

            // Create payload
            var payload = CreateConsensusPayload(recoveryMessage, context);

            // Invoke OnReceive
            InvokeOnReceive(service, payload);

            // Verify the recovery message was processed
            Assert.IsNotNull(context.PreparationPayloads[0]);
            Assert.AreEqual(preparationPayloads[0], context.PreparationPayloads[0]);
        }

        [TestMethod]
        public void TestCheckPrepareResponse()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            var checkPrepareResponseMethod = typeof(ConsensusService).GetMethod(
                "CheckPrepareResponse",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Setup PrepareRequest from primary (0)
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, context);
            context.PreparationPayloads[0] = prepareRequestPayload;

            // Create PrepareResponse from backup (1)
            var prepareResponse = new PrepareResponse
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 1,
                ViewNumber = 0,
                PreparationHash = prepareRequestPayload.Hash
            };

            var prepareResponsePayload = CreateConsensusPayload(prepareResponse, context);

            // Check the PrepareResponse
            bool result = (bool)checkPrepareResponseMethod.Invoke(service, new object[] { prepareResponse, prepareResponsePayload });

            // Should be valid
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCheckPreparations()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            var checkPreparationsMethod = typeof(ConsensusService).GetMethod(
                "CheckPreparations",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Initially not enough preparations
            bool result = (bool)checkPreparationsMethod.Invoke(service, null);
            Assert.IsFalse(result);

            // Setup PrepareRequest from primary (0)
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, context);
            context.PreparationPayloads[0] = prepareRequestPayload;

            // Create PrepareResponse from backup (1)
            var prepareResponse = new PrepareResponse
            {
                BlockIndex = context.Block.Index,
                ValidatorIndex = 1,
                ViewNumber = 0,
                PreparationHash = prepareRequestPayload.Hash
            };

            var prepareResponsePayload = CreateConsensusPayload(prepareResponse, context);
            context.PreparationPayloads[1] = prepareResponsePayload;

            // Check again with preparations
            result = (bool)checkPreparationsMethod.Invoke(service, null);

            // Should have enough for consensus now
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCheckCommits()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            var checkCommitsMethod = typeof(ConsensusService).GetMethod(
                "CheckCommits",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Initially not enough commits
            bool result = (bool)checkCommitsMethod.Invoke(service, null);
            Assert.IsFalse(result);

            // Create Commit messages from all validators
            for (int i = 0; i < context.Validators.Length; i++)
            {
                var commit = new Commit
                {
                    BlockIndex = context.Block.Index,
                    ValidatorIndex = (byte)i,
                    ViewNumber = 0,
                    Signature = new byte[64] // Dummy signature, not checked in TestCheckCommits
                };

                var commitPayload = CreateConsensusPayload(commit, context);
                context.CommitPayloads[i] = commitPayload;
            }

            // Check again with commits
            result = (bool)checkCommitsMethod.Invoke(service, null);

            // Should have enough commits now
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCheckExpectedView()
        {
            var context = MockConsensusComponents.CreateConsensusContext();
            var service = MockConsensusComponents.CreateConsensusService(context);

            // Access private method via reflection
            var checkExpectedViewMethod = typeof(ConsensusService).GetMethod(
                "CheckExpectedView",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(byte) },
                null);

            // Reset context for testing
            context.Reset(0);
            context.Block = MockConsensusComponents.CreateTestBlock();
            context.ViewNumber = 0;

            // Initially no ChangeView messages
            var result = (bool)checkExpectedViewMethod.Invoke(service, new object[] { 1 });
            Assert.IsFalse(result);

            // Create ChangeView messages from all validators except primary
            for (int i = 1; i < context.Validators.Length; i++)
            {
                var changeView = new ChangeView
                {
                    BlockIndex = context.Block.Index,
                    ValidatorIndex = (byte)i,
                    ViewNumber = 0,
                    Reason = ChangeViewReason.Timeout
                };

                var changeViewPayload = CreateConsensusPayload(changeView, context);
                context.ChangeViewPayloads[i] = changeViewPayload;
            }

            // Check again with ChangeView messages
            result = (bool)checkExpectedViewMethod.Invoke(service, new object[] { 1 });

            // Should have enough for view change now
            Assert.IsTrue(result);
        }
    }
}
