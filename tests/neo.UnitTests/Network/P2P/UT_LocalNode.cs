using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using System;
using System.Linq;
using System.Net;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_LocalNode 
    {
        private static NeoSystem testBlockchain;

        [TestInitialize]
        public void Init()
        {
            testBlockchain = TestBlockchain.TheNeoSystem;
        }

        [TestMethod]
        public void TestDefaults()
        {
            Assert.AreEqual(0, LocalNode.Singleton.ListenerTcpPort);
            Assert.AreEqual(0, LocalNode.Singleton.ListenerWsPort);
            Assert.AreEqual(3, LocalNode.Singleton.MaxConnectionsPerAddress);
            Assert.AreEqual(10, LocalNode.Singleton.MinDesiredConnections);
            Assert.AreEqual(40, LocalNode.Singleton.MaxConnections);
            Assert.AreEqual(0, LocalNode.Singleton.UnconnectedCount);

            CollectionAssert.AreEqual(Array.Empty<RemoteNode>(), LocalNode.Singleton.GetRemoteNodes().ToArray());
            CollectionAssert.AreEqual(Array.Empty<IPEndPoint>(), LocalNode.Singleton.GetUnconnectedPeers().ToArray());
        }
    }
}
