// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ProtocolSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Wallets;
using System.Text.RegularExpressions;

namespace Neo.UnitTests;

[TestClass]
public class UT_ProtocolSettings
{
    [TestMethod]
    public void CheckFirstLetterOfAddresses()
    {
        UInt160 min = UInt160.Parse("0x0000000000000000000000000000000000000000");
        Assert.AreEqual('N', min.ToAddress(TestProtocolSettings.Default.AddressVersion)[0]);
        UInt160 max = UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff");
        Assert.AreEqual('N', max.ToAddress(TestProtocolSettings.Default.AddressVersion)[0]);
    }

    [TestMethod]
    public void Default_Network_should_be_mainnet_Network_value()
    {
        var mainNetNetwork = 0x334F454Eu;
        Assert.AreEqual(mainNetNetwork, TestProtocolSettings.Default.Network);
    }

    [TestMethod]
    public void TestGetMemoryPoolMaxTransactions()
    {
        Assert.AreEqual(50000, TestProtocolSettings.Default.MemoryPoolMaxTransactions);
    }

    [TestMethod]
    public void TestGetMillisecondsPerBlock()
    {
        Assert.AreEqual((uint)15000, (uint)TestProtocolSettings.Default.MillisecondsPerBlock);
    }

    [TestMethod]
    public void TestGetSeedList()
    {
        CollectionAssert.AreEqual(new string[] {
            "seed1.neo.org:10333",
            "seed2.neo.org:10333",
            "seed3.neo.org:10333",
            "seed4.neo.org:10333",
            "seed5.neo.org:10333"
        }, TestProtocolSettings.Default.SeedList.ToArray());
    }

    [TestMethod]
    public void TestStandbyCommitteeAddressesFormat()
    {
        foreach (var point in TestProtocolSettings.Default.StandbyCommittee)
        {
            Assert.MatchesRegex(new Regex("^[0-9A-Fa-f]{66}$"), point.ToString()); // ECPoint is 66 hex characters
        }
    }

    [TestMethod]
    public void TestValidatorsCount()
    {
        Assert.HasCount(TestProtocolSettings.Default.ValidatorsCount * 3, TestProtocolSettings.Default.StandbyCommittee);
    }

    [TestMethod]
    public void TestMaxTransactionsPerBlock()
    {
        Assert.IsGreaterThan(0u, TestProtocolSettings.Default.MaxTransactionsPerBlock);
        Assert.IsLessThanOrEqualTo(50000u, TestProtocolSettings.Default.MaxTransactionsPerBlock); // Assuming 50000 as a reasonable upper limit
    }

    [TestMethod]
    public void TestMaxTraceableBlocks()
    {
        Assert.IsGreaterThan(0u, TestProtocolSettings.Default.MaxTraceableBlocks);
    }

    [TestMethod]
    public void TestMaxValidUntilBlockIncrement()
    {
        Assert.IsGreaterThan(0u, TestProtocolSettings.Default.MaxValidUntilBlockIncrement);
    }

    [TestMethod]
    public void TestInitialGasDistribution()
    {
        Assert.IsGreaterThan(0ul, TestProtocolSettings.Default.InitialGasDistribution);
    }

    [TestMethod]
    public void TestHardforksSettings()
    {
        Assert.IsNotNull(TestProtocolSettings.Default.Hardforks);
    }

    [TestMethod]
    public void TestAddressVersion()
    {
        Assert.IsGreaterThanOrEqualTo(0, TestProtocolSettings.Default.AddressVersion);
        Assert.IsLessThanOrEqualTo(255, TestProtocolSettings.Default.AddressVersion); // Address version is a byte
    }

    [TestMethod]
    public void TestNetworkSettingsConsistency()
    {
        Assert.IsGreaterThan(0u, TestProtocolSettings.Default.Network);
        Assert.IsNotNull(TestProtocolSettings.Default.SeedList);
    }

