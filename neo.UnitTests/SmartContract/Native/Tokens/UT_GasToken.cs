using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native.Tokens
{
    [TestClass]
    public class UT_GasToken
    {
        Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void Check_Name() => NativeContract.GAS.Name().Should().Be("GAS");

        [TestMethod]
        public void Check_Symbol() => NativeContract.GAS.Symbol().Should().Be("gas");

        [TestMethod]
        public void Check_Decimals() => NativeContract.GAS.Decimals().Should().Be(8);

        [TestMethod]
        public void Check_SupportedStandards() => NativeContract.GAS.SupportedStandards().Should().BeEquivalentTo(new string[] { "NEP-5", "NEP-10" });

        [TestMethod]
        public void Check_BalanceOfTransferAndBurn()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            byte[] to = new byte[20];

            var keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.NEO.Initialize(new ApplicationEngine(TriggerType.Application, null, snapshot, 0));
            var supply = NativeContract.GAS.TotalSupply(snapshot);
            supply.Should().Be(3000000000000000);

            // Check unclaim

            var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(600000000000));
            unclaim.State.Should().BeTrue();

            // Transfer

            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true).Should().BeTrue();
            NativeContract.NEO.BalanceOf(snapshot, from).Should().Be(100_000_000);
            NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(0);

            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(3000600000000000);
            NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(0);

            // Check unclaim

            unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            supply = NativeContract.GAS.TotalSupply(snapshot);
            supply.Should().Be(3000600000000000);

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 3); // Gas

            // Transfer

            keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.GAS.Transfer(snapshot, from, to, 3000600000000000, false).Should().BeFalse(); // Not signed
            NativeContract.GAS.Transfer(snapshot, from, to, 3000600000000001, true).Should().BeFalse(); // More than balance
            NativeContract.GAS.Transfer(snapshot, from, to, 3000600000000000, true).Should().BeTrue(); // All balance

            // Balance of

            NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(3000600000000000);
            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(0);

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 1); // All

            // Burn

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0);
            keyCount = snapshot.Storages.GetChangeSet().Count();

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                NativeContract.GAS.Burn(engine, new UInt160(to), BigInteger.MinusOne));

            // Burn more than expected

            Assert.ThrowsException<InvalidOperationException>(() =>
                NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(3000600000000001)));

            // Real burn

            NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(1));

            NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(3000599999999999);

            keyCount.Should().Be(snapshot.Storages.GetChangeSet().Count());

            // Burn all

            NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(3000599999999999));

            (keyCount - 1).Should().Be(snapshot.Storages.GetChangeSet().Count());

            // Bad inputs

            NativeContract.GAS.Transfer(snapshot, from, to, BigInteger.MinusOne, true).Should().BeFalse();
            NativeContract.GAS.Transfer(snapshot, new byte[19], to, BigInteger.One, false).Should().BeFalse();
            NativeContract.GAS.Transfer(snapshot, from, new byte[19], BigInteger.One, false).Should().BeFalse();
        }

        [TestMethod]
        public void Check_BadScript()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), 0);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            NativeContract.GAS.Invoke(engine).Should().BeFalse();
        }

        [TestMethod]
        public void TestGetSysFeeAmount1()
        {
            using (ApplicationEngine engine = NativeContract.GAS.TestCall("getSysFeeAmount", 2u))
            {
                engine.ResultStack.Peek().GetBigInteger().Should().Be(new BigInteger(0));
                engine.ResultStack.Peek().GetType().Should().Be(typeof(Integer));
            }

            using (ApplicationEngine engine = NativeContract.GAS.TestCall("getSysFeeAmount", 0u))
            {
                engine.ResultStack.Peek().GetBigInteger().Should().Be(new BigInteger(0));
            }
        }

        [TestMethod]
        public void TestGetSysFeeAmount2()
        {
            var snapshot = Store.GetSnapshot().Clone();
            NativeContract.GAS.GetSysFeeAmount(snapshot, 0).Should().Be(new BigInteger(0));
            NativeContract.GAS.GetSysFeeAmount(snapshot, 1).Should().Be(new BigInteger(0));

            byte[] key = BitConverter.GetBytes(1);
            StorageKey storageKey = new StorageKey
            {
                ScriptHash = NativeContract.GAS.Hash,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = 15;
            Buffer.BlockCopy(key, 0, storageKey.Key, 1, key.Length);

            BigInteger sys_fee = new BigInteger(10);
            snapshot.Storages.Add(storageKey, new StorageItem
            {
                Value = sys_fee.ToByteArray(),
                IsConstant = true
            });

            NativeContract.GAS.GetSysFeeAmount(snapshot, 1).Should().Be(sys_fee);
        }
    }
}
