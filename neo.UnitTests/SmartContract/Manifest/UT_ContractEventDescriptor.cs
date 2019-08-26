using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Manifest;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractEventDescriptor
    {
        [TestMethod]
        public void TestFromJson()
        {
            ContractEventDescriptor expected = new ContractEventDescriptor
            {
                Name = "AAA",
                Parameters = new ContractParameterDefinition[0]
            };
            ContractEventDescriptor actual = ContractEventDescriptor.FromJson(expected.ToJson());
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(0, actual.Parameters.Length);
        }
    }
}
