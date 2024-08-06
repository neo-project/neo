// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeSerializablePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.Models.Payloads;
using System;

namespace Neo.Plugins.NamedPipeService.Tests.Payloads
{
    [TestClass]
    public class UT_PipeSerializablePayload
    {
        [TestMethod]
        public void IPipeMessage_ToArray_Null()
        {
            var block1 = new PipeSerializablePayload<Block>() { Value = null };
            var expectedBytes = block1.ToArray();

            var block2 = new PipeSerializablePayload<Block>() { Value = null };
            var actualBytes = block2.ToArray();
            var actualBytesWithoutHeader = actualBytes;

            CollectionAssert.AreEqual(expectedBytes, actualBytesWithoutHeader);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Null()
        {
            var block1 = new PipeSerializablePayload<Block>() { Value = null };
            var expectedBytes = block1.ToArray();

            var block2 = new PipeSerializablePayload<Block>();
            block2.FromArray(expectedBytes);

            var actualBytes = block2.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.IsNull(block2.Value);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Data()
        {
            var block1 = CreateEmptyPipeBlock();
            var expectedBytes = block1.ToArray();

            var block2 = new PipeSerializablePayload<Block>();
            block2.FromArray(expectedBytes);

            var actualBytes = block2.ToArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(block1.Size, block2.Size);
            Assert.AreEqual(block1.Value.Hash, block2.Value.Hash);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_Data()
        {
            var block1 = CreateEmptyPipeBlock();
            var expectedBytes = block1.ToArray();

            var block2 = CreateEmptyPipeBlock();
            var actualBytes = block2.ToArray();
            var actualBytesWithoutHeader = actualBytes;

            CollectionAssert.AreEqual(expectedBytes, actualBytesWithoutHeader);
        }

        private static PipeSerializablePayload<Block> CreateEmptyPipeBlock() =>
            new()
            {
                Value = new Block()
                {
                    Header = new Header()
                    {
                        Version = 0,
                        PrevHash = UInt256.Zero,
                        MerkleRoot = UInt256.Zero,
                        Timestamp = 0,
                        Index = 0,
                        Nonce = 0,
                        PrimaryIndex = 0,
                        NextConsensus = UInt160.Zero,
                        Witness = new Witness()
                        {
                            InvocationScript = Memory<byte>.Empty,
                            VerificationScript = Memory<byte>.Empty,
                        },
                    },
                    Transactions = [],
                }
            };

    }
}
