// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MerkleBlockPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_MerkleBlockPayload
    {
        private NeoSystem _system;

        [TestInitialize]
        public void TestSetup()
        {
            _system = TestBlockchain.GetSystem();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _system.Dispose();
        }

        [TestMethod]
        public void Size_Get()
        {
            var test = MerkleBlockPayload.Create(_system.GenesisBlock, new BitArray(1024, false));
            Assert.AreEqual(244, test.Size);
            Assert.AreEqual(test.ToArray().Length, test.Size);

            test = MerkleBlockPayload.Create(_system.GenesisBlock, new BitArray(0, false));
            Assert.AreEqual(116, test.Size);
            Assert.AreEqual(test.ToArray().Length, test.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = MerkleBlockPayload.Create(_system.GenesisBlock, new BitArray(2, false));
            var clone = test.ToArray().AsSerializable<MerkleBlockPayload>();

            Assert.AreEqual(test.TxCount, clone.TxCount);
            Assert.AreEqual(test.Hashes.Length, clone.Hashes.Length);
            Assert.AreEqual(test.Flags.Length, clone.Flags.Length);
            Assert.AreSequenceEqual(test.Hashes, clone.Hashes);
            Assert.IsTrue(test.Flags.Span.SequenceEqual(clone.Flags.Span));
        }

        [TestMethod]
        [DataRow(3)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(8)]
        [DataRow(9)]
        [DataRow(10)]
        [DataRow(11)]
        [DataRow(12)]
        [DataRow(17)]
        public void FilteredOddLayerBlocksRoundTrip(int count)
        {
            var transactions = Enumerable.Range(1, count).Select(i => new Transaction
            {
                Nonce = (uint)i,
                Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.None }],
                Attributes = [],
                Script = new byte[] { (byte)VM.OpCode.PUSH1 },
                Witnesses = [Witness.Empty]
            }).ToArray();
            var block = new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = MerkleTree.ComputeRoot(transactions.Select(tx => tx.Hash).ToArray()),
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = transactions
            };

            // Exhaust all filters for small trees; exercise each selected leaf, mixed
            // selections and the fully expanded tree for the larger odd layers.
            int filters = count <= 9 ? 1 << count : count + 3;
            for (int filter = filters - 1; filter >= 0; filter--)
            {
                var flags = new BitArray(count);
                for (int i = 0; i < count; i++)
                    flags[i] = count <= 9 ? (filter & (1 << i)) != 0
                        : filter == i || filter == count + 1 || filter == count + 2 && (i == 1 || i == count - 1);

                var payload = MerkleBlockPayload.Create(block, flags);
                byte[] bytes = payload.ToArray();
                var clone = bytes.AsSerializable<MerkleBlockPayload>();
                Assert.AreEqual(bytes.Length, payload.Size);
                Assert.AreEqual(count, clone.TxCount);
                Assert.AreEqual(block.Header.Hash, clone.Header.Hash);
                Assert.AreSequenceEqual(payload.Hashes, clone.Hashes);
                Assert.IsTrue(payload.Flags.Span.SequenceEqual(clone.Flags.Span));
                Assert.IsTrue(payload.Hashes.Length <= BitOperations.RoundUpToPowerOf2((uint)count));
                for (int i = 0; i < count; i++)
                    if (flags[i]) Assert.Contains(transactions[i].Hash, clone.Hashes);

                if (filter == filters - 1)
                {
                    var message = Message.Create(MessageCommand.MerkleBlock, payload);
                    foreach (bool compress in new[] { false, true })
                    {
                        var wire = ByteString.CopyFrom(message.ToArray(compress));
                        Assert.AreEqual(wire.Count, Message.TryDeserialize(wire, out var decoded));
                        Assert.AreEqual(MessageCommand.MerkleBlock, decoded.Command);
                        Assert.AreSequenceEqual(payload.Hashes, ((MerkleBlockPayload)decoded.Payload).Hashes);
                    }
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(3)]
        [DataRow(5)]
        [DataRow(8)]
        [DataRow(252)]
        [DataRow(253)]
        [DataRow(65535)]
        public void HashCountIsBoundedByPaddedTreeCapacity(int count)
        {
            int capacity = (int)BitOperations.RoundUpToPowerOf2((uint)count);
            var payload = new MerkleBlockPayload
            {
                Header = _system.GenesisBlock.Header,
                TxCount = count,
                Hashes = Enumerable.Repeat(UInt256.Zero, capacity).ToArray(),
                Flags = new byte[(count + 7) / 8]
            };
            byte[] bytes = payload.ToArray();
            var clone = bytes.AsSerializable<MerkleBlockPayload>();
            Assert.AreEqual(bytes.Length, payload.Size);
            Assert.HasCount(capacity, clone.Hashes);

            payload.Hashes = Enumerable.Repeat(UInt256.Zero, capacity + 1).ToArray();
            Assert.ThrowsExactly<FormatException>(() => payload.ToArray().AsSerializable<MerkleBlockPayload>());
        }
    }
}
