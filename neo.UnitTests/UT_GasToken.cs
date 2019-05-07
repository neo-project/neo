using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

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
        public void Check_Name()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("name");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteArray));
            Encoding.ASCII.GetString((result as VM.Types.ByteArray).GetByteArray()).Should().Be("GAS");
        }

        [TestMethod]
        public void Check_Symbol()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("symbol");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteArray));
            Encoding.ASCII.GetString((result as VM.Types.ByteArray).GetByteArray()).Should().Be("gas");
        }

        [TestMethod]
        public void Check_Decimals()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("decimals");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            (result as VM.Types.Integer).GetBigInteger().Should().Be(8);
        }

        [TestMethod]
        public void Check_SupportedStandards()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("supportedStandards");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            (result as VM.Types.Array).ToArray()
                .Select(u => Encoding.ASCII.GetString(u.GetByteArray()))
                .ToArray()
                .Should().BeEquivalentTo(new string[] { "NEP-5", "NEP-10" });
        }

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

            // Check unclaim

            var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(800000000000));
            unclaim.State.Should().BeTrue();

            // Transfer

            UT_NeoToken.Check_Transfer(snapshot, from, to, BigInteger.Zero, true).Should().BeTrue();
            UT_NeoToken.Check_BalanceOf(snapshot, from).Should().Be(100_000_000);
            UT_NeoToken.Check_BalanceOf(snapshot, to).Should().Be(0);

            Check_BalanceOf(snapshot, from).Should().Be(800000000000);
            Check_BalanceOf(snapshot, to).Should().Be(0);

            // Check unclaim

            unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 1); // Gas

            // Transfer

            keyCount = snapshot.Storages.GetChangeSet().Count();

            Check_Transfer(snapshot, from, to, 800000000000, false).Should().BeFalse(); // Not signed
            Check_Transfer(snapshot, from, to, 800000000001, true).Should().BeFalse(); // More than balance
            Check_Transfer(snapshot, from, to, 800000000000, true).Should().BeTrue(); // All balance

            // Balance of

            Check_BalanceOf(snapshot, to).Should().Be(800000000000);
            Check_BalanceOf(snapshot, from).Should().Be(0);

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount); // All

            // Bad inputs

            Check_Transfer(snapshot, from, to, BigInteger.MinusOne, true).Should().BeFalse();
            Check_Transfer(snapshot, new byte[19], to, BigInteger.One, false).Should().BeFalse();
            Check_Transfer(snapshot, from, new byte[19], BigInteger.One, false).Should().BeFalse();
        }

        [TestMethod]
        public void Check_BadScript()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            typeof(GasToken).GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(NativeContractBase.GAS, new object[] { engine }).Should().Be(false);
        }

        internal static bool Check_Transfer(Snapshot snapshot, byte[] from, byte[] to, BigInteger amount, bool signFrom)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new UT_NeoToken.CheckWitness(signFrom ? new UInt160[] { new UInt160(from) } : null), snapshot, Fixed8.Zero, true);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(amount);
            script.EmitPush(to);
            script.EmitPush(from);
            script.EmitPush(3);
            script.Emit(OpCode.PACK);
            script.EmitPush("transfer");
            engine.LoadScript(script.ToArray());

            engine.Execute();

            if (engine.State == VMState.FAULT)
            {
                return false;
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (result as VM.Types.Boolean).GetBoolean();
        }

        internal static BigInteger Check_BalanceOf(Snapshot snapshot, byte[] account)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(account);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("balanceOf");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return (result as VM.Types.Integer).GetBigInteger();
        }
    }
}