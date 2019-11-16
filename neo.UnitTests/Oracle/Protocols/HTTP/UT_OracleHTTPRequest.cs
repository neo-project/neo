using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Oracle.Protocols.HTTP;

namespace Neo.UnitTests.Oracle.Protocols.HTTP
{
    [TestClass]
    public class UT_OracleHTTPRequest
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
            requestB.Method = OracleHTTPRequest.HTTPMethod.GET;
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);
        }

        private OracleHTTPRequest CreateDefault()
        {
            return new OracleHTTPRequest()
            {
                Body = new byte[0],
                Filter = "",
                Method = OracleHTTPRequest.HTTPMethod.DELETE,
                URL = ""
            };
        }
    }
}
