using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Oracle.Protocols.HTTPS;

namespace Neo.UnitTests.Oracle.Protocols.HTTPS
{
    [TestClass]
    public class UT_OracleHTTPSRequest
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
            requestB.Method = (OracleHTTPSRequest.HTTPMethod)0xFF;
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);
        }

        private OracleHTTPSRequest CreateDefault()
        {
            return new OracleHTTPSRequest()
            {
                Body = new byte[0],
                Filter = "",
                Method = OracleHTTPSRequest.HTTPMethod.GET,
                URL = ""
            };
        }
    }
}
