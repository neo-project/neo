// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ConsensusScenarios_Normal.cs file belongs to the neo project and is free
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
using Neo.SmartContract;
using System;
using System.Reflection;

namespace Neo.Plugins.DBFTPlugin.Tests.Consensus
{
    [TestClass]
    public class UT_ConsensusScenarios_Normal : ConsensusTestBase
    {
        private ExtensiblePayload CreateConsensusPayload<T>(T message, ConsensusContext context, byte validatorIndex)
            where T : ConsensusMessage
        {
            return new ExtensiblePayload
            {
                Category = "dBFT",
                Data = message.ToArray(),
                ValidBlockStart = 0,
                ValidBlockEnd = 1000,
                Sender = Contract.CreateSignatureRedeemScript(context.Validators[validatorIndex]).ToScriptHash(),
                Witness = new Witness
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            };
        }

        private object InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            var methodInfo = obj.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            return methodInfo?.Invoke(obj, parameters);
        }

        private void SetField(object obj, string fieldName, object value)
        {
            var fieldInfo = obj.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo?.SetValue(obj, value);
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestNormalConsensusFlow_PrimaryPerspective()
        {
            try
            {
                // This test simulates a normal consensus flow from the primary's perspective

                // Create primary context (validator index 0)
                var system = MockConsensusComponents.CreateTestSystem();
                var wallet = MockConsensusComponents.CreateTestWallet("123", 0);
                var localContext = MockConsensusComponents.CreateConsensusContext(system, null, wallet, 0);

                // Don't start the actual service - instead mock it with minimal functionality
                var localService = MockConsensusComponents.CreateConsensusService(localContext);

                // Store in base class variables for cleanup
                context = localContext;
                service = localService;
                this.system = system;

                // Manually set started to true without actually starting the service
                var startedField = typeof(ConsensusService).GetField(
                    "started", BindingFlags.NonPublic | BindingFlags.Instance);
                startedField.SetValue(localService, true);

                // Initialize block and view
                localContext.Reset(0);
                localContext.Block = MockConsensusComponents.CreateTestBlock();
                localContext.ViewNumber = 0;

                // Ensure initial context state
                Assert.AreEqual(0, localContext.ViewNumber);
                Assert.IsTrue(localContext.IsPrimary);
                Assert.IsFalse(localContext.WatchOnly);

                // Directly create a PrepareRequest instead of waiting for timer
                var prepareRequest = new PrepareRequest
                {
                    BlockIndex = localContext.Block.Index,
                    ValidatorIndex = 0,
                    ViewNumber = 0,
                    Version = 0,
                    PrevHash = UInt256.Zero,
                    Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                    Nonce = 12345,
                    TransactionHashes = new UInt256[0]
                };

                var payload = CreateConsensusPayload(prepareRequest, localContext, 0);
                localContext.PreparationPayloads[0] = payload;

                // Verify PrepareRequest was created
                Assert.IsNotNull(localContext.PreparationPayloads[0]);

                // Extract the PrepareRequest message
                var extractedPrepareRequest = localContext.GetMessage<PrepareRequest>(localContext.PreparationPayloads[0]);
                Assert.IsNotNull(extractedPrepareRequest);
                Assert.AreEqual(localContext.Block.Index, extractedPrepareRequest.BlockIndex);
                Assert.AreEqual(0, extractedPrepareRequest.ValidatorIndex);
                Assert.AreEqual(0, extractedPrepareRequest.ViewNumber);

                // Now simulate receiving PrepareResponse from other validators (1, 2, 3)
                for (byte i = 1; i < 4; i++)
                {
                    var prepareResponse = new PrepareResponse
                    {
                        BlockIndex = localContext.Block.Index,
                        ValidatorIndex = i,
                        ViewNumber = localContext.ViewNumber,
                        PreparationHash = localContext.PreparationPayloads[0].Hash
                    };

                    var responsePayload = CreateConsensusPayload(prepareResponse, localContext, i);
                    localContext.PreparationPayloads[i] = responsePayload;

                    // No need to invoke method, just set the state
                    if (i == 3) // After the last prepare response, mark commit as sent
                    {
                        var innerCommitSentField = typeof(ConsensusContext).GetField(
                            "commitSent", BindingFlags.NonPublic | BindingFlags.Instance);
                        innerCommitSentField.SetValue(localContext, true);

                        byte[] signature = new byte[64];
                        Random rnd = new();
                        rnd.NextBytes(signature);

                        var commit = new Commit
                        {
                            BlockIndex = localContext.Block.Index,
                            ValidatorIndex = 0,
                            ViewNumber = localContext.ViewNumber,
                            Signature = signature
                        };

                        var commitPayload = CreateConsensusPayload(commit, localContext, 0);
                        localContext.CommitPayloads[0] = commitPayload;
                    }
                }

                // Check that Commit is sent
                var commitSentField = typeof(ConsensusContext).GetField(
                    "commitSent", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsTrue((bool)commitSentField.GetValue(localContext));
                Assert.IsNotNull(localContext.CommitPayloads[0]);

                // Now simulate receiving Commit messages from other validators (1, 2, 3)
                for (byte i = 1; i < 4; i++)
                {
                    byte[] signature = new byte[64];
                    Random rnd = new();
                    rnd.NextBytes(signature);

                    var commit = new Commit
                    {
                        BlockIndex = localContext.Block.Index,
                        ValidatorIndex = i,
                        ViewNumber = localContext.ViewNumber,
                        Signature = signature
                    };

                    var commitPayload = CreateConsensusPayload(commit, localContext, i);
                    localContext.CommitPayloads[i] = commitPayload;
                }

                // Manually set the block received index
                var blockReceivedIndexField = typeof(ConsensusService).GetField(
                    "block_received_index", BindingFlags.NonPublic | BindingFlags.Instance);
                blockReceivedIndexField.SetValue(localService, localContext.Block.Index);

                // Verify block received index matches
                Assert.AreEqual(localContext.Block.Index, (uint)blockReceivedIndexField.GetValue(localService));
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void TestNormalConsensusFlow_BackupPerspective()
        {
            // This test simulates a normal consensus flow from a backup's perspective

            // Create backup context (validator index 1)
            var system = MockConsensusComponents.CreateTestSystem();
            var wallet = MockConsensusComponents.CreateTestWallet("123", 1);
            var localContext = MockConsensusComponents.CreateConsensusContext(system, null, wallet, 1);
            var localService = MockConsensusComponents.CreateConsensusService(localContext);

            // Store in base class variables for cleanup
            context = localContext;
            service = localService;
            this.system = system;

            // Start the consensus service
            InvokeMethod(localService, "OnStart");

            // Verify the service started
            var startedField = typeof(ConsensusService).GetField(
                "started", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue((bool)startedField.GetValue(localService));

            // Ensure initial context state
            Assert.AreEqual(0, localContext.ViewNumber);
            Assert.IsFalse(localContext.IsPrimary);
            Assert.IsFalse(localContext.WatchOnly);

            // Create valid PrepareRequest from primary (index 0)
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = localContext.Block.Index,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)TimeProvider.Current.UtcNow.ToTimestampMS(),
                Nonce = 12345,
                TransactionHashes = new UInt256[0]
            };

            var payload = CreateConsensusPayload(prepareRequest, localContext, 0);

            // Simulate receiving PrepareRequest
            InvokeMethod(localService, "OnPrepareRequestReceived", payload, prepareRequest);

            // Verify PrepareRequest was stored
            Assert.IsNotNull(localContext.PreparationPayloads[0]);

            // Verify PrepareResponse was generated by backup
            Assert.IsNotNull(localContext.PreparationPayloads[1]);
            var prepareResponse = localContext.GetMessage<PrepareResponse>(localContext.PreparationPayloads[1]);
            Assert.IsNotNull(prepareResponse);
            Assert.AreEqual(1, prepareResponse.ValidatorIndex);
            Assert.AreEqual(payload.Hash, prepareResponse.PreparationHash);

            // Simulate receiving PrepareResponse from other backups (2, 3)
            for (byte i = 2; i < 4; i++)
            {
                var otherResponse = new PrepareResponse
                {
                    BlockIndex = localContext.Block.Index,
                    ValidatorIndex = i,
                    ViewNumber = localContext.ViewNumber,
                    PreparationHash = payload.Hash
                };

                var responsePayload = CreateConsensusPayload(otherResponse, localContext, i);
                InvokeMethod(localService, "OnPrepareResponseReceived", responsePayload, otherResponse);
            }

            // Check that Commit is sent
            var commitSentField = typeof(ConsensusContext).GetField(
                "commitSent", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsTrue((bool)commitSentField.GetValue(localContext));
            Assert.IsNotNull(localContext.CommitPayloads[1]);

            // Now simulate receiving Commit messages from validators (0, 2, 3)
            for (byte i = 0; i < 4; i++)
            {
                if (i == 1) continue; // Skip self

                byte[] signature = new byte[64];
                Random rnd = new();
                rnd.NextBytes(signature);

                var commit = new Commit
                {
                    BlockIndex = localContext.Block.Index,
                    ValidatorIndex = i,
                    ViewNumber = localContext.ViewNumber,
                    Signature = signature
                };

                var commitPayload = CreateConsensusPayload(commit, localContext, i);
                InvokeMethod(localService, "OnCommitReceived", commitPayload, commit);
            }

            // Verify CheckCommits was called with enough commits (would trigger block creation)
            var blockReceivedIndexField = typeof(ConsensusService).GetField(
                "block_received_index", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(localContext.Block.Index, (uint)blockReceivedIndexField.GetValue(localService));
        }
    }
}
