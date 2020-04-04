using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Oracle.Protocols.Https;
using System.Net;

namespace Neo.UnitTests.Oracle.Protocols.Https
{
    [TestClass]
    public class UT_OracleHttpsProtocol
    {
        static readonly string[] Internal = new string[]
        {
            "::",
            "::1",
            "0.0.0.0",
            "255.255.255.255",
            "127.0.0.1",
            "192.168.1.1",
            "172.22.32.11"
        };

        static readonly string[] External = new string[]
        {
            "88.22.32.11"
        };

        [TestMethod]
        public void TestIsInternalAddress()
        {
            foreach (var i in Internal)
            {
                Assert.IsTrue(OracleHttpsProtocol.IsInternal(IPAddress.Parse(i)), $"{i} is not internal");
            }

            foreach (var i in External)
            {
                Assert.IsFalse(OracleHttpsProtocol.IsInternal(IPAddress.Parse(i)), $"{i} is internal");
            }
        }

        [TestMethod]
        public void TestIsPrivateHost()
        {
            IPHostEntry entry = new IPHostEntry();

            foreach (var i in Internal)
            {
                entry.AddressList = new IPAddress[] { IPAddress.Parse(i) };

                Assert.IsTrue(OracleHttpsProtocol.IsInternal(entry), $"{i} is not internal");
            }

            foreach (var i in External)
            {
                entry.AddressList = new IPAddress[] { IPAddress.Parse(i) };

                Assert.IsFalse(OracleHttpsProtocol.IsInternal(entry), $"{i} is internal");
            }
        }
    }
}
