using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Oracle;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleFilter
    {

        [TestMethod]
        public void TestSerialization()
        {
            OracleFilter filter = new OracleFilter()
            {
                ContractHash = UInt160.Parse("0x7ab841144dcdbf228ff57f7068f795e2afd1a3c1"),
                FilterMethod = "MyMethod",
                FilterArgs = "MyArgs"
            };

            var data = filter.ToArray();
            var copy = data.AsSerializable<OracleFilter>();

            Assert.AreEqual(filter.Size, data.Length);

            Assert.AreEqual(filter.ContractHash, copy.ContractHash);
            Assert.AreEqual(filter.FilterMethod, copy.FilterMethod);
            Assert.AreEqual(filter.FilterArgs, copy.FilterArgs);
        }
    }
}
