using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NeoToken
    {
        private Store Store;

        internal class CheckWitness : IScriptContainer, IVerifiable
        {
            private readonly UInt160[] _hashForVerify;

            public Witness[] Witnesses => throw new NotImplementedException();

            public int Size => 0;

            public CheckWitness(UInt160[] hashForVerify)
            {
                _hashForVerify = hashForVerify;
            }

            public void Deserialize(BinaryReader reader) { }

            public void DeserializeUnsigned(BinaryReader reader) { }

            public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
            {
                return _hashForVerify;
            }

            public void Serialize(BinaryWriter writer) { }

            public void SerializeUnsigned(BinaryWriter writer) { }

            byte[] IScriptContainer.GetMessage() => new byte[0];

        }

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
        public void Check_Symbol()
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
        public void Check_Decimals()
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
        public void Check_SupportedStandards()
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
        public void Check_UnclaimedGas()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            Check_Initialize(snapshot, from);

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(800000000000));
            unclaim.State.Should().BeTrue();

            unclaim = Check_UnclaimedGas(snapshot, new byte[19]);
            unclaim.Value.Should().Be(BigInteger.Zero);
            unclaim.State.Should().BeFalse();
        }

        [TestMethod]
        public void Check_RegisterValidator()
        {
            var snapshot = Store.GetSnapshot().Clone();

            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();
            Check_Initialize(snapshot, account);

            var ret = Check_RegisterValidator(snapshot, new byte[0]);
            ret.State.Should().BeFalse();
            ret.Result.Should().BeFalse();

            ret = Check_RegisterValidator(snapshot, new byte[33]);
            ret.State.Should().BeFalse();
            ret.Result.Should().BeFalse();

            var keyCount = snapshot.Storages.GetChangeSet().Count();
            var point = Blockchain.StandbyValidators[0].EncodePoint(true);

            ret = Check_RegisterValidator(snapshot, point); // Exists
            ret.State.Should().BeTrue();
            ret.Result.Should().BeFalse();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount); // No changes

            point[20]++; // fake point
            ret = Check_RegisterValidator(snapshot, point); // New

            ret.State.Should().BeTrue();
            ret.Result.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 1); // New validator
        }

        [TestMethod]
        public void Check_Transfer()
        {
            var snapshot = Store.GetSnapshot().Clone();
            snapshot.PersistingBlock = new Block() { Index = 1000 };

            byte[] from = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            byte[] to = new byte[20];

            Check_Initialize(snapshot, from);

            var keyCount = snapshot.Storages.GetChangeSet().Count();

            // Check unclaim

            var unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(800000000000));
            unclaim.State.Should().BeTrue();

            // Transfer

            Check_Transfer(snapshot, from, to, BigInteger.One, false).Should().BeFalse(); // Not signed
            Check_Transfer(snapshot, from, to, BigInteger.One, true).Should().BeTrue();
            Check_BalanceOf(snapshot, from).Should().Be(99_999_999);
            Check_BalanceOf(snapshot, to).Should().Be(1);

            // Check unclaim

            unclaim = Check_UnclaimedGas(snapshot, from);
            unclaim.Value.Should().Be(new BigInteger(0));
            unclaim.State.Should().BeTrue();

            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount + 2); // Gas + new balance

            // Return balance

            keyCount = snapshot.Storages.GetChangeSet().Count();

            Check_Transfer(snapshot, to, from, BigInteger.One, true).Should().BeTrue();
            Check_BalanceOf(snapshot, to).Should().Be(0);
            snapshot.Storages.GetChangeSet().Count().Should().Be(keyCount - 1);  // Remove neo balance from address two

            // Bad inputs

            Check_Transfer(snapshot, from, to, BigInteger.MinusOne, true).Should().BeFalse();
            Check_Transfer(snapshot, new byte[19], to, BigInteger.One, false).Should().BeFalse();
            Check_Transfer(snapshot, from, new byte[19], BigInteger.One, false).Should().BeFalse();

            // More than balance

            Check_Transfer(snapshot, to, from, new BigInteger(2), true).Should().BeFalse();
        }

        [TestMethod]
        public void Check_BalanceOf()
        {
            var snapshot = Store.GetSnapshot().Clone();
            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();

            Check_Initialize(snapshot, account);
            Check_BalanceOf(snapshot, account).Should().Be(100_000_000);

            account[5]++; // Without existing balance

            Check_BalanceOf(snapshot, account).Should().Be(0);
        }

        [TestMethod]
        public void Check_Initialize()
        {
            var snapshot = Store.GetSnapshot().Clone();
            byte[] account = Contract.CreateMultiSigRedeemScript(Blockchain.StandbyValidators.Length / 2 + 1,
                Blockchain.StandbyValidators).ToScriptHash().ToArray();
            Check_Initialize(snapshot, account);
        }

        [TestMethod]
        public void Check_BadScript()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            typeof(NeoToken).GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(NativeContractBase.NEO, new object[] { engine }).Should().Be(false);
        }

        public static byte[] NativeContract(string contract)
        {
            var scriptSyscall = new ScriptBuilder();
            scriptSyscall.EmitSysCall(contract);
            return scriptSyscall.ToArray();
        }

        internal static (bool State, bool Result) Check_RegisterValidator(Snapshot snapshot, byte[] pubkey)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(pubkey);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("registerValidator");
            engine.LoadScript(script.ToArray());

            engine.Execute();

            if (engine.State == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return (true, (result as VM.Types.Boolean).GetBoolean());
        }

        internal static ECPoint[] Check_GetValidators(Snapshot snapshot)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("getValidators");
            engine.LoadScript(script.ToArray());

            engine.Execute();

            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));

            return (result as VM.Types.Array).Select(u => u.GetByteArray().AsSerializable<ECPoint>()).ToArray();
        }

        internal static (BigInteger Value, bool State) Check_UnclaimedGas(Snapshot snapshot, byte[] address)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(snapshot.PersistingBlock.Index);
            script.EmitPush(address);
            script.EmitPush(2);
            script.Emit(OpCode.PACK);
            script.EmitPush("unclaimedGas");
            engine.LoadScript(script.ToArray());

            engine.Execute();

            if (engine.State == VMState.FAULT)
            {
                return (BigInteger.Zero, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return ((result as VM.Types.Integer).GetBigInteger(), true);
        }

        internal static bool Check_Transfer(Snapshot snapshot, byte[] from, byte[] to, BigInteger amount, bool signFrom)
        {
            var engine = new ApplicationEngine(TriggerType.Application,
                new CheckWitness(signFrom ? new UInt160[] { new UInt160(from) } : null), snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

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

        internal static void Check_Initialize(Snapshot snapshot, byte[] account)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            var script = new ScriptBuilder();
            script.EmitPush(account);
            script.EmitPush(1);
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

            foreach (var st in storages) st.Key.ScriptHash.Should().Be(NativeContractBase.NEO.ScriptHash);

            // First key, the flag

            storages[0].Item.Value.Should().BeEquivalentTo(new byte[] { 0x01 });
            storages[0].Key.Key.Should().BeEquivalentTo(new byte[] { 11 });
            storages[0].Item.IsConstant.Should().Be(true);

            // Balance

            CheckBalance(account, storages[1], 100_000_000, 0, new ECPoint[] { });

            // StandbyValidators

            var validators = Check_GetValidators(snapshot);

            for (var x = 0; x < Blockchain.StandbyValidators.Length; x++)
            {
                CheckValidator(Blockchain.StandbyValidators[x], storages[x + 2]);
                validators[x].Equals(Blockchain.StandbyValidators[x]);
            }

            // Check double call

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

            script = new ScriptBuilder();
            script.EmitPush(account);
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush("initialize");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            (result as VM.Types.Boolean).GetBoolean().Should().Be(false);
        }

        internal static void CheckValidator(ECPoint eCPoint, DataCache<StorageKey, StorageItem>.Trackable trackable)
        {
            var st = new BigInteger(trackable.Item.Value);
            st.Should().Be(0);

            trackable.Key.Key.Should().BeEquivalentTo(new byte[] { 33 }.Concat(eCPoint.EncodePoint(true)));
            trackable.Item.IsConstant.Should().Be(false);
        }

        internal static void CheckBalance(byte[] account, DataCache<StorageKey, StorageItem>.Trackable trackable, BigInteger balance, BigInteger height, ECPoint[] votes)
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

        internal static BigInteger Check_BalanceOf(Snapshot snapshot, byte[] account)
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, Fixed8.Zero, true);

            engine.LoadScript(NativeContract("Neo.Native.Tokens.NEO"));

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