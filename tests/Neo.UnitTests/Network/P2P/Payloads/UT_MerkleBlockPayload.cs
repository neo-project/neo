using System;
using System.Collections;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_MerkleBlockPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = MerkleBlockPayload.Create(TestBlockchain.TheNeoSystem.GenesisBlock, new BitArray(1024, false));
            test.Size.Should().Be(247); // 239 + nonce 

            test = MerkleBlockPayload.Create(TestBlockchain.TheNeoSystem.GenesisBlock, new BitArray(0, false));
            test.Size.Should().Be(119); // 111 + nonce
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = MerkleBlockPayload.Create(TestBlockchain.TheNeoSystem.GenesisBlock, new BitArray(2, false));
            var clone = test.ToArray().AsSerializable<MerkleBlockPayload>();

            Assert.AreEqual(test.TxCount, clone.TxCount);
            Assert.AreEqual(test.Hashes.Length, clone.TxCount);
            CollectionAssert.AreEqual(test.Hashes, clone.Hashes);
            Assert.IsTrue(test.Flags.Span.SequenceEqual(clone.Flags.Span));
        }
    }
}
