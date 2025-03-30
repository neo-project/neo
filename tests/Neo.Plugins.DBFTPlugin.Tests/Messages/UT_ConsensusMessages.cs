// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ConsensusMessages.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Tests.Messages
{
    [TestClass]
    public class UT_ConsensusMessages
    {
        [TestMethod]
        public void TestPrepareRequestSerialization()
        {
            var message = new PrepareRequest
            {
                BlockIndex = 1,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Version = 0,
                PrevHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001"),
                Timestamp = 12345,
                Nonce = 67890,
                TransactionHashes = new UInt256[]
                {
                    UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000002"),
                    UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000003")
                }
            };

            // Serialize
            var data = message.ToArray();

            // Deserialize
            var deserialized = new PrepareRequest();
            MemoryReader reader = new(data);
            ((ISerializable)deserialized).Deserialize(ref reader);

            // Verify
            Assert.AreEqual(message.BlockIndex, deserialized.BlockIndex);
            Assert.AreEqual(message.ValidatorIndex, deserialized.ValidatorIndex);
            Assert.AreEqual(message.ViewNumber, deserialized.ViewNumber);
            Assert.AreEqual(message.Version, deserialized.Version);
            Assert.AreEqual(message.PrevHash, deserialized.PrevHash);
            Assert.AreEqual(message.Timestamp, deserialized.Timestamp);
            Assert.AreEqual(message.Nonce, deserialized.Nonce);

            // Convert ReadOnlyMemory to arrays for comparison
            Assert.AreEqual(message.TransactionHashes.Length, deserialized.TransactionHashes.Length);
            for (int i = 0; i < message.TransactionHashes.Length; i++)
            {
                Assert.AreEqual(message.TransactionHashes[i], deserialized.TransactionHashes[i]);
            }
        }

        [TestMethod]
        public void TestPrepareResponseSerialization()
        {
            var message = new PrepareResponse
            {
                BlockIndex = 1,
                ValidatorIndex = 0,
                ViewNumber = 0,
                PreparationHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001")
            };

            // Serialize
            var data = message.ToArray();

            // Deserialize
            var deserialized = new PrepareResponse();
            MemoryReader reader = new(data);
            ((ISerializable)deserialized).Deserialize(ref reader);

            // Verify
            Assert.AreEqual(message.BlockIndex, deserialized.BlockIndex);
            Assert.AreEqual(message.ValidatorIndex, deserialized.ValidatorIndex);
            Assert.AreEqual(message.ViewNumber, deserialized.ViewNumber);
            Assert.AreEqual(message.PreparationHash, deserialized.PreparationHash);
        }

        [TestMethod]
        public void TestCommitSerialization()
        {
            var message = new Commit
            {
                BlockIndex = 1,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Signature = new byte[64] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4 }
            };

            // Serialize
            var data = message.ToArray();

            // Deserialize
            var deserialized = new Commit();
            MemoryReader reader = new(data);
            ((ISerializable)deserialized).Deserialize(ref reader);

            // Verify
            Assert.AreEqual(message.BlockIndex, deserialized.BlockIndex);
            Assert.AreEqual(message.ValidatorIndex, deserialized.ValidatorIndex);
            Assert.AreEqual(message.ViewNumber, deserialized.ViewNumber);
            CollectionAssert.AreEqual(message.Signature.ToArray(), deserialized.Signature.ToArray());
        }

        [TestMethod]
        public void TestChangeViewSerialization()
        {
            var message = new ChangeView
            {
                BlockIndex = 1,
                ValidatorIndex = 0,
                ViewNumber = 0,
                Timestamp = 12345,
                Reason = Neo.Plugins.DBFTPlugin.Types.ChangeViewReason.Timeout
            };

            // Serialize
            var data = message.ToArray();

            // Deserialize
            var deserialized = new ChangeView();
            MemoryReader reader = new(data);
            ((ISerializable)deserialized).Deserialize(ref reader);

            // Verify
            Assert.AreEqual(message.BlockIndex, deserialized.BlockIndex);
            Assert.AreEqual(message.ValidatorIndex, deserialized.ValidatorIndex);
            Assert.AreEqual(message.ViewNumber, deserialized.ViewNumber);
            Assert.AreEqual(message.NewViewNumber, deserialized.NewViewNumber);
            Assert.AreEqual(message.Reason, deserialized.Reason);
        }

        [TestMethod]
        public void TestRecoveryMessageSerialization()
        {
            var message = new RecoveryMessage
            {
                BlockIndex = 1,
                ValidatorIndex = 0,
                ViewNumber = 0,
                PreparationHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001"),
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>
                {
                    [1] = new RecoveryMessage.ChangeViewPayloadCompact
                    {
                        ValidatorIndex = 1,
                        OriginalViewNumber = 0,
                        Timestamp = 12345,
                        InvocationScript = new byte[] { 1, 2, 3 }
                    }
                },
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>
                {
                    [1] = new RecoveryMessage.PreparationPayloadCompact
                    {
                        ValidatorIndex = 1,
                        InvocationScript = new byte[] { 4, 5, 6 }
                    }
                },
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>
                {
                    [1] = new RecoveryMessage.CommitPayloadCompact
                    {
                        ValidatorIndex = 1,
                        ViewNumber = 0,
                        Signature = new byte[] { 7, 8, 9 },
                        InvocationScript = new byte[] { 10, 11, 12 }
                    }
                }
            };

            // Serialize
            var data = message.ToArray();

            // Deserialize
            var deserialized = new RecoveryMessage();
            var reader = new MemoryReader(data);
            ((ISerializable)deserialized).Deserialize(ref reader);

            // Verify
            Assert.AreEqual(message.BlockIndex, deserialized.BlockIndex);
            Assert.AreEqual(message.ValidatorIndex, deserialized.ValidatorIndex);
            Assert.AreEqual(message.ViewNumber, deserialized.ViewNumber);
            Assert.AreEqual(message.PreparationHash, deserialized.PreparationHash);

            // Verify ChangeViewMessages
            Assert.AreEqual(message.ChangeViewMessages.Count, deserialized.ChangeViewMessages.Count);
            Assert.AreEqual(message.ChangeViewMessages[1].ValidatorIndex, deserialized.ChangeViewMessages[1].ValidatorIndex);
            Assert.AreEqual(message.ChangeViewMessages[1].OriginalViewNumber, deserialized.ChangeViewMessages[1].OriginalViewNumber);
            Assert.AreEqual(message.ChangeViewMessages[1].Timestamp, deserialized.ChangeViewMessages[1].Timestamp);
            CollectionAssert.AreEqual(message.ChangeViewMessages[1].InvocationScript.ToArray(), deserialized.ChangeViewMessages[1].InvocationScript.ToArray());

            // Verify PreparationMessages
            Assert.AreEqual(message.PreparationMessages.Count, deserialized.PreparationMessages.Count);
            Assert.AreEqual(message.PreparationMessages[1].ValidatorIndex, deserialized.PreparationMessages[1].ValidatorIndex);
            CollectionAssert.AreEqual(message.PreparationMessages[1].InvocationScript.ToArray(), deserialized.PreparationMessages[1].InvocationScript.ToArray());

            // Verify CommitMessages
            Assert.AreEqual(message.CommitMessages.Count, deserialized.CommitMessages.Count);
            Assert.AreEqual(message.CommitMessages[1].ValidatorIndex, deserialized.CommitMessages[1].ValidatorIndex);
            Assert.AreEqual(message.CommitMessages[1].ViewNumber, deserialized.CommitMessages[1].ViewNumber);
            CollectionAssert.AreEqual(message.CommitMessages[1].Signature.ToArray(), deserialized.CommitMessages[1].Signature.ToArray());
            CollectionAssert.AreEqual(message.CommitMessages[1].InvocationScript.ToArray(), deserialized.CommitMessages[1].InvocationScript.ToArray());
        }

        [TestMethod]
        public void TestRecoveryRequestSerialization()
        {
            var message = new RecoveryRequest
            {
                BlockIndex = 1,
                ValidatorIndex = 0,
                Timestamp = 12345
            };

            // Serialize
            var data = message.ToArray();

            // Deserialize
            var deserialized = new RecoveryRequest();
            MemoryReader reader = new(data);
            ((ISerializable)deserialized).Deserialize(ref reader);

            // Verify
            Assert.AreEqual(message.BlockIndex, deserialized.BlockIndex);
            Assert.AreEqual(message.ValidatorIndex, deserialized.ValidatorIndex);
            Assert.AreEqual(message.Timestamp, deserialized.Timestamp);
        }
    }
}
