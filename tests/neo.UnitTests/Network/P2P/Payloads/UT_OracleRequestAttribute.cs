using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_OracleRequestAttribute
    {
        [TestMethod]
        public void TestSerialization()
        {
            OracleRequestAttribute attr = new OracleRequestAttribute();

            var data = attr.ToArray();
            var copy = data.AsSerializable<OracleRequestAttribute>();

            Assert.AreEqual(attr.Size, data.Length);

            Assert.AreEqual(attr.Type, copy.Type);
            Assert.AreEqual(attr.Size, copy.Size);
        }
    }
}
