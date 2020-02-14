using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Oracle;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.UnitTests.Extensions;
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
        public void Test_GetTimeOutMilliSeconds()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.OraclePolicy.Hash, "getTimeOutMilliSeconds");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, 1000);
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
            byte[] privateKey1 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair1 = new KeyPair(privateKey1);
            ECPoint pubkey1 = keyPair1.PublicKey;

            byte[] privateKey2 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair2 = new KeyPair(privateKey2);
            ECPoint pubkey2 = keyPair2.PublicKey;

            ECPoint[] pubkeys = new ECPoint[2];
            pubkeys[0] = pubkey1;
            pubkeys[1] = pubkey2;

            var snapshot = Blockchain.Singleton.GetSnapshot();

            using ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(NativeContract.OraclePolicy.Hash, "delegateOracleValidator", new ContractParameter
            {
                Type = ContractParameterType.Hash160,
                Value = Contract.CreateSignatureRedeemScript(pubkey1).ToScriptHash()
            }, new ContractParameter
            {
                Type = ContractParameterType.Array,
                Value = pubkeys.Select(p => new ContractParameter
                {
                    Type = ContractParameterType.PublicKey,
                    Value = p
                }).ToArray()
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

            var engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, 0, true);
            engine.Execute().Should().Be(VMState.HALT);
        }
    }
}
