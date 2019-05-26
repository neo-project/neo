using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Block
    {
        Block uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Block();
        }

        [TestMethod]
        public void Transactions_Get()
        {
            uut.Transactions.Should().BeNull();
        }

        [TestMethod]
        public void Header_Get()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 0);

            uut.Header.Should().NotBeNull();
            uut.Header.PrevHash.Should().Be(val256);
            uut.Header.MerkleRoot.Should().Be(merkRootVal);
            uut.Header.Timestamp.Should().Be(timestampVal);
            uut.Header.Index.Should().Be(indexVal);
            uut.Header.Signatures.Should().BeEquivalentTo(signaturesVal);
        }

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 0);
            // blockbase 4 + 32 + 32 + 4 + 4 + 20 + 1
            // block 9 + 1
            uut.Size.Should().Be(107);
        }

        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction()
            };

            uut.Size.Should().Be(157);
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction(),
                TestUtils.GetTransaction(),
                TestUtils.GetTransaction()
            };

            uut.Size.Should().Be(257);
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 1);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 90, 92, 165, 113, 177, 185, 161, 160, 235, 170, 231, 248, 38, 157, 47, 48, 13, 204, 6, 138, 124, 43, 26, 49, 152, 41, 204, 110, 65, 163, 39, 79, 128, 171, 4, 253, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            data.Length.Should().Be(requiredData.Length);
            for (int i = 0; i < data.Length; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(new Block(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 1);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 90, 92, 165, 113, 177, 185, 161, 160, 235, 170, 231, 248, 38, 157, 47, 48, 13, 204, 6, 138, 124, 43, 26, 49, 152, 41, 204, 110, 65, 163, 39, 79, 128, 171, 4, 253, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }

            assertStandardBlockTestVals(val256, merkRoot, val160, timestampVal, indexVal, signaturesVal, transactionsVal);
        }

        private void assertStandardBlockTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, uint timestampVal, uint indexVal, byte[][] signaturesVal, Transaction[] transactionsVal, bool testTransactions = true)
        {
            uut.PrevHash.Should().Be(val256);
            uut.MerkleRoot.Should().Be(merkRoot);
            uut.Timestamp.Should().Be(timestampVal);
            uut.Index.Should().Be(indexVal);
            uut.NextConsensus.Should().Be(val160);
            uut.Signatures.Length.Should().Be(0);
            if (testTransactions)
            {
                uut.Transactions.Length.Should().Be(1);
                uut.Transactions[0].Should().Be(transactionsVal[0]);
            }
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            uut.Equals(uut).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_DiffObj()
        {
            Block newBlock = new Block();
            UInt256 val256 = UInt256.Zero;
            UInt256 prevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(newBlock, val256, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 0);

            uut.Equals(newBlock).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_Null()
        {
            uut.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_SameHash()
        {

            Block newBlock = new Block();
            UInt256 prevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(newBlock, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 1);

            uut.Equals(newBlock).Should().BeTrue();
        }

        [TestMethod]
        public void RebuildMerkleRoot_Updates()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 1);

            UInt256 merkleRoot = uut.MerkleRoot;

            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 3);
            uut.RebuildMerkleRoot();

            uut.MerkleRoot.Should().NotBe(merkleRoot);
        }

        [TestMethod]
        public void ToJson()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            byte[][] signaturesVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out signaturesVal, out transactionsVal, 1);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0x83d69e89ba8bb1f15849241eb2fccd4a455dca600cc5325dbc700f39f9c06edf");
            jObj["size"].AsNumber().Should().Be(157);
            jObj["version"].AsNumber().Should().Be(0);
            jObj["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            jObj["merkleroot"].AsString().Should().Be("0x4f27a3416ecc2998311a2b7c8a06cc0d302f9d26f8e7aaeba0a1b9b171a55c5a");
            jObj["time"].AsNumber().Should().Be(4244941696);
            jObj["index"].AsNumber().Should().Be(0);
            jObj["nextconsensus"].AsString().Should().Be("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

            jObj["tx"].Should().NotBeNull();
            JArray txObj = (JArray)jObj["tx"];
            txObj[0]["txid"].AsString().Should().Be("0x0a1260ca626599450b3fdcbc39bde10e6f35fb99cd4f688828d9e711c055cfb5");
            txObj[0]["size"].AsNumber().Should().Be(50);
            txObj[0]["version"].AsNumber().Should().Be(0);
            ((JArray)txObj[0]["attributes"]).Count.Should().Be(0);
            txObj[0]["net_fee"].AsString().Should().Be("0");
        }
    }
}
