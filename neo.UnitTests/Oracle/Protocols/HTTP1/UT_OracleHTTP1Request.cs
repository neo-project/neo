using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Oracle.Protocols.HTTP1;

namespace Neo.UnitTests.Oracle.Protocols.HTTP1
{
    [TestClass]
    public class UT_OracleHTTP1Request
    {
        [TestMethod]
        public void TestHash()
        {
            var requestA = CreateDefault();
            var requestB = CreateDefault();

            requestB.Body = new byte[1];
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.Filter = "X";
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.URL = "X";
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.Method = OracleHTTP1Method.GET;
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);
        }

        private OracleHTTP1Request CreateDefault()
        {
            return new OracleHTTP1Request()
            {
                Body = new byte[0],
                Filter = "",
                Method = OracleHTTP1Method.DELETE,
                URL = ""
            };
        }
    }
}
