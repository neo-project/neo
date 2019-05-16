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
        public void Transactions_Set()
        {
            Transaction[] val = new Transaction[10];
            uut.Transactions = val;
            uut.Transactions.Length.Should().Be(10);
        }



        [TestMethod]
        public void Header_Get()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 0);

            uut.Header.Should().NotBeNull();
            uut.Header.PrevHash.Should().Be(val256);
            uut.Header.MerkleRoot.Should().Be(merkRootVal);
            uut.Header.Timestamp.Should().Be(timestampVal);
            uut.Header.Index.Should().Be(indexVal);
            uut.Header.Witness.Should().Be(scriptVal);
        }

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 0);
            // blockbase 4 + 32 + 32 + 4 + 4 + 20 + 1 + 3
            // block 9 + 1
            uut.Size.Should().Be(110);
        }

        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[1] {
                TestUtils.GetTransaction()
            };

            uut.Size.Should().Be(151);
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 0);

            uut.Transactions = new Transaction[3] {
                TestUtils.GetTransaction(),
                TestUtils.GetTransaction(),
                TestUtils.GetTransaction()
            };

            uut.Size.Should().Be(233);
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRootVal;
            UInt160 val160;
            uint timestampVal, indexVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRootVal, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 1);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 49, 73, 102, 67, 23, 43, 100, 236, 22, 37, 65, 124, 112, 39, 36, 66, 127, 219, 57, 69, 11, 184, 182, 127, 132, 95, 64, 200, 252, 206, 222, 197, 128, 171, 4, 253, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

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
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(new Block(), val256, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 1);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 49, 73, 102, 67, 23, 43, 100, 236, 22, 37, 65, 124, 112, 39, 36, 66, 127, 219, 57, 69, 11, 184, 182, 127, 132, 95, 64, 200, 252, 206, 222, 197, 128, 171, 4, 253, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }

            assertStandardBlockTestVals(val256, merkRoot, val160, timestampVal, indexVal, scriptVal, transactionsVal);
        }

        private void assertStandardBlockTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, uint timestampVal, uint indexVal, Witness scriptVal, Transaction[] transactionsVal, bool testTransactions = true)
        {
            uut.PrevHash.Should().Be(val256);
            uut.MerkleRoot.Should().Be(merkRoot);
            uut.Timestamp.Should().Be(timestampVal);
            uut.Index.Should().Be(indexVal);
            uut.NextConsensus.Should().Be(val160);
            uut.Witness.InvocationScript.Length.Should().Be(0);
            uut.Witness.Size.Should().Be(scriptVal.Size);
            uut.Witness.VerificationScript[0].Should().Be(scriptVal.VerificationScript[0]);
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
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(newBlock, val256, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 0);

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
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(newBlock, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 1);

            uut.Equals(newBlock).Should().BeTrue();
        }

        [TestMethod]
        public void RebuildMerkleRoot_Updates()
        {
            UInt256 val256 = UInt256.Zero;
            UInt256 merkRoot;
            UInt160 val160;
            uint timestampVal, indexVal;
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 1);

            UInt256 merkleRoot = uut.MerkleRoot;

            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 3);
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
            Witness scriptVal;
            Transaction[] transactionsVal;
            TestUtils.SetupBlockWithValues(uut, val256, out merkRoot, out val160, out timestampVal, out indexVal, out scriptVal, out transactionsVal, 1);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0x5c3688d7d110bc4f829c65e327baad67ae2406080748e46f2d2bf1097247c328");
            jObj["size"].AsNumber().Should().Be(151);
            jObj["version"].AsNumber().Should().Be(0);
            jObj["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            jObj["merkleroot"].AsString().Should().Be("0xc5decefcc8405f847fb6b80b4539db7f422427707c412516ec642b1743664931");
            jObj["time"].AsNumber().Should().Be(4244941696);
            jObj["index"].AsNumber().Should().Be(0);
            jObj["nextconsensus"].AsString().Should().Be("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

            JObject scObj = jObj["script"];
            scObj["invocation"].AsString().Should().Be("");
            scObj["verification"].AsString().Should().Be("51");

            jObj["tx"].Should().NotBeNull();
            JArray txObj = (JArray)jObj["tx"];
            txObj[0]["txid"].AsString().Should().Be("0xc5decefcc8405f847fb6b80b4539db7f422427707c412516ec642b1743664931");
            txObj[0]["size"].AsNumber().Should().Be(41);
            txObj[0]["version"].AsNumber().Should().Be(0);
            ((JArray)txObj[0]["attributes"]).Count.Should().Be(0);
            txObj[0]["net_fee"].AsString().Should().Be("0");
            ((JArray)txObj[0]["witnesses"]).Count.Should().Be(0);
        }
    }
}
