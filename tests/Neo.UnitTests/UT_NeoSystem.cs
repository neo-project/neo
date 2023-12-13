using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
