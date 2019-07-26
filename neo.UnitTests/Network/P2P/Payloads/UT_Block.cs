using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Text;

namespace Neo.UnitTests.Network.P2P.Payloads
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
            TestUtils.SetupBlockWithValues(uut, val256, out var merkRootVal, out var val160, out var timestampVal, out var indexVal, out var scriptVal, out var transactionsVal, 0);

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
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, 0);
            // blockbase 4 + 64 + 32 + 4 + 4 + 20 + 4
            // block 9 + 1
            uut.Size.Should().Be(114);
        }

        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction()
            };

            uut.Size.Should().Be(165);
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction(),
                TestUtils.GetTransaction(),
                TestUtils.GetTransaction()
            };

            uut.Size.Should().Be(267);
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, 1);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 41, 176, 215, 72, 169, 204, 248, 197, 175, 60, 222, 16, 219, 62, 54, 236, 154, 95, 114, 6, 67, 162, 188, 180, 173, 215, 107, 61, 175, 65, 216, 233, 19, 255, 133, 76, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0 };
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
            TestUtils.SetupBlockWithValues(new Block(), val256, out var merkRoot, out var val160, out var timestampVal, out var indexVal, out var scriptVal, out var transactionsVal, 1);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            byte[] data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 41, 176, 215, 72, 169, 204, 248, 197, 175, 60, 222, 16, 219, 62, 54, 236, 154, 95, 114, 6, 67, 162, 188, 180, 173, 215, 107, 61, 175, 65, 216, 233, 19, 255, 133, 76, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 81, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0 };

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

        private void assertStandardBlockTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, ulong timestampVal, uint indexVal, Witness scriptVal, Transaction[] transactionsVal, bool testTransactions = true)
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
            ulong timestampVal;
            uint indexVal;
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
            ulong timestampVal;
            uint indexVal;
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
            ulong timestampVal;
            uint indexVal;
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
            TestUtils.SetupBlockWithValues(uut, val256, out var merkRoot, out var val160, out var timestampVal, out var indexVal, out var scriptVal, out var transactionsVal, 1);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0x4e1d8392e7c44830e7e45c18e5e0e3ef3c36af883868846d3691a436a62494b2");
            jObj["size"].AsNumber().Should().Be(165);
            jObj["version"].AsNumber().Should().Be(0);
            jObj["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            jObj["merkleroot"].AsString().Should().Be("0xd841af3d6bd7adb4bca24306725f9aec363edb10de3cafc5f8cca948d7b0290f");
            jObj["time"].AsNumber().Should().Be(328665601001);
            jObj["index"].AsNumber().Should().Be(0);
            jObj["nextconsensus"].AsString().Should().Be("AFmseVrdL9f9oyCzZefL9tG6UbvhPbdYzM");

            JObject scObj = ((JArray)jObj["witnesses"])[0];
            scObj["invocation"].AsString().Should().Be("");
            scObj["verification"].AsString().Should().Be("51");

            jObj["tx"].Should().NotBeNull();
            JArray txObj = (JArray)jObj["tx"];
            txObj[0]["hash"].AsString().Should().Be("0x64ed4e0d79407c60bde534feb44fbbd19bd065282d27ecd3a1a7a647f66affa6");
            txObj[0]["size"].AsNumber().Should().Be(51);
            txObj[0]["version"].AsNumber().Should().Be(0);
            ((JArray)txObj[0]["attributes"]).Count.Should().Be(0);
            txObj[0]["net_fee"].AsString().Should().Be("0");
        }
    }
}
