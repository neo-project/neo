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

using Neo.Build.Core.Builders;
using System;

namespace Neo.Build.Core.Tests.Builders
{
    [TestClass]
    public class UT_BlockBuilder
    {
        [TestMethod]
        public void CheckCreateDefaults()
        {
            var builder = BlockBuilder.Create();

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(0u, block.Index);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }

        [TestMethod]
        public void CheckAddIndex()
        {
            var builder = BlockBuilder.Create()
                .AddIndex(9999u);

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(9999u, block.Index);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }
    }
}
