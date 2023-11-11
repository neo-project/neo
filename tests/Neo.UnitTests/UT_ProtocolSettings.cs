using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ProtocolSettings
    {
        [TestMethod]
        public void CheckFirstLetterOfAddresses()
        {
            UInt160 min = UInt160.Parse("0x0000000000000000000000000000000000000000");
            min.ToAddress(ProtocolSettings.Default.AddressVersion)[0].Should().Be('N');
            UInt160 max = UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff");
            max.ToAddress(ProtocolSettings.Default.AddressVersion)[0].Should().Be('N');
        }

        [TestMethod]
        public void Default_Network_should_be_mainnet_Network_value()
        {
            var mainNetNetwork = 0x334F454Eu;
            ProtocolSettings.Default.Network.Should().Be(mainNetNetwork);
        }

        [TestMethod]
        public void TestGetMemoryPoolMaxTransactions()
        {
            ProtocolSettings.Default.MemoryPoolMaxTransactions.Should().Be(50000);
        }

        [TestMethod]
        public void TestGetMillisecondsPerBlock()
        {
            ProtocolSettings.Default.MillisecondsPerBlock.Should().Be(15000);
        }

        [TestMethod]
        public void TestGetSeedList()
        {
            ProtocolSettings.Default.SeedList.Should().BeEquivalentTo(new string[] { "seed1.neo.org:10333", "seed2.neo.org:10333", "seed3.neo.org:10333", "seed4.neo.org:10333", "seed5.neo.org:10333", });
        }
    }
}
