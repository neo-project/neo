using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Oracle.Protocols.Https;
using System;
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

        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

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

        [TestMethod]
        public void WrongProtocol()
        {
            var protocol = new OracleHttpsProtocol
            {
                AllowPrivateHost = true
            };

            var request = new OracleHttpsRequest() { Filter = null, Method = (HttpMethod)0xFF, URL = new Uri("https://google.com") };
            var response = protocol.Process(request);

            Assert.IsTrue(response.Error);
            Assert.IsNull(response.Result);
            Assert.AreEqual(request.Hash, response.RequestHash);
        }

        [TestMethod]
        public void WrongTimeout()
        {
            var protocol = new OracleHttpsProtocol
            {
                AllowPrivateHost = true
            };

            protocol.Config.TimeOut = 0;
            var request = new OracleHttpsRequest() { Filter = null, Method = HttpMethod.GET, URL = new Uri("https://google.com") };
            var response = protocol.Process(request);

            Assert.IsTrue(response.Error);
            Assert.IsNull(response.Result);
            Assert.AreEqual(request.Hash, response.RequestHash);
        }
    }
}
