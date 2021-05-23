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
            using var engine_1 = ApplicationEngine.Create(TriggerType.Application, null, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);
            using var engine_2 = ApplicationEngine.Create(TriggerType.Application, null, null, TestBlockchain.TheNeoSystem.GenesisBlock, settings: TestBlockchain.TheNeoSystem.Settings, gas: 1100_00000000);

            uint rand_1 = engine_1.GetRandom();
            uint rand_2 = engine_1.GetRandom();
            uint rand_3 = engine_1.GetRandom();
            uint rand_4 = engine_1.GetRandom();
            uint rand_5 = engine_1.GetRandom();

            uint rand_6 = engine_2.GetRandom();
            uint rand_7 = engine_2.GetRandom();
            uint rand_8 = engine_2.GetRandom();
            uint rand_9 = engine_2.GetRandom();
            uint rand_10 = engine_2.GetRandom();

            rand_1.Should().Be(2083236893u);
            rand_2.Should().Be(3262894070u);
            rand_3.Should().Be(1882351097u);
            rand_4.Should().Be(2376063185u);
            rand_5.Should().Be(2304732336u);

            rand_1.Should().NotBe(rand_6);
            rand_2.Should().NotBe(rand_7);
            rand_3.Should().NotBe(rand_8);
            rand_4.Should().NotBe(rand_9);
            rand_5.Should().NotBe(rand_10);
        }
    }
}
