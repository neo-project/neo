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
        private Mock<ISnapshot> _iSnapshotMock;

        [TestInitialize]
        public void TestSetup()
        {
            // Mock IStore and ISnapshot
            var mockStore = new Mock<IStore>();

            // Setup mock behaviors for ISnapshot
            _iSnapshotMock.Setup(snapshot => snapshot.TryGet(It.IsAny<byte[]>()))
                        .Returns((byte[] key) => null); // Return null or appropriate value
            _iSnapshotMock.Setup(snapshot => snapshot.Seek(It.IsAny<byte[]>(), It.IsAny<SeekDirection>()))
                        .Returns(new List<(byte[], byte[])>()); // Return an empty list or appropriate values

            // Setup mock behaviors for IStore
            mockStore.Setup(store => store.GetSnapshot()).Returns(_iSnapshotMock.Object);
            mockStore.Setup(store => store.Put(It.IsAny<byte[]>(), It.IsAny<byte[]>()));
            mockStore.Setup(store => store.Delete(It.IsAny<byte[]>()));
            mockStore.Setup(store => store.Contains(It.IsAny<byte[]>())).Returns(false); // Return appropriate value

            // Mock IStoreProvider
            var mockStoreProvider = new Mock<IStoreProvider>();
            mockStoreProvider.Setup(provider => provider.GetStore(It.IsAny<string>())).Returns(mockStore.Object);

            // Initialize NeoSystem with the mocked store provider
            var protocolSettings = TestProtocolSettings.Default;
            var neoSystem = new NeoSystem(protocolSettings, mockStoreProvider.Object);

            // Initialize RpcServer with the actual NeoSystem and default settings
            _rpcServer = new RpcServer(neoSystem, RpcServerSettings.Default);
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
