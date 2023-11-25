using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;

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
            TestUtils.SetupBlockWithValues(uut, val256, out var merkRootVal, out _, out var timestampVal, out var nonceVal, out var indexVal, out var scriptVal, out _, 0);

            uut.Header.Should().NotBeNull();
            uut.Header.PrevHash.Should().Be(val256);
            uut.Header.MerkleRoot.Should().Be(merkRootVal);
            uut.Header.Timestamp.Should().Be(timestampVal);
            uut.Header.Index.Should().Be(indexVal);
            uut.Header.Nonce.Should().Be(nonceVal);
            uut.Header.Witness.Should().Be(scriptVal);
        }

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 0);
            // header 4 + 32 + 32 + 8 + 4 + 1 + 20 + 4
            // tx 1
            uut.Size.Should().Be(114); // 106 + nonce
        }

        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction(UInt160.Zero)
            };

            uut.Size.Should().Be(167); // 159 + nonce
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction(UInt160.Zero),
                TestUtils.GetTransaction(UInt160.Zero),
                TestUtils.GetTransaction(UInt160.Zero)
            };

            uut.Size.Should().Be(273); // 265 + nonce
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 1);

            var hex = "0000000000000000000000000000000000000000000000000000000000000000000000006c23be5d32679baa9c5c2aa0d329fd2a2441d7875d0f34d42f58f70428fbbbb9e913ff854c00000000000000000000000000000000000000000000000000000000000000000000000001000111010000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000001000112010000";
            uut.ToArray().ToHexString().Should().Be(hex);
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(new Block(), val256, out _, out var val160, out var timestampVal, out var indexVal, out var nonceVal, out var scriptVal, out var transactionsVal, 1);

            var hex = "0000000000000000000000000000000000000000000000000000000000000000000000006c23be5d32679baa9c5c2aa0d329fd2a2441d7875d0f34d42f58f70428fbbbb9e913ff854c00000000000000000000000000000000000000000000000000000000000000000000000001000111010000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000001000112010000";

            MemoryReader reader = new(hex.HexToBytes());
            uut.Deserialize(ref reader);
            UInt256 merkRoot = uut.MerkleRoot;

            AssertStandardBlockTestVals(val256, merkRoot, val160, timestampVal, indexVal, nonceVal, scriptVal, transactionsVal);
        }

        private void AssertStandardBlockTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, ulong timestampVal, ulong nonceVal, uint indexVal, Witness scriptVal, Transaction[] transactionsVal, bool testTransactions = true)
        {
            uut.PrevHash.Should().Be(val256);
            uut.MerkleRoot.Should().Be(merkRoot);
            uut.Timestamp.Should().Be(timestampVal);
            uut.Index.Should().Be(indexVal);
            uut.Nonce.Should().Be(nonceVal);
            uut.NextConsensus.Should().Be(val160);
            uut.Witness.InvocationScript.Length.Should().Be(0);
            uut.Witness.Size.Should().Be(scriptVal.Size);
            uut.Witness.VerificationScript.Span[0].Should().Be(scriptVal.VerificationScript.Span[0]);
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
            Block newBlock = new();
            UInt256 val256 = UInt256.Zero;
            UInt256 prevHash = new(TestUtils.GetByteArray(32, 0x42));
            TestUtils.SetupBlockWithValues(newBlock, val256, out _, out _, out _, out ulong _, out uint _, out _, out _, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out _, out _, out _, out _, out _, out _, out _, 0);

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
            Block newBlock = new();
            UInt256 prevHash = new(TestUtils.GetByteArray(32, 0x42));
            TestUtils.SetupBlockWithValues(newBlock, prevHash, out _, out _, out _, out _, out _, out _, out _, 1);
            TestUtils.SetupBlockWithValues(uut, prevHash, out _, out _, out _, out _, out _, out _, out _, 1);

            uut.Equals(newBlock).Should().BeTrue();
        }

        [TestMethod]
        public void ToJson()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(uut, val256, out _, out _, out var timeVal, out var indexVal, out var nonceVal, out _, out _, 1);

            JObject jObj = uut.ToJson(TestProtocolSettings.Default);
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0x60193a05005c433787d8a9b95da332bbeebb311e904525e9fb1bacc34ff1ead7");
            jObj["size"].AsNumber().Should().Be(167); // 159 + nonce
            jObj["version"].AsNumber().Should().Be(0);
            jObj["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            jObj["merkleroot"].AsString().Should().Be("0xb9bbfb2804f7582fd4340f5d87d741242afd29d3a02a5c9caa9b67325dbe236c");
            jObj["time"].AsNumber().Should().Be(timeVal);
            jObj["nonce"].AsString().Should().Be(nonceVal.ToString("X16"));
            jObj["index"].AsNumber().Should().Be(indexVal);
            jObj["nextconsensus"].AsString().Should().Be("NKuyBkoGdZZSLyPbJEetheRhMjeznFZszf");

            JObject scObj = (JObject)jObj["witnesses"][0];
            scObj["invocation"].AsString().Should().Be("");
            scObj["verification"].AsString().Should().Be("EQ==");

            jObj["tx"].Should().NotBeNull();
            JObject txObj = (JObject)jObj["tx"][0];
            txObj["hash"].AsString().Should().Be("0xb9bbfb2804f7582fd4340f5d87d741242afd29d3a02a5c9caa9b67325dbe236c");
            txObj["size"].AsNumber().Should().Be(53);
            txObj["version"].AsNumber().Should().Be(0);
            ((JArray)txObj["attributes"]).Count.Should().Be(0);
            txObj["netfee"].AsString().Should().Be("0");
        }
    }
}
