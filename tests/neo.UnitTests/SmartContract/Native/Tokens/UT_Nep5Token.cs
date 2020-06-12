using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract.Native.Tokens;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_Nep5Token : TestKit
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        protected const byte Prefix_TotalSupply = 11;
        private static readonly TestNep5Token test = new TestNep5Token();

        [TestMethod]
        public void TestTotalSupply()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            StorageItem item = new StorageItem
            {
                Value = new byte[] { 0x01 }
            };
            var key = CreateStorageKey(Prefix_TotalSupply);

            key.Id = test.Id;

            snapshot.Storages.Add(key, item);
            test.TotalSupply(snapshot).Should().Be(1);
        }

        [TestMethod]
        public void TestTotalSupplyDecimal()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            BigInteger totalSupply = 100_000_000;
            totalSupply *= test.Factor;

            StorageItem item = new StorageItem
            {
                Value = totalSupply.ToByteArrayStandard()
            };
            var key = CreateStorageKey(Prefix_TotalSupply);

            key.Id = test.Id;

            snapshot.Storages.Add(key, item);

            test.TotalSupply(snapshot).Should().Be(10_000_000_000_000_000);
        }

        public StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                Id = 0,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }
    }

    public class TestNep5Token : Nep5Token<NeoToken.NeoAccountState>
    {
        public override int Id => 0x10000005;

        public override string Name => "testNep5Token";

        public override string Symbol => throw new NotImplementedException();

        public override byte Decimals => 8;
    }
}