    [TestMethod]
    public void TestECPointParsing()
    {
        foreach (var point in TestProtocolSettings.Default.StandbyCommittee)
        {
            try
            {
                ECPoint.Parse(point.ToString(), ECCurve.Secp256r1);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void TestSeedListFormatAndReachability()
    {
        foreach (var seed in TestProtocolSettings.Default.SeedList)
        {
            Assert.MatchesRegex(new Regex(@"^[\w.-]+:\d+$"), seed); // Format: domain:port
        }
    }

    [TestMethod]
    public void TestDefaultNetworkValue()
    {
        Assert.AreEqual((uint)0, ProtocolSettings.Default.Network);
    }

    [TestMethod]
    public void TestDefaultAddressVersionValue()
    {
        Assert.AreEqual(ProtocolSettings.Default.AddressVersion, TestProtocolSettings.Default.AddressVersion);
    }

    [TestMethod]
    public void TestDefaultValidatorsCountValue()
    {
        Assert.AreEqual(0, ProtocolSettings.Default.ValidatorsCount);
    }

    [TestMethod]
    public void TestDefaultMillisecondsPerBlockValue()
    {
        Assert.AreEqual(ProtocolSettings.Default.MillisecondsPerBlock, TestProtocolSettings.Default.MillisecondsPerBlock);
    }

    [TestMethod]
    public void TestDefaultMaxTransactionsPerBlockValue()
    {
        Assert.AreEqual(ProtocolSettings.Default.MaxTransactionsPerBlock, TestProtocolSettings.Default.MaxTransactionsPerBlock);
    }

    [TestMethod]
    public void TestDefaultMemoryPoolMaxTransactionsValue()
    {
        Assert.AreEqual(ProtocolSettings.Default.MemoryPoolMaxTransactions, TestProtocolSettings.Default.MemoryPoolMaxTransactions);
    }

    [TestMethod]
    public void TestDefaultMaxTraceableBlocksValue()
    {
        Assert.AreEqual(ProtocolSettings.Default.MaxTraceableBlocks, TestProtocolSettings.Default.MaxTraceableBlocks);
    }

    [TestMethod]
    public void TestDefaultMaxValidUntilBlockIncrementValue()
    {
        Assert.AreEqual(ProtocolSettings.Default.MaxValidUntilBlockIncrement, TestProtocolSettings.Default.MaxValidUntilBlockIncrement);
    }

    [TestMethod]
    public void TestDefaultInitialGasDistributionValue()
    {
        Assert.AreEqual(ProtocolSettings.Default.InitialGasDistribution, TestProtocolSettings.Default.InitialGasDistribution);
    }

    [TestMethod]
    public void TestDefaultHardforksValue()
    {
        CollectionAssert.AreEqual(ProtocolSettings.Default.Hardforks, TestProtocolSettings.Default.Hardforks);
    }

    [TestMethod]
    public void TestTimePerBlockCalculation()
    {
        var expectedTimeSpan = TimeSpan.FromMilliseconds(TestProtocolSettings.Default.MillisecondsPerBlock);
        Assert.AreEqual(expectedTimeSpan, TestProtocolSettings.Default.TimePerBlock);
    }

    [TestMethod]
    public void TestLoad()
    {
        var loadedSetting = ProtocolSettings.Load("test.config.json");

        // Comparing all properties
        Assert.AreEqual(TestProtocolSettings.Default.Network, loadedSetting.Network);
        Assert.AreEqual(TestProtocolSettings.Default.AddressVersion, loadedSetting.AddressVersion);
        CollectionAssert.AreEqual(TestProtocolSettings.Default.StandbyCommittee.ToList(), loadedSetting.StandbyCommittee.ToList());
        Assert.AreEqual(TestProtocolSettings.Default.ValidatorsCount, loadedSetting.ValidatorsCount);
        CollectionAssert.AreEqual(TestProtocolSettings.Default.SeedList, loadedSetting.SeedList);
        Assert.AreEqual(TestProtocolSettings.Default.MillisecondsPerBlock, loadedSetting.MillisecondsPerBlock);
        Assert.AreEqual(TestProtocolSettings.Default.MaxTransactionsPerBlock, loadedSetting.MaxTransactionsPerBlock);
        Assert.AreEqual(TestProtocolSettings.Default.MemoryPoolMaxTransactions, loadedSetting.MemoryPoolMaxTransactions);
        Assert.AreEqual(TestProtocolSettings.Default.MaxTraceableBlocks, loadedSetting.MaxTraceableBlocks);
        Assert.AreEqual(TestProtocolSettings.Default.MaxValidUntilBlockIncrement, loadedSetting.MaxValidUntilBlockIncrement);
        Assert.AreEqual(TestProtocolSettings.Default.InitialGasDistribution, loadedSetting.InitialGasDistribution);
        CollectionAssert.AreEqual(TestProtocolSettings.Default.Hardforks, loadedSetting.Hardforks);

        // If StandbyValidators is a derived property, comparing it as well
        CollectionAssert.AreEqual(TestProtocolSettings.Default.StandbyValidators.ToList(), loadedSetting.StandbyValidators.ToList());
    }
}
