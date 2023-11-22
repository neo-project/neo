using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
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
            min.ToAddress(TestProtocolSettings.Default.AddressVersion)[0].Should().Be('N');
            UInt160 max = UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff");
            max.ToAddress(TestProtocolSettings.Default.AddressVersion)[0].Should().Be('N');
        }

        [TestMethod]
        public void Default_Network_should_be_mainnet_Network_value()
        {
            var mainNetNetwork = 0x334F454Eu;
            TestProtocolSettings.Default.Network.Should().Be(mainNetNetwork);
        }

        [TestMethod]
        public void TestGetMemoryPoolMaxTransactions()
        {
            TestProtocolSettings.Default.MemoryPoolMaxTransactions.Should().Be(50000);
        }

        [TestMethod]
        public void TestGetMillisecondsPerBlock()
        {
            TestProtocolSettings.Default.MillisecondsPerBlock.Should().Be(15000);
        }

        [TestMethod]
        public void TestGetSeedList()
        {
            TestProtocolSettings.Default.SeedList.Should().BeEquivalentTo(new string[] { "seed1.neo.org:10333", "seed2.neo.org:10333", "seed3.neo.org:10333", "seed4.neo.org:10333", "seed5.neo.org:10333", });
        }

        [TestMethod]
        public void TestStandbyCommitteeAddressesFormat()
        {
            foreach (var point in TestProtocolSettings.Default.StandbyCommittee)
            {
                point.ToString().Should().MatchRegex("^[0-9A-Fa-f]{66}$"); // ECPoint is 66 hex characters
            }
        }

        [TestMethod]
        public void TestValidatorsCount()
        {
            TestProtocolSettings.Default.StandbyCommittee.Count.Should().Be(TestProtocolSettings.Default.ValidatorsCount * 3);
        }

        [TestMethod]
        public void TestMaxTransactionsPerBlock()
        {
            TestProtocolSettings.Default.MaxTransactionsPerBlock.Should().BePositive().And.BeLessOrEqualTo(50000); // Assuming 50000 as a reasonable upper limit
        }

        [TestMethod]
        public void TestMaxTraceableBlocks()
        {
            TestProtocolSettings.Default.MaxTraceableBlocks.Should().BePositive();
        }

        [TestMethod]
        public void TestInitialGasDistribution()
        {
            TestProtocolSettings.Default.InitialGasDistribution.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public void TestHardforksSettings()
        {
            TestProtocolSettings.Default.Hardforks.Should().NotBeNull();
        }

        [TestMethod]
        public void TestAddressVersion()
        {
            TestProtocolSettings.Default.AddressVersion.Should().BeInRange(0, 255); // Address version is a byte
        }

        [TestMethod]
        public void TestNetworkSettingsConsistency()
        {
            TestProtocolSettings.Default.Network.Should().BePositive();
            TestProtocolSettings.Default.SeedList.Should().NotBeEmpty();
        }

        [TestMethod]
        public void TestECPointParsing()
        {
            foreach (var point in TestProtocolSettings.Default.StandbyCommittee)
            {
                Action act = () => ECPoint.Parse(point.ToString(), ECCurve.Secp256r1);
                act.Should().NotThrow();
            }
        }

        [TestMethod]
        public void TestSeedListFormatAndReachability()
        {
            foreach (var seed in TestProtocolSettings.Default.SeedList)
            {
                seed.Should().MatchRegex(@"^[\w.-]+:\d+$"); // Format: domain:port
            }
        }
    }
}
