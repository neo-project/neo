using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NeoSystem
    {
        private NeoSystem neoSystem;

        [TestInitialize]
        public void Setup()
        {
            neoSystem = TestBlockchain.TheNeoSystem;
        }

        [TestMethod]
        public void TestGetBlockchain() => neoSystem.Blockchain.Should().NotBeNull();

        [TestMethod]
        public void TestGetLocalNode() => neoSystem.LocalNode.Should().NotBeNull();

        [TestMethod]
        public void TestGetTaskManager() => neoSystem.TaskManager.Should().NotBeNull();

        [TestMethod]
        public void TestGetConsensus() => neoSystem.Consensus.Should().BeNull();

        [TestMethod]
        public void TestGetOracle() => neoSystem.Oracle.Should().BeNull();

        [TestMethod]
        public void TestStartStopOracle()
        {
            Assert.IsFalse(neoSystem.StopOracle());
            Assert.ThrowsException<ArgumentException>(() => neoSystem.StartOracle(null, 0));

            neoSystem.StartOracle(null, 1);
            neoSystem.StartOracle(null, 1); // Do nothing
            Assert.IsTrue(neoSystem.StopOracle());
        }
    }
}
