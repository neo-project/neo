using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public partial class UT_RpcServer
    {
        private NeoSystem _neoSystem;
        private RpcServer _rpcServer;
        private TestMemoryStoreProvider _memoryStoreProvider;
        private MemoryStore _memoryStore;

        [TestInitialize]
        public void TestSetup()
        {
            _memoryStore = new MemoryStore();
            _memoryStoreProvider = new TestMemoryStoreProvider(_memoryStore);
            var protocolSettings = TestProtocolSettings.Default;
            _neoSystem = new NeoSystem(protocolSettings, _memoryStoreProvider);
            _rpcServer = new RpcServer(_neoSystem, RpcServerSettings.Default);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _memoryStore.GetSnapshot().Commit();
            _memoryStore.Reset();
        }

        [TestMethod]
        public void TestCheckAuth_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:testpass"));

            // Act
            var result = _rpcServer.CheckAuth(context);

            // Assert
            Assert.IsTrue(result);
        }


    }
}
