using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using System;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_AssetDescriptor
    {
        [TestMethod]
        public void TestConstructorWithNonexistAssetId()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            Action action = () =>
            {
                var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, ProtocolSettings.Default, UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4"));
            };
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Check_GAS()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, ProtocolSettings.Default, NativeContract.GAS.Hash);
            descriptor.AssetId.Should().Be(NativeContract.GAS.Hash);
            descriptor.AssetName.Should().Be(nameof(GasToken));
            descriptor.ToString().Should().Be(nameof(GasToken));
            descriptor.Symbol.Should().Be("GAS");
            descriptor.Decimals.Should().Be(8);
        }
    }
}
