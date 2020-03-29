using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            requestB.Error = OracleResultError.FilterError;
            Assert.AreNotEqual(requestA.Hash, requestB.Hash);
        }

        private OracleResponse CreateDefault()
        {
            return new OracleResponse()
            {
                Error = OracleResultError.None,
                RequestHash = UInt160.Zero,
                Result = new byte[0]
            };
        }
    }
}
