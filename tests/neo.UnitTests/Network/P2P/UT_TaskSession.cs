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
            Assert.ThrowsException<NullReferenceException>(() => new TaskSession(null));

            var ses = new TaskSession(new VersionPayload() { Capabilities = new NodeCapability[] { new FullNodeCapability(123) } });

            Assert.IsTrue(ses.IsFullNode);
            Assert.AreEqual((uint)123, ses.LastBlockIndex);
            Assert.AreEqual(0, ses.IndexTasks.Count);
            Assert.AreEqual(0, ses.InvTasks.Count);
            Assert.AreEqual((uint)0, ses.TimeoutTimes);
            Assert.AreEqual((uint)0, ses.InvalidBlockCount);

            ses = new TaskSession(new VersionPayload() { Capabilities = new NodeCapability[0] });

            Assert.IsFalse(ses.IsFullNode);
            Assert.AreEqual((uint)0, ses.LastBlockIndex);
            Assert.AreEqual(0, ses.IndexTasks.Count);
            Assert.AreEqual(0, ses.InvTasks.Count);
            Assert.AreEqual((uint)0, ses.TimeoutTimes);
            Assert.AreEqual((uint)0, ses.InvalidBlockCount);
        }
    }
}
