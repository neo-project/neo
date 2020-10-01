using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract.Manifest;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractAbi
    {
        [TestMethod]
        public void ParseFromJson_Default()
        {
            var json = @"{""methods"":[],""events"":[]}";
            var abi = ContractAbi.FromJson(JObject.Parse(json));

            Assert.AreEqual(abi.ToString(), json);
            Assert.AreEqual(abi.ToString(), new ContractAbi() { Events = new ContractEventDescriptor[0], Methods = new ContractMethodDescriptor[0] }.ToString());
        }

        [TestMethod]
        public void TestDeserializeAndSerialize()
        {
            var expected = TestUtils.CreateDefaultAbi("main", Neo.SmartContract.ContractParameterType.Any);
            var actual = expected.ToArray().AsSerializable<ContractAbi>();
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        [TestMethod]
        public void TestGetSize()
        {
            var temp = TestUtils.CreateDefaultManifest(UInt160.Zero);
            Assert.AreEqual(224, temp.Size);
        }

        [TestMethod]
        public void TestClone()
        {
            var expected = TestUtils.CreateDefaultAbi("main", Neo.SmartContract.ContractParameterType.Any);
            var actual = expected.Clone();
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }
    }
}
