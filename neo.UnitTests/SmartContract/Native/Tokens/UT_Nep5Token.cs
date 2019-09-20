using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_Nep5Token
    {
        protected const byte Prefix_TotalSupply = 11;

        [TestMethod]
        public void TestTotalSupply()
        {
            var mockSnapshot = new Mock<Snapshot>();
            var myDataCache = new TestDataCache<StorageKey, StorageItem>();
            StorageItem item = new StorageItem
            {
                Value = new byte[] { 0x01 }
            };
            var key = CreateStorageKey(Prefix_TotalSupply);

            var ServiceHash = "test".ToInteropMethodHash();
            byte[] script = null;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ServiceHash);
                script = sb.ToArray();
            }
            var Hash = script.ToScriptHash();
            key.ScriptHash = Hash;

            myDataCache.Add(key, item);
            mockSnapshot.SetupGet(p => p.Storages).Returns(myDataCache);
            TestNep5Token test = new TestNep5Token();
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            StackItem stackItem = test.TotalSupply(ae, null);
            stackItem.GetBigInteger().Should().Be(1);
        }

        [TestMethod]
        public void TestTotalSupplyDecimal()
        {
            var mockSnapshot = new Mock<Snapshot>();
            var myDataCache = new TestDataCache<StorageKey, StorageItem>();

            TestNep5Token test = new TestNep5Token();
            BigInteger totalSupply = 100_000_000;
            totalSupply *= test.Factor;

            byte[] value = totalSupply.ToByteArray();
            StorageItem item = new StorageItem
            {
                Value = value
            };
            var key = CreateStorageKey(Prefix_TotalSupply);

            var ServiceHash = "test".ToInteropMethodHash();
            byte[] script = null;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitSysCall(ServiceHash);
                script = sb.ToArray();
            }
            var Hash = script.ToScriptHash();
            key.ScriptHash = Hash;

            myDataCache.Add(key, item);
            mockSnapshot.SetupGet(p => p.Storages).Returns(myDataCache);

            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0);
            StackItem stackItem = test.TotalSupply(ae, null);
            stackItem.GetBigInteger().Should().Be(10_000_000_000_000_000);
        }

        public StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = null,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            if (key != null)
                Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);
            return storageKey;
        }
    }

    public class TestNep5Token : Nep5Token<NeoToken.AccountState>
    {
        public override string Name => throw new NotImplementedException();

        public override string Symbol => throw new NotImplementedException();

        public override byte Decimals => 8;

        public override string ServiceName => "test";

        public new StackItem TotalSupply(ApplicationEngine engine, VM.Types.Array args)
        {
            return base.TotalSupply(engine, args);
        }
    }
}
