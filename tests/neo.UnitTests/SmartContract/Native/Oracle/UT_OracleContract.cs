using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Oracle.Protocols.Https;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Oracle;
using Neo.SmartContract.Native.Tokens;
using Neo.UnitTests.Wallets;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.Oracle
{
    [TestClass]
    public class UT_OracleContract
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

            Assert.ThrowsException<ArgumentException>(() => NativeContract.Oracle.Initialize(engine)); // already registered
        }
        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            StorageKey storageKey = new StorageKey
            {
                Id = NativeContract.Oracle.Id,
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
            script.EmitAppCall(NativeContract.Oracle.Hash, "getPerRequestFee");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, 1000);
        }

        [TestMethod]
        public void Neo_Oracle_Get()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Good

            using (var script = new ScriptBuilder())
            {
                script.EmitAppCall(NativeContract.Oracle.Hash, "get", "https://google.com", UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff").ToArray(), "MyFilter", "MyFilterArgs");

                using var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true, new OracleExecutionCache(Oracle));

                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                var response = engine.ResultStack.Pop<ByteString>();
                Assert.AreEqual("MyResponse", response.GetString());
            }

            // Wrong Filter

            using (var script = new ScriptBuilder())
            {
                script.EmitAppCall(NativeContract.Oracle.Hash, "get", "https://google.com", UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff").ToArray(), "WrongFilter", "MyFilterArgs");

                using var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true, new OracleExecutionCache(Oracle));

                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                var isNull = engine.ResultStack.Pop<Null>();
                Assert.IsTrue(isNull.IsNull);
            }

            // Wrong schema

            using (var script = new ScriptBuilder())
            {
                script.EmitAppCall(NativeContract.Oracle.Hash, "get", "http://google.com", null, null, null);

                using (var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true, new OracleExecutionCache(Oracle)))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.FAULT);
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
            }
        }

        private OracleResponse Oracle(OracleRequest arg)
        {
            if (arg is OracleHttpsRequest https)
            {
                if (https.Filter != null &&
                    (https.Filter.ContractHash != UInt160.Parse("0xffffffffffffffffffffffffffffffffffffffff") ||
                    https.Filter.FilterMethod != "MyFilter"))
                {
                    return OracleResponse.CreateError(UInt160.Zero);
                }

                if (https.URL.ToString() == "https://google.com/" && https.Method == HttpMethod.GET)
                {
                    return OracleResponse.CreateResult(UInt160.Zero, "MyResponse", 0);
                }
            }

            return OracleResponse.CreateError(UInt160.Zero);
        }

        [TestMethod]
        public void Test_SetPerRequestFee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Init

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            var from = NativeContract.Oracle.GetOracleMultiSigAddress(snapshot);
            var value = 12345;

            // Set 
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setPerRequestFee", new ContractParameter(ContractParameterType.Integer) { Value = value });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).GetBoolean());

            // Set (wrong witness)
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setPerRequestFee", new ContractParameter(ContractParameterType.Integer) { Value = value });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(null), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            // Set wrong (negative)

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setPerRequestFee", new ContractParameter(ContractParameterType.Integer) { Value = -1 });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            // Get

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getPerRequestFee");
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
            script.EmitAppCall(NativeContract.Oracle.Hash, "getConfig", new ContractParameter(ContractParameterType.String) { Value = HttpConfig.Key });
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));

            var cfg = new HttpConfig();
            cfg.FromStackItem(result);

            Assert.AreEqual(cfg.TimeOut, 5000);
        }

        [TestMethod]
        public void Test_SetConfig()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Init

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            var from = NativeContract.Oracle.GetOracleMultiSigAddress(snapshot);
            var key = HttpConfig.Key;
            var value = new HttpConfig() { TimeOut = 12345 };

            // Set (wrong witness)
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setConfig", new object[] { key, new object[] { value.TimeOut } });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(null), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            // Set good

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setConfig", new object[] { key, new object[] { value.TimeOut } });
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).GetBoolean());

            // Get

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getConfig", new object[] { key });
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            var array = (VM.Types.Array)result;
            Assert.AreEqual(array[0].GetInteger(), new BigInteger(value.TimeOut));
        }

        [TestMethod]
        public void Test_GetOracleValidators()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // Fake a oracle validator has cosignee.
            ECPoint[] oraclePubKeys = NativeContract.Oracle.GetOracleValidators(snapshot);

            ECPoint pubkey0 = oraclePubKeys[0]; // Validator0 is the cosignor
            ECPoint cosignorPubKey = oraclePubKeys[1]; // Validator1 is the cosignee
            var validator0Key = NativeContract.Oracle.CreateStorageKey(24, pubkey0); // 24 = Prefix_Validator
            var validator0Value = new StorageItem()
            {
                Value = cosignorPubKey.ToArray()
            };
            snapshot.Storages.Add(validator0Key, validator0Value);

            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getOracleValidators");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            Assert.AreEqual(6, ((VM.Types.Array)result).Count);

            // The validator0's cosignee should be the validator1
            var validators = (VM.Types.Array)result;
            var cosignee0Bytes = ((VM.Types.ByteString)validators[0]).GetSpan().ToHexString();
            var cosignee1Bytes = ((VM.Types.ByteString)validators[1]).GetSpan().ToHexString();
            Assert.AreNotEqual(cosignee0Bytes, cosignee1Bytes);
            var validator1Bytes = cosignorPubKey.ToArray().ToHexString();
            Assert.AreNotEqual(cosignee1Bytes, validator1Bytes);

            // clear data
            snapshot.Storages.Delete(validator0Key);
        }

        [TestMethod]
        public void Test_GetOracleValidatorsCount()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getOracleValidatorsCount");
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

            ECPoint[] oraclePubKeys = NativeContract.Oracle.GetOracleValidators(snapshot);

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
            sb.EmitAppCall(NativeContract.Oracle.Hash, "delegateOracleValidator", new ContractParameter
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
            var balance = new AccountState()
            {
                Balance = 1000000 * NativeContract.GAS.Factor
            };
            var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem(balance));
            snapshot.Commit();

            // Fake an nonexist validator in delegatedOracleValidators
            byte[] fakerPrivateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02};
            KeyPair fakerKeyPair = new KeyPair(fakerPrivateKey);
            ECPoint fakerPubkey = fakerKeyPair.PublicKey;
            var invalidOracleValidatorKey = NativeContract.Oracle.CreateStorageKey(24, fakerPubkey); // 24 = Prefix_Validator
            var invalidOracleValidatorValue = new StorageItem()
            {
                Value = fakerPubkey.ToArray()
            };
            snapshot.Storages.Add(invalidOracleValidatorKey, invalidOracleValidatorValue);

            var tx = wallet.MakeTransaction(sb.ToArray(), account.ScriptHash);
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
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            //wrong witness
            using ScriptBuilder sb2 = new ScriptBuilder();
            sb2.EmitAppCall(NativeContract.Oracle.Hash, "delegateOracleValidator", new ContractParameter
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
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            //correct
            from = Contract.CreateSignatureContract(pubkey0).ScriptHash;

            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(tx.Script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).GetBoolean());

            // The invalid oracle validator should be removed
            Assert.IsNull(snapshot.Storages.TryGet(invalidOracleValidatorKey));

            Test_GetOracleValidators();
        }
    }
}
