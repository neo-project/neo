using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract.Native;
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
                var snapshot = Blockchain.Singleton.GetSnapshot();
                var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4"));
            };
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Check_GAS()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, NativeContract.GAS.Hash);
            descriptor.AssetId.Should().Be(NativeContract.GAS.Hash);
            descriptor.AssetName.Should().Be("GAS");
            descriptor.ToString().Should().Be("GAS");
            descriptor.Decimals.Should().Be(8);
        }

        [TestMethod]
        public void Check_NEO()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, NativeContract.NEO.Hash);
            descriptor.AssetId.Should().Be(NativeContract.NEO.Hash);
            descriptor.AssetName.Should().Be("NEO");
            descriptor.ToString().Should().Be("NEO");
            descriptor.Decimals.Should().Be(0);
        }
    }
}
