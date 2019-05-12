using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Neo.UnitTests
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
        public void Check_BalanceOfAndTransfer()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            byte[] to = new byte[20];

            UT_NeoToken.Check_Initialize(snapshot, from);

            var keyCount = snapshot.Storages.GetChangeSet().Count();
            var supply = NativeContract.GAS.TotalSupply(snapshot);
            supply.Should().Be(0);

            // Check unclaim

            var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(800000000000));
            unclaim.State.Should().BeTrue();

            // Transfer

            NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true).Should().BeTrue();
            NativeContract.NEO.BalanceOf(snapshot, from).Should().Be(100_000_000);
            NativeContract.NEO.BalanceOf(snapshot, to).Should().Be(0);

            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(800000000000);
            NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(0);

            // Check unclaim

            unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            supply = NativeContract.GAS.TotalSupply(snapshot);
            supply.Should().Be(800000000000);

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 2); // Gas

            // Transfer

            keyCount = snapshot.Storages.GetChangeSet().Count();

            NativeContract.GAS.Transfer(snapshot, from, to, 800000000000, false).Should().BeFalse(); // Not signed
            NativeContract.GAS.Transfer(snapshot, from, to, 800000000001, true).Should().BeFalse(); // More than balance
            NativeContract.GAS.Transfer(snapshot, from, to, 800000000000, true).Should().BeTrue(); // All balance

            // Balance of

            NativeContract.GAS.BalanceOf(snapshot, to).Should().Be(800000000000);
            NativeContract.GAS.BalanceOf(snapshot, from).Should().Be(0);

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount); // All

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

            typeof(GasToken).GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(NativeContract.GAS, new object[] { engine }).Should().Be(false);
        }
    }
}