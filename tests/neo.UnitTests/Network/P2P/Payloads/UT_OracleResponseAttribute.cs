using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_OracleResponseAttribute
    {
        [TestMethod]
        public void TestSerialization()
        {
            OracleResponseAttribute attr = new OracleResponseAttribute()
            {
                RequestTx = UInt256.Parse("0x557f5c9d0c865a211a749899681e5b4fbf745b3bcf0c395e6d6a7f1edb0d86f1")
            };

            var data = attr.ToArray();
            var copy = data.AsSerializable<OracleResponseAttribute>();

            Assert.AreEqual(attr.Size, data.Length);

            Assert.AreEqual(attr.RequestTx, copy.RequestTx);
            Assert.AreEqual(attr.Type, copy.Type);
            Assert.AreEqual(attr.Size, copy.Size);
        }
    }
}
