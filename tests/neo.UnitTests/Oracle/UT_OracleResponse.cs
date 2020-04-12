using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Oracle;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleResponse
    {
        [TestMethod]
        public void TestHash()
        {
            var requestA = CreateDefault();
            var requestB = CreateDefault();

            requestB.Result = new byte[1];
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.RequestHash = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.Result = null;
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);
        }

        [TestMethod]
        public void TestSerialization()
        {
            var entry = CreateDefault();
            var data = entry.ToArray();

            Assert.AreEqual(entry.Size, data.Length);

            var copy = data.AsSerializable<OracleResponse>();

            Assert.AreEqual(entry.Hash, copy.Hash);
            Assert.AreEqual(entry.Error, copy.Error);
            Assert.AreEqual(entry.RequestHash, copy.RequestHash);
            CollectionAssert.AreEqual(entry.Result, copy.Result);
        }

        internal static OracleResponse CreateDefault()
        {
            return new OracleResponse()
            {
                RequestHash = UInt160.Parse("0xff00ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                Result = new byte[] { 0x01, 0x02, 0x03 }
            };
        }
    }
}
