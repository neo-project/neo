// Copyright (C) 2015-2025 The Neo Project.
//
// UT_BlockBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Builders;
using Neo.Extensions.Factories;

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_BlockBuilder
    {
        [TestMethod]
        public void TestCreateBuilder()
        {
            var builder = BlockBuilder.CreateEmpty();

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void TestEmptyBlock()
        {
            var block = BlockBuilder.CreateEmpty().Build();

            Assert.IsNotNull(block);
            Assert.IsNotNull(block.Hash);
        }

        [TestMethod]
        public void TestBlockVersion()
        {
            byte expectedVersion = 1;
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddVersion(expectedVersion)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedVersion, actualBlock.Version);
        }

        [TestMethod]
        public void TestBlockIndex()
        {
            byte expectedIndex = 1;
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddIndex(expectedIndex)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedIndex, actualBlock.Index);
        }

        [TestMethod]
        public void TestBlockPrimaryIndex()
        {
            byte expectedPrimaryIndex = 1;
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddPrimaryIndex(expectedPrimaryIndex)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedPrimaryIndex, actualBlock.PrimaryIndex);
        }

        [TestMethod]
        public void TestBlockNextConsensus()
        {
            var expectedNextConsensus = UInt160.Zero;
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddNextConsensus(expectedNextConsensus)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedNextConsensus, actualBlock.NextConsensus);
        }

        [TestMethod]
        public void TestBlockNonce()
        {
            var expectedNonce = RandomNumberFactory.NextUInt64();
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddNonce(expectedNonce)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedNonce, actualBlock.Nonce);
        }

        [TestMethod]
        public void TestBlockPrevHash()
        {
            var expectedPrevHash = UInt256.Zero;
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddPrevHash(expectedPrevHash)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedPrevHash, actualBlock.PrevHash);
        }

        [TestMethod]
        public void TestBlockTimestamp()
        {
            var expectedTimestamp = RandomNumberFactory.NextUInt64();
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddTimestamp(expectedTimestamp)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedTimestamp, actualBlock.Timestamp);
        }

        [TestMethod]
        public void TestBlockTransaction()
        {
            var expectedTransaction = TransactionBuilder.CreateEmpty().Build();
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddTransaction(expectedTransaction)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedTransaction, actualBlock.Transactions[0]);
        }

        [TestMethod]
        public void TestBlockWitness()
        {
            var expectedWitness = WitnessBuilder.CreateEmpty().Build();
            var actualBlock = BlockBuilder.CreateEmpty()
                .AddWitness(expectedWitness)
                .Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedWitness, actualBlock.Witness);
        }

        [TestMethod]
        public void TestBlockMerkleRoot()
        {
            var expectedMerkleRoot = UInt256.Zero;
            var actualBlock = BlockBuilder.CreateEmpty().Build();

            Assert.IsNotNull(actualBlock);
            Assert.AreEqual(expectedMerkleRoot, actualBlock.MerkleRoot);
        }
    }
}
