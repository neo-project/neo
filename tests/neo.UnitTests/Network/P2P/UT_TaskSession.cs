using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Xunit.Sdk;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskSession
    {
        [TestMethod]
        public void CreateTest()
        {
            var ses = new TaskSession(null, new VersionPayload() { Capabilities = new NodeCapability[] { new FullNodeCapability(123) } });

            Assert.IsNull(ses.RemoteNode);
            Assert.IsFalse(ses.HasTask);
            Assert.AreEqual((uint)123, ses.LastBlockIndex);
            Assert.IsTrue(ses.IsFullNode);

            ses = new TaskSession(null, new VersionPayload() { Capabilities = new NodeCapability[0] });

            Assert.IsNull(ses.RemoteNode);
            Assert.IsFalse(ses.HasTask);
            Assert.AreEqual((uint)0, ses.LastBlockIndex);
            Assert.IsFalse(ses.IsFullNode);
        }
    }
}
