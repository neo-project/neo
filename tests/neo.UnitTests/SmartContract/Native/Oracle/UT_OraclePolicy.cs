using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.UnitTests.Wallets;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using static Neo.UnitTests.Extensions.Nep5NativeContractExtensions;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OraclePolicy
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestInitialize()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = new ApplicationEngine(TriggerType.System, null, snapshot, 0, true);

            snapshot.Storages.Delete(CreateStorageKey(11));
            snapshot.PersistingBlock = Blockchain.GenesisBlock;
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            NativeContract.OraclePolicy.Initialize(engine).Should().BeTrue();
        }
        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                Id = NativeContract.OraclePolicy.Id,
                Key = new byte[sizeof(byte) + (key?.Length ?? 0)]
            };
            storageKey.Key[0] = prefix;
            key?.CopyTo(storageKey.Key.AsSpan(1));
            return storageKey;
        }

        [TestMethod]
        public void Test_GetPerRequestFee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "getPerRequestFee");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, 1000);
        }

        [TestMethod]
        public void Test_SetPerRequestFee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Init

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            NativeContract.OraclePolicy.Initialize(engine).Should().BeTrue();
            var from = NativeContract.OraclePolicy.GetOracleMultiSigAddress(snapshot);
            var value = 12345;

            // Set 
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "setPerRequestFee", new ContractParameter(ContractParameterType.Integer) { Value = value });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).ToBoolean());

            // Set (wrong witness)
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "setPerRequestFee", new ContractParameter(ContractParameterType.Integer) { Value = value });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(null), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).ToBoolean());

            // Set wrong (negative)

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "setPerRequestFee", new ContractParameter(ContractParameterType.Integer) { Value = -1 });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).ToBoolean());

            // Get

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "getPerRequestFee");
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, value);
        }

        [TestMethod]
        public void Test_GetHttpConfig()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "getHttpConfig");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)result)[0].GetBigInteger(), 5000);
        }

        [TestMethod]
        public void Test_SetHttpConfig()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Init

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            NativeContract.OraclePolicy.Initialize(engine).Should().BeTrue();
            var from = NativeContract.OraclePolicy.GetOracleMultiSigAddress(snapshot);
            var value = 12345;

            // Set wrong (negative)

            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "setHttpConfig", new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).ToBoolean());

            // Set (wrong witness)
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "setHttpConfig", new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(null), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).ToBoolean());

            // Set good

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "setHttpConfig", new ContractParameter(ContractParameterType.Integer) { Value = value });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).ToBoolean());

            // Get

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "getHttpConfig");
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)result)[0].GetBigInteger(), value);
        }

        [TestMethod]
        public void Test_GetOracleValidators()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "getOracleValidators");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)result).Count, 7);
        }

        [TestMethod]
        public void Test_GetOracleValidatorsCount()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "getOracleValidatorsCount");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, 7);
        }

        [TestMethod]
        public void Test_DelegateOracleValidator()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            ECPoint[] oraclePubKeys = PolicyContract.NEO.GetValidators(snapshot);

            ECPoint pubkey0 = oraclePubKeys[0];

            byte[] privateKey1 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair1 = new KeyPair(privateKey1);
            ECPoint pubkey1 = keyPair1.PublicKey;

            byte[] privateKey2 = { 0x02,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair2 = new KeyPair(privateKey2);
            ECPoint pubkey2 = keyPair2.PublicKey;

            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(NativeContract.OraclePolicy.Hash, "delegateOracleValidator", new ContractParameter
            {
                Type = ContractParameterType.ByteArray,
                Value = pubkey0.ToArray()
            }, new ContractParameter
            {
                Type = ContractParameterType.ByteArray,
                Value = pubkey1.ToArray()
            });

            MyWallet wallet = new MyWallet();
            WalletAccount account = wallet.CreateAccount(privateKey1);

            // Fake balance
            var key = NativeContract.GAS.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new Nep5AccountState().ToByteArray()
            });
            entry.Value = new Nep5AccountState()
            {
                Balance = 1000000 * NativeContract.GAS.Factor
            }
            .ToByteArray();
            snapshot.Commit();

            var tx = wallet.MakeTransaction(sb.ToArray(), account.ScriptHash, new TransactionAttribute[] { });
            ContractParametersContext context = new ContractParametersContext(tx);
            wallet.Sign(context);
            tx.Witnesses = context.GetWitnesses();

            // wrong witness
            var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, 0, true);
            engine.LoadScript(tx.Script);
            var state = engine.Execute();
            state.Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).ToBoolean());

            //wrong witness
            using ScriptBuilder sb2 = new ScriptBuilder();
            sb2.EmitAppCall(NativeContract.OraclePolicy.Hash, "delegateOracleValidator", new ContractParameter
            {
                Type = ContractParameterType.ByteArray,
                Value = pubkey1.ToArray()
            }, new ContractParameter
            {
                Type = ContractParameterType.ByteArray,
                Value = pubkey2.ToArray()
            });

            var from = Contract.CreateSignatureContract(pubkey1).ScriptHash;

            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(sb2.ToArray());
            state = engine.Execute();
            state.Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).ToBoolean());

            //correct
            from = Contract.CreateSignatureContract(pubkey0).ScriptHash;

            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(tx.Script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).ToBoolean());
            Test_GetOracleValidators();
        }
    }
}
