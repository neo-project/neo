using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngineProvider
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationEngine.ResetApplicationEngineProvider();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ApplicationEngine.ResetApplicationEngineProvider();
        }

        [TestMethod]
        public void TestSetAppEngineProvider()
        {
            var provider = new TestProvider();
            ApplicationEngine.SetApplicationEngineProvider(provider).Should().BeTrue();

            using var appEngine = ApplicationEngine.Create(TriggerType.Application, null, null, null, 0);
            (appEngine is TestEngine).Should().BeTrue();
        }

        [TestMethod]
        public void TestDefaultAppEngineProvider()
        {
            using var appEngine = ApplicationEngine.Create(TriggerType.Application, null, null, null, 0);
            (appEngine is ApplicationEngine).Should().BeTrue();
        }

        [TestMethod]
        public void TestCantSetAppEngineProviderTwice()
        {
            var provider = new TestProvider();
            ApplicationEngine.SetApplicationEngineProvider(provider).Should().BeTrue();

            var provider2 = new TestProvider();
            ApplicationEngine.SetApplicationEngineProvider(provider2).Should().BeFalse();
        }

        [TestMethod]
        public void TestCanResetAppEngineProviderTwice()
        {
            var provider = new TestProvider();
            ApplicationEngine.SetApplicationEngineProvider(provider).Should().BeTrue();

            ApplicationEngine.ResetApplicationEngineProvider();

            var provider2 = new TestProvider();
            ApplicationEngine.SetApplicationEngineProvider(provider2).Should().BeTrue();
        }

        class TestProvider : IApplicationEngineProvider
        {
            public ApplicationEngine Create(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, long gas)
            {
                return new TestEngine(trigger, container, snapshot, persistingBlock, gas);
            }
        }

        class TestEngine : ApplicationEngine
        {
            public TestEngine(TriggerType trigger, IVerifiable container, DataCache snapshot, Block persistingBlock, long gas)
                : base(trigger, container, snapshot, persistingBlock, gas)
            {
            }
        }
    }
}
