using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NeoToken
    {
        NeoSystem System;
        Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            System = TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        public byte[] NativeContract(string contract)
        {
            var scriptSyscall = new ScriptBuilder();
            scriptSyscall.EmitSysCall(contract);
            return scriptSyscall.ToArray();
        }

        [TestMethod]
        public void CheckScriptHash_Name()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("name");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteArray));
            Encoding.ASCII.GetString((result as VM.Types.ByteArray).GetByteArray()).Should().Be("NEO");
        }

        [TestMethod]
        public void CheckScriptHash_Symbol()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("symbol");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteArray));
            Encoding.ASCII.GetString((result as VM.Types.ByteArray).GetByteArray()).Should().Be("neo");
        }

        [TestMethod]
        public void CheckScriptHash_Decimals()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("decimals");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            (result as VM.Types.Integer).GetBigInteger().Should().Be(0);
        }

        [TestMethod]
        public void CheckScriptHash_SupportedStandards()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

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
        public void CheckScriptHash_Initialize()
        {
            var snapshot = Store.GetSnapshot();
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("initialize");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            (result as VM.Types.Boolean).GetBoolean().Should().Be(true);

            var storages = snapshot.Storages.GetChangeSet().ToArray();

            // Count

            storages.Length.Should().Be(Blockchain.StandbyValidators.Length + 2);

            // All hashes equal

            foreach (var st in storages) st.Key.ScriptHash.Equals(NeoToken.ScriptHash);

            // First key, the flag

            storages[0].Item.Value.Should().BeEquivalentTo(new byte[] { 0x01 });
            storages[0].Key.Key.Should().BeEquivalentTo(new byte[] { 11 });
            storages[0].Item.IsConstant.Should().Be(true);

            // Balance

            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            // Balance

            CheckBalance(account, storages[1], 100000000, 0, new ECPoint[] { });

            // StandbyValidators

            for (int x = 0; x < Blockchain.StandbyValidators.Length; x++)
            {
                CheckValidator(Blockchain.StandbyValidators[x], storages[x + 2]);
            }

            // Check double call

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("initialize");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            (result as VM.Types.Boolean).GetBoolean().Should().Be(false);
        }

        private void CheckValidator(ECPoint eCPoint, DataCache<StorageKey, StorageItem>.Trackable trackable)
        {
            var st = new BigInteger(trackable.Item.Value);
            st.Should().Be(0);

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 33 }.Concat(eCPoint.EncodePoint(true)));
            trackable.Item.IsConstant.Should().Be(false);
        }

        private void CheckBalance(byte[] account, IO.Caching.DataCache<StorageKey, StorageItem>.Trackable trackable, BigInteger balance, BigInteger height, ECPoint[] votes)
        {
            var st = (VM.Types.Struct)trackable.Item.Value.DeserializeStackItem(3);

            st.Count.Should().Be(3);
            st.Select(u => u.GetType()).ToArray().Should().BeEquivalentTo(new Type[] { typeof(VM.Types.Integer), typeof(VM.Types.Integer), typeof(VM.Types.ByteArray) }); // Balance

            st[0].GetBigInteger().Should().Be(balance); // Balance
            st[1].GetBigInteger().Should().Be(height);  // BalanceHeight
            (st[2].GetByteArray().AsSerializableArray<ECPoint>(Blockchain.MaxValidators)).Should().BeEquivalentTo(votes);  // Votes

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 20 }.Concat(account));
            trackable.Item.IsConstant.Should().Be(false);
        }

        [TestMethod]
        public void CheckScriptHash_BadScript()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            typeof(NeoToken).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { engine }).Should().Be(false);
        }
    }
}