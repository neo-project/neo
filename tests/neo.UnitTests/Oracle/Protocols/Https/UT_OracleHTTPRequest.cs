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

            Assert.AreEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.Filter = new Neo.Oracle.OracleFilter()
            {
                ContractHash = UInt160.Zero,
                FilterMethod = "",
                FilterArgs = ""
            };
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.URL = new Uri("https://google.com/?dummy=1");
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);

            requestB = CreateDefault();
            requestB.Method = (HttpMethod)0xFF;
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);
        }

        private OracleHttpsRequest CreateDefault()
        {
            return new OracleHttpsRequest()
            {
                Method = HttpMethod.GET,
                URL = new Uri("https://google.com")
            };
        }
    }
}
