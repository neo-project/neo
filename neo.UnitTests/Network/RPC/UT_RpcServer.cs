using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC;
using System;
using System.Net;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_RpcServer
    {
        private RpcServer server;

        [TestInitialize]
        public void Setup()
        {
            var system = TestBlockchain.InitializeMockNeoSystem();
            server = new RpcServer(system);
        }

        [TestCleanup]
        public void TestDispose()
        {
            server.Dispose();
        }

        [TestMethod]
        public void TestWallet()
        {
            var wallet = TestUtils.GenerateTestWallet();
            server.Wallet = wallet;
            server.Wallet.Should().Be(wallet);
        }

        [TestMethod]
        public void TestMaxGasInvoke()
        {
            server.MaxGasInvoke.Should().Be(0);
        }

        [TestMethod]
        public void TestStart()
        {
            Action action = () => server.Start(IPAddress.Parse("127.0.0.1"), 8999);
            action.Should().NotThrow<Exception>();
        }
    }
}
