using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
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
            // blockbase 4 + 32 + 32 + 4 + 4 + 20 + 4
            // block 9 + 1
            uut.Size.Should().Be(110);
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

            uut.Size.Should().Be(161);
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

            uut.Size.Should().Be(263);
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, 1);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                uut.Serialize(writer);
                data = stream.ToArray();
            }

            Assert.AreEqual(data.ToHexString(), "0000000000000000000000000000000000000000000000000000000000000000000000000f29b0d748a9ccf8c5af3cde10db3e36ec9a5f720643a2bcb4add76b3daf41d880ab04fd0000000000000000000000000000000000000000000000000100015101000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100010000");
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(new Block(), val256, out var merkRoot, out var val160, out var timestampVal, out var indexVal, out var scriptVal, out var transactionsVal, 1);

            uut.MerkleRoot = merkRoot; // need to set for deserialise to be valid

            using (MemoryStream ms = new MemoryStream("0000000000000000000000000000000000000000000000000000000000000000000000000f29b0d748a9ccf8c5af3cde10db3e36ec9a5f720643a2bcb4add76b3daf41d880ab04fd0000000000000000000000000000000000000000000000000100015101000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100010000".HexToBytes(), false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                uut.Deserialize(reader);
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
            TestUtils.SetupBlockWithValues(uut, val256, out var merkRoot, out var val160, out var timestampVal, out var indexVal, out var scriptVal, out var transactionsVal, 1);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0x1d8642796276c8ce3c5c03b8984a1b593d99b49a63d830bb06f800b8c953be77");
            jObj["size"].AsNumber().Should().Be(161);
            jObj["version"].AsNumber().Should().Be(0);
            jObj["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            jObj["merkleroot"].AsString().Should().Be("0xd841af3d6bd7adb4bca24306725f9aec363edb10de3cafc5f8cca948d7b0290f");
            jObj["time"].AsNumber().Should().Be(4244941696);
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
