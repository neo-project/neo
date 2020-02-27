using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Oracle.Protocols.Https;
using System;

namespace Neo.UnitTests.Oracle.Protocols.Https
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
            requestB.URL = new Uri("https://google.es/?dummy=1");
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.Method = (OracleHttpsRequest.HTTPMethod)0xFF;
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);
        }

        private OracleHttpsRequest CreateDefault()
        {
            return new OracleHttpsRequest()
            {
                Body = new byte[0],
                Filter = "",
                Method = OracleHttpsRequest.HTTPMethod.GET,
                URL = new Uri("https://google.es")
            };
        }
    }
}
