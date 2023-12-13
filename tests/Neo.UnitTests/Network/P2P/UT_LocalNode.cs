using System;
using System.Linq;
using System.Net;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_LocalNode : TestKit
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
            var senderProbe = CreateTestProbe();
            senderProbe.Send(testBlockchain.LocalNode, new LocalNode.GetInstance());
            var localnode = senderProbe.ExpectMsg<LocalNode>();

            Assert.AreEqual(0, localnode.ListenerTcpPort);
            Assert.AreEqual(0, localnode.ListenerWsPort);
            Assert.AreEqual(3, localnode.MaxConnectionsPerAddress);
            Assert.AreEqual(10, localnode.MinDesiredConnections);
            Assert.AreEqual(40, localnode.MaxConnections);
            Assert.AreEqual(0, localnode.UnconnectedCount);

            CollectionAssert.AreEqual(Array.Empty<RemoteNode>(), localnode.GetRemoteNodes().ToArray());
            CollectionAssert.AreEqual(Array.Empty<IPEndPoint>(), localnode.GetUnconnectedPeers().ToArray());
        }
    }
}
