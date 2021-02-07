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
            var localNode = new LocalNode(testBlockchain);

            Assert.AreEqual(0, localNode.ListenerTcpPort);
            Assert.AreEqual(0, localNode.ListenerWsPort);
            Assert.AreEqual(3, localNode.MaxConnectionsPerAddress);
            Assert.AreEqual(10, localNode.MinDesiredConnections);
            Assert.AreEqual(40, localNode.MaxConnections);
            Assert.AreEqual(0, localNode.UnconnectedCount);

            CollectionAssert.AreEqual(Array.Empty<RemoteNode>(), localNode.GetRemoteNodes().ToArray());
            CollectionAssert.AreEqual(Array.Empty<IPEndPoint>(), localNode.GetUnconnectedPeers().ToArray());
        }
    }
}
