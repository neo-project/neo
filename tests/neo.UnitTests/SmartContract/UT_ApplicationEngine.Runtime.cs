using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestGetRandom()
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            uint rand_1 = engine.GetRandom();
            uint rand_2 = engine.GetRandom();
            uint rand_3 = engine.GetRandom();
            uint rand_4 = engine.GetRandom();
            uint rand_5 = engine.GetRandom();

            rand_1.Should().Be(2083236893u);
            rand_2.Should().Be(3262894070u);
            rand_3.Should().Be(1882351097u);
            rand_4.Should().Be(2376063185u);
            rand_5.Should().Be(2304732336u);
        }
    }
}
