using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.IO;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Transaction
    {
        Transaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Transaction();
        }

        [TestMethod]
        public void Script_Get()
        {
            uut.Script.Should().BeNull();
        }

        [TestMethod]
        public void Script_Set()
        {
            byte[] val = TestUtils.GetByteArray(32, 0x42);
            uut.Script = val;
            uut.Script.Length.Should().Be(32);
            for (int i = 0; i < val.Length; i++)
            {
                uut.Script[i].Should().Be(val[i]);
            }
        }

        [TestMethod]
        public void Gas_Get()
        {
            uut.Gas.Should().Be(0);
        }

        [TestMethod]
        public void Gas_Set()
        {
            long val = 4200000000;
            uut.Gas = val;
            uut.Gas.Should().Be(val);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Attributes = new TransactionAttribute[0];
            uut.Witnesses = new Witness[0];

            byte[] val = TestUtils.GetByteArray(32, 0x42);
            uut.Script = val;

            uut.Version.Should().Be(0);
            uut.Script.Length.Should().Be(32);
            uut.Script.GetVarSize().Should().Be(33);
            uut.Size.Should().Be(52);
        }

        [TestMethod]
        public void ToJson()
        {
            byte[] scriptVal = TestUtils.GetByteArray(32, 0x42);
            uut.Script = scriptVal;
            long gasVal = 4200000000;
            uut.Gas = gasVal;

            uut.Attributes = new TransactionAttribute[0];
            uut.Witnesses = new Witness[0];

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["txid"].AsString().Should().Be("0x13968617bebc4f17c9adfd8c30f5c18d73edce9beb332937ead4b1cf6cca6851");
            jObj["size"].AsNumber().Should().Be(52);
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            jObj["net_fee"].AsString().Should().Be("0");
            ((JArray)jObj["witnesses"]).Count.Should().Be(0);

            jObj["script"].AsString().Should().Be("4220202020202020202020202020202020202020202020202020202020202020");
            jObj["gas"].AsNumber().Should().Be(42);
        }
    }
}
