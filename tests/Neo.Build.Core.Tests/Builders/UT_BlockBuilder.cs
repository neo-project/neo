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
            Assert.AreNotEqual(0u, block.Nonce);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
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
            Assert.AreNotEqual(0u, block.Nonce);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }

        [TestMethod]
        public void CheckNextConsensus()
        {
            var builder = BlockBuilder.Create()
                .AddNextConsensus(UInt160.Parse("0xce45fca32b8cd071bfbc20389c20cd7025f85ff0"));

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(0u, block.Index);
            Assert.AreNotEqual(0u, block.Nonce);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Parse("0xce45fca32b8cd071bfbc20389c20cd7025f85ff0"), block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }

        [TestMethod]
        public void CheckPrevHash()
        {
            var builder = BlockBuilder.Create()
                .AddPrevHash(UInt256.Parse("0x254f4102a0aa0a86c07ddea9327efb4b9d8d1608bd0dcc34c211b47b44d25665"));

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(0u, block.Index);
            Assert.AreNotEqual(0u, block.Nonce);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Parse("0x254f4102a0aa0a86c07ddea9327efb4b9d8d1608bd0dcc34c211b47b44d25665"), block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }

        [TestMethod]
        public void CheckPrimaryIndex()
        {
            var builder = BlockBuilder.Create()
                .AddPrimaryIndex(255);

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(0u, block.Index);
            Assert.AreNotEqual(0u, block.Nonce);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(255, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }

        [TestMethod]
        public void CheckNonce()
        {
            var builder = BlockBuilder.Create()
                .AddNonce(666ul);

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(666ul, block.Nonce);
            Assert.AreEqual(0u, block.Index);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0uL, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }

        [TestMethod]
        public void CheckTimestamp()
        {
            var builder = BlockBuilder.Create()
                .AddTimestamp(6767675ul);

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(0u, block.Index);
            Assert.AreNotEqual(0u, block.Nonce);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreEqual(6767675ul, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
            Assert.AreEqual(0u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }

        [TestMethod]
        public void CheckVersion()
        {
            var builder = BlockBuilder.Create()
                .AddVersion(1u);

            Assert.IsNotNull(builder);
            Assert.IsInstanceOfType<BlockBuilder>(builder);

            var block = builder.Build();

            Assert.IsNotNull(block);
            Assert.AreEqual(0u, block.Index);
            Assert.AreNotEqual(0u, block.Nonce);
            Assert.AreNotEqual(UInt256.Zero, block.Hash);
            Assert.AreEqual(UInt256.Zero, block.MerkleRoot);
            Assert.AreEqual(UInt160.Zero, block.NextConsensus);
            Assert.AreNotEqual(0uL, block.Nonce);
            Assert.AreEqual(UInt256.Zero, block.PrevHash);
            Assert.AreEqual(0, block.PrimaryIndex);
            Assert.AreNotEqual(0ul, block.Timestamp);
            Assert.IsTrue((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > block.Timestamp);
            Assert.AreEqual(1u, block.Version);
            Assert.IsNotNull(block.Witness);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.InvocationScript);
            Assert.AreEqual(ReadOnlyMemory<byte>.Empty, block.Witness.VerificationScript);
        }
    }
}
