using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;
using Xunit.Sdk;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskSession
    {
        [TestMethod]
        public void CreateTest()
        {
            var ses = new TaskSession(new VersionPayload() { Capabilities = new NodeCapability[] { new FullNodeCapability(123) } });

            Assert.IsFalse(ses.HasTask);
            Assert.AreEqual((uint)123, ses.LastBlockIndex);
            Assert.AreEqual(0, ses.IndexTasks.Count);
            Assert.IsTrue(ses.IsFullNode);

            ses = new TaskSession(new VersionPayload() { Capabilities = Array.Empty<NodeCapability>() });

            Assert.IsFalse(ses.HasTask);
            Assert.AreEqual((uint)0, ses.LastBlockIndex);
            Assert.AreEqual(0, ses.IndexTasks.Count);
            Assert.IsFalse(ses.IsFullNode);
        }
    }
}
