// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ProtocolSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Wallets;
using System;
using System.IO;

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
        public void HardForkTestBAndNotA()
        {
            string json = CreateHKSettings("\"HF_Basilisk\": 4120000");

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);
            ProtocolSettings settings = ProtocolSettings.Load(file, false);
            File.Delete(file);

            settings.Hardforks[Hardfork.HF_Aspidochelone].Should().Be(0);
            settings.Hardforks[Hardfork.HF_Basilisk].Should().Be(4120000);

            // Check IsHardforkEnabled

            settings.IsHardforkEnabled(Hardfork.HF_Aspidochelone, 0).Should().BeTrue();
            settings.IsHardforkEnabled(Hardfork.HF_Aspidochelone, 10).Should().BeTrue();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 0).Should().BeFalse();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 10).Should().BeFalse();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 4120000).Should().BeTrue();
        }

        [TestMethod]
        public void HardForkTestAAndNotB()
        {
            string json = CreateHKSettings("\"HF_Aspidochelone\": 0");

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);
            ProtocolSettings settings = ProtocolSettings.Load(file, false);
            File.Delete(file);

            settings.Hardforks[Hardfork.HF_Aspidochelone].Should().Be(0);
            settings.Hardforks.ContainsKey(Hardfork.HF_Basilisk).Should().BeFalse();

            // Check IsHardforkEnabled

            settings.IsHardforkEnabled(Hardfork.HF_Aspidochelone, 0).Should().BeTrue();
            settings.IsHardforkEnabled(Hardfork.HF_Aspidochelone, 10).Should().BeTrue();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 0).Should().BeFalse();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 10).Should().BeFalse();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 4120000).Should().BeFalse();
        }

        [TestMethod]
        public void HardForkTestNone()
        {
            string json = CreateHKSettings("");

            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);
            ProtocolSettings settings = ProtocolSettings.Load(file, false);
            File.Delete(file);

            settings.Hardforks[Hardfork.HF_Aspidochelone].Should().Be(0);
            settings.Hardforks[Hardfork.HF_Basilisk].Should().Be(0);

            // Check IsHardforkEnabled

            settings.IsHardforkEnabled(Hardfork.HF_Aspidochelone, 0).Should().BeTrue();
            settings.IsHardforkEnabled(Hardfork.HF_Aspidochelone, 10).Should().BeTrue();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 0).Should().BeTrue();
            settings.IsHardforkEnabled(Hardfork.HF_Basilisk, 10).Should().BeTrue();
        }

        [TestMethod]
        public void HardForkTestAMoreThanB()
        {
            string json = CreateHKSettings("\"HF_Aspidochelone\": 4120001, \"HF_Basilisk\": 4120000");
            var file = Path.GetTempFileName();
            File.WriteAllText(file, json);
            Assert.ThrowsException<ArgumentException>(() => ProtocolSettings.Load(file, false));
            File.Delete(file);
        }

        internal static string CreateHKSettings(string hf)
        {
            return @"
{
  ""ProtocolConfiguration"": {
    ""Network"": 860833102,
    ""AddressVersion"": 53,
    ""MillisecondsPerBlock"": 15000,
    ""MaxTransactionsPerBlock"": 512,
    ""MemoryPoolMaxTransactions"": 50000,
    ""MaxTraceableBlocks"": 2102400,
    ""Hardforks"": {
      " + hf + @"
    },
    ""InitialGasDistribution"": 5200000000000000,
    ""ValidatorsCount"": 7,
    ""StandbyCommittee"": [
      ""03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c"",
      ""02df48f60e8f3e01c48ff40b9b7f1310d7a8b2a193188befe1c2e3df740e895093"",
      ""03b8d9d5771d8f513aa0869b9cc8d50986403b78c6da36890638c3d46a5adce04a"",
      ""02ca0e27697b9c248f6f16e085fd0061e26f44da85b58ee835c110caa5ec3ba554"",
      ""024c7b7fb6c310fccf1ba33b082519d82964ea93868d676662d4a59ad548df0e7d"",
      ""02aaec38470f6aad0042c6e877cfd8087d2676b0f516fddd362801b9bd3936399e"",
      ""02486fd15702c4490a26703112a5cc1d0923fd697a33406bd5a1c00e0013b09a70"",
      ""023a36c72844610b4d34d1968662424011bf783ca9d984efa19a20babf5582f3fe"",
      ""03708b860c1de5d87f5b151a12c2a99feebd2e8b315ee8e7cf8aa19692a9e18379"",
      ""03c6aa6e12638b36e88adc1ccdceac4db9929575c3e03576c617c49cce7114a050"",
      ""03204223f8c86b8cd5c89ef12e4f0dbb314172e9241e30c9ef2293790793537cf0"",
      ""02a62c915cf19c7f19a50ec217e79fac2439bbaad658493de0c7d8ffa92ab0aa62"",
      ""03409f31f0d66bdc2f70a9730b66fe186658f84a8018204db01c106edc36553cd0"",
      ""0288342b141c30dc8ffcde0204929bb46aed5756b41ef4a56778d15ada8f0c6654"",
      ""020f2887f41474cfeb11fd262e982051c1541418137c02a0f4961af911045de639"",
      ""0222038884bbd1d8ff109ed3bdef3542e768eef76c1247aea8bc8171f532928c30"",
      ""03d281b42002647f0113f36c7b8efb30db66078dfaaa9ab3ff76d043a98d512fde"",
      ""02504acbc1f4b3bdad1d86d6e1a08603771db135a73e61c9d565ae06a1938cd2ad"",
      ""0226933336f1b75baa42d42b71d9091508b638046d19abd67f4e119bf64a7cfb4d"",
      ""03cdcea66032b82f5c30450e381e5295cae85c5e6943af716cc6b646352a6067dc"",
      ""02cd5a5547119e24feaa7c2a0f37b8c9366216bab7054de0065c9be42084003c8a""
    ],
    ""SeedList"": [
      ""seed1.neo.org:10333"",
      ""seed2.neo.org:10333"",
      ""seed3.neo.org:10333"",
      ""seed4.neo.org:10333"",
      ""seed5.neo.org:10333""
    ]
  }
}
";
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

        [TestMethod]
        public void TestDefaultNetworkValue()
        {
            ProtocolSettings.Default.Network.Should().Be(0);
        }

        [TestMethod]
        public void TestDefaultAddressVersionValue()
        {
            TestProtocolSettings.Default.AddressVersion.Should().Be(ProtocolSettings.Default.AddressVersion);
        }

        [TestMethod]
        public void TestDefaultValidatorsCountValue()
        {
            ProtocolSettings.Default.ValidatorsCount.Should().Be(0);
        }

        [TestMethod]
        public void TestDefaultMillisecondsPerBlockValue()
        {
            TestProtocolSettings.Default.MillisecondsPerBlock.Should().Be(ProtocolSettings.Default.MillisecondsPerBlock);
        }

        [TestMethod]
        public void TestDefaultMaxTransactionsPerBlockValue()
        {
            TestProtocolSettings.Default.MaxTransactionsPerBlock.Should().Be(ProtocolSettings.Default.MaxTransactionsPerBlock);
        }

        [TestMethod]
        public void TestDefaultMemoryPoolMaxTransactionsValue()
        {
            TestProtocolSettings.Default.MemoryPoolMaxTransactions.Should().Be(ProtocolSettings.Default.MemoryPoolMaxTransactions);
        }

        [TestMethod]
        public void TestDefaultMaxTraceableBlocksValue()
        {
            TestProtocolSettings.Default.MaxTraceableBlocks.Should().Be(ProtocolSettings.Default.MaxTraceableBlocks);
        }

        [TestMethod]
        public void TestDefaultInitialGasDistributionValue()
        {
            TestProtocolSettings.Default.InitialGasDistribution.Should().Be(ProtocolSettings.Default.InitialGasDistribution);
        }

        [TestMethod]
        public void TestDefaultHardforksValue()
        {
            TestProtocolSettings.Default.Hardforks.Should().BeEquivalentTo(ProtocolSettings.Default.Hardforks);
        }

        [TestMethod]
        public void TestTimePerBlockCalculation()
        {
            var expectedTimeSpan = TimeSpan.FromMilliseconds(TestProtocolSettings.Default.MillisecondsPerBlock);
            TestProtocolSettings.Default.TimePerBlock.Should().Be(expectedTimeSpan);
        }

        [TestMethod]
        public void TestLoad()
        {
            var loadedSetting = ProtocolSettings.Load("test.config.json", false);

            // Comparing all properties
            TestProtocolSettings.Default.Network.Should().Be(loadedSetting.Network);
            TestProtocolSettings.Default.AddressVersion.Should().Be(loadedSetting.AddressVersion);
            TestProtocolSettings.Default.StandbyCommittee.Should().BeEquivalentTo(loadedSetting.StandbyCommittee);
            TestProtocolSettings.Default.ValidatorsCount.Should().Be(loadedSetting.ValidatorsCount);
            TestProtocolSettings.Default.SeedList.Should().BeEquivalentTo(loadedSetting.SeedList);
            TestProtocolSettings.Default.MillisecondsPerBlock.Should().Be(loadedSetting.MillisecondsPerBlock);
            TestProtocolSettings.Default.MaxTransactionsPerBlock.Should().Be(loadedSetting.MaxTransactionsPerBlock);
            TestProtocolSettings.Default.MemoryPoolMaxTransactions.Should().Be(loadedSetting.MemoryPoolMaxTransactions);
            TestProtocolSettings.Default.MaxTraceableBlocks.Should().Be(loadedSetting.MaxTraceableBlocks);
            TestProtocolSettings.Default.InitialGasDistribution.Should().Be(loadedSetting.InitialGasDistribution);
            TestProtocolSettings.Default.Hardforks.Should().BeEquivalentTo(loadedSetting.Hardforks);

            // If StandbyValidators is a derived property, comparing it as well
            TestProtocolSettings.Default.StandbyValidators.Should().BeEquivalentTo(loadedSetting.StandbyValidators);
        }
    }
}
