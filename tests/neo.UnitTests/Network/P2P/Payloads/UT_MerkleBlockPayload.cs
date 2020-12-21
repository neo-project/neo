using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System.Collections;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_MerkleBlockPayload
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Size_Get()
        {
            var test = MerkleBlockPayload.Create(Blockchain.GenesisBlock, new BitArray(1024, false));
            test.Size.Should().Be(270);

            test = MerkleBlockPayload.Create(Blockchain.GenesisBlock, new BitArray(0, false));
            test.Size.Should().Be(142);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = MerkleBlockPayload.Create(Blockchain.GenesisBlock, new BitArray(2, false));
            var clone = test.ToArray().AsSerializable<MerkleBlockPayload>();

            Assert.AreEqual(test.ContentCount, clone.ContentCount);
            Assert.AreEqual(test.Hashes.Length, clone.ContentCount);
            CollectionAssert.AreEqual(test.Hashes, clone.Hashes);
            CollectionAssert.AreEqual(test.Flags, clone.Flags);
        }
    }
}
