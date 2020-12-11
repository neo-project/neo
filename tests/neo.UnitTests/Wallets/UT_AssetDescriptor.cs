using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using System;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_AssetDescriptor
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestConstructorWithNonexistAssetId()
        {
            Action action = () =>
            {
                var descriptor = new Neo.Wallets.AssetDescriptor(UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4"));
            };
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Check_GAS()
        {
            var descriptor = new Neo.Wallets.AssetDescriptor(NativeContract.GAS.Hash);
            descriptor.AssetId.Should().Be(NativeContract.GAS.Hash);
            descriptor.AssetName.Should().Be(nameof(GasToken));
            descriptor.ToString().Should().Be(nameof(GasToken));
            descriptor.Decimals.Should().Be(8);
        }

        [TestMethod]
        public void Check_NEO()
        {
            var descriptor = new Neo.Wallets.AssetDescriptor(NativeContract.NEO.Hash);
            descriptor.AssetId.Should().Be(NativeContract.NEO.Hash);
            descriptor.AssetName.Should().Be(nameof(NeoToken));
            descriptor.ToString().Should().Be(nameof(NeoToken));
            descriptor.Decimals.Should().Be(0);
        }
    }
}
