using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static Neo.UnitTests.Extensions.Nep5NativeContractExtensions;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_OracleContract
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
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
            script.EmitAppCall(NativeContract.Oracle.Hash, "getRequestBaseFee");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, 0);
        }

        [TestMethod]
        public void Test_SetPerRequestFee()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Init
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            var from = NativeContract.NEO.GetCommitteeAddress(engine.Snapshot);
            long value = 12345;

            // Set
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setRequestBaseFee", value);
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).GetBoolean());

            // Set (wrong witness)
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setRequestBaseFee", value);
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(null), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            // Set wrong (negative)
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setRequestBaseFee", -1);
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            // Get
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getRequestBaseFee");
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, value);
        }

        [TestMethod]
        public void Test_GetValidHeight()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getRequestMaxValidHeight");
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, 0);
        }

        [TestMethod]
        public void Test_SetValidHeight()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Init
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            var from = NativeContract.NEO.GetCommitteeAddress(snapshot);
            uint value = 123;

            // Set 
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setRequestMaxValidHeight", value);
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsTrue((result as VM.Types.Boolean).GetBoolean());

            // Set (wrong witness)
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setRequestMaxValidHeight", value);
            engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(null), snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            Assert.IsFalse((result as VM.Types.Boolean).GetBoolean());

            // Get
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getRequestMaxValidHeight");
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            Assert.AreEqual(result, value);
        }

        [TestMethod]
        public void Test_GetOracleValidators()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            var from = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Neo.Cryptography.ECC.ECPoint[] defaultNodes = NativeContract.NEO.GetCommittee(snapshot);
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setOracleValidators", defaultNodes.ToByteArray());
            var engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.Execute();
            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "getOracleValidators");
            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);
            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            Assert.AreEqual(21, ((VM.Types.Array)result).Count);
        }

        [TestMethod]
        public void Check_Request()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var request = new OracleRequest()
            {
                Url = "https://www.baidu.com/",
                FilterPath = "dotest",
                OracleFee = 10000
            };
            var ret_Request = Check_Request(snapshot, request, out UInt256 requestTxHash, out Transaction tx);
            ret_Request.Result.GetBoolean().Should().BeTrue();
            ret_Request.State.Should().BeTrue();
        }

        internal static (bool State, StackItem Result) Check_Request(StoreView snapshot, OracleRequest request, out UInt256 requestTxHash, out Transaction tx)
        {
            var from = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Neo.Cryptography.ECC.ECPoint[] defaultNodes = NativeContract.NEO.GetCommittee(snapshot);
            var script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash, "setOracleValidators", defaultNodes.ToByteArray());
            var engine = new ApplicationEngine(TriggerType.Application, new ManualWitness(from), snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.Execute();

            snapshot.PersistingBlock = new Block() { Index = 1000 };
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair keyPair = new KeyPair(privateKey);
            UInt160 account = Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash();

            script = new ScriptBuilder();
            script.EmitAppCall(NativeContract.Oracle.Hash,
                "request",
                request.Url,
                request.FilterPath,
                "back",
                request.OracleFee);
            script.Emit(OpCode.RET);
            script.Emit(OpCode.DROP);
            script.Emit(OpCode.RET);
            ContractManifest manifest = new ContractManifest()
            {
                Permissions = new[] { ContractPermission.DefaultPermission },
                Abi = new ContractAbi()
                {
                    Hash = script.ToArray().ToScriptHash(),
                    Events = new ContractEventDescriptor[0],
                    Methods = new ContractMethodDescriptor[0]
                },
                Features = ContractFeatures.NoProperty,
                Groups = new ContractGroup[0],
                SafeMethods = WildcardContainer<string>.Create(),
                Trusts = WildcardContainer<UInt160>.Create(),
                Extra = null,
            };
            manifest.Abi.Methods = new ContractMethodDescriptor[]
            {
                new ContractMethodDescriptor()
                {
                    Name = "testInvoke",
                    Parameters = new ContractParameterDefinition[0],
                    ReturnType = ContractParameterType.Void,
                    Offset=0x00
                },
                new ContractMethodDescriptor()
                {
                    Name = "back",
                    Parameters =new ContractParameterDefinition[]{
                        new ContractParameterDefinition(){
                            Name="data",
                            Type=ContractParameterType.ByteArray
                        }
                    },
                    ReturnType = ContractParameterType.Void,
                    Offset=script.ToArray().Length-2
                }
            };
            ContractState contractState = new ContractState
            {
                Id = 0x43000000,
                Script = script.ToArray(),
                Manifest = manifest
            };
            snapshot.Contracts.Add(contractState.ScriptHash, contractState);

            ScriptBuilder builder = new ScriptBuilder();
            builder.EmitAppCall(contractState.ScriptHash, "testInvoke");
            tx = new Transaction
            {
                Version = 0,
                Nonce = (uint)1000,
                Script = builder.ToArray(),
                Sender = account,
                ValidUntilBlock = snapshot.Height + Transaction.MaxValidUntilBlockIncrement,
                Attributes = new TransactionAttribute[0],
                Witnesses = new Witness[] { new Witness
            {
                InvocationScript = System.Array.Empty<byte>(),
                VerificationScript = Contract.CreateSignatureRedeemScript(keyPair.PublicKey)
            }}
            };
            var data = new ContractParametersContext(tx);
            byte[] sig = data.Verifiable.Sign(keyPair);
            tx.Witnesses[0].InvocationScript = sig;
            requestTxHash = tx.Hash;
            engine = ApplicationEngine.Run(builder.ToArray(), snapshot, tx, null, 0, true);
            if (engine.State == VMState.FAULT)
            {
                return (false, false);
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));
            return (true, result);
        }

        [TestMethod]
        public void Check_CallBack()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var request = new OracleRequest()
            {
                Url = "https://www.baidu.com/",
                FilterPath = "dotest",
                CallbackMethod = "back",
                OracleFee = 1000L
            };
            var ret_Request = Check_Request(snapshot, request, out UInt256 requestTxHash, out Transaction tx);
            ret_Request.Result.GetBoolean().Should().Be(true);
            ret_Request.State.Should().BeTrue();
            snapshot.Transactions.Add(tx.Hash, new TransactionState() { Transaction = tx, VMState = VMState.HALT, BlockIndex = snapshot.PersistingBlock.Index });

            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair keyPair = new KeyPair(privateKey);

            OracleResponseAttribute response = new OracleResponseAttribute();
            response.RequestTxHash = requestTxHash;
            response.Data = keyPair.PublicKey.ToArray();
            response.FilterCost = 0;
            Transaction responsetx = CreateResponseTransaction(snapshot, response);
            Console.WriteLine(responsetx.SystemFee);
        }

        private static Transaction CreateResponseTransaction(StoreView initsnapshot, OracleResponseAttribute response)
        {
            StoreView snapshot = initsnapshot.Clone();

            var oracleAddress = NativeContract.Oracle.GetOracleMultiSigAddress(snapshot);
            ScriptBuilder sb = new ScriptBuilder();
            sb.EmitAppCall(NativeContract.Oracle.Hash, "onPersist");

            var tx = new Transaction()
            {
                Version = 0,
                ValidUntilBlock = snapshot.Height + Transaction.MaxValidUntilBlockIncrement,
                Attributes = new TransactionAttribute[]{
                    new Cosigner()
                    {
                        Account = oracleAddress,
                        AllowedContracts = new UInt160[]{ NativeContract.Oracle.Hash },
                        Scopes = WitnessScope.CustomContracts
                    },
                    response
                },
                Sender = oracleAddress,
                Witnesses = new Witness[0],
                Script = sb.ToArray(),
                NetworkFee = 0,
                Nonce = 0,
                SystemFee = 0
            };

            snapshot.PersistingBlock = new Block() { Index = snapshot.Height + 1, Transactions = new Transaction[] { tx } };
            //commit response
            var engine = new ApplicationEngine(TriggerType.System, null, snapshot, 0, true);
            engine.LoadScript(sb.ToArray());
            if (engine.Execute() != VMState.HALT) throw new InvalidOperationException();

            var sb2 = new ScriptBuilder();
            sb2.EmitAppCall(NativeContract.Oracle.Hash, "callback");

            var state = new TransactionState
            {
                BlockIndex = snapshot.PersistingBlock.Index,
                Transaction = tx
            };
            snapshot.Transactions.Add(tx.Hash, state);

            var engine2 = ApplicationEngine.Run(sb2.ToArray(), snapshot, tx, testMode: true);
            if (engine2.State != VMState.HALT) throw new ApplicationException();
            tx.SystemFee = engine2.GasConsumed;
            // Calculate network fee
            int size = tx.Size;
            var oracleValidators = NativeContract.Oracle.GetOracleValidators(snapshot);
            var oracleMultiContract = Contract.CreateMultiSigContract(oracleValidators.Length - (oracleValidators.Length - 1) / 3, oracleValidators);
            tx.NetworkFee += Wallet.CalculateNetworkFee(oracleMultiContract.Script, ref size);
            tx.NetworkFee += size * NativeContract.Policy.GetFeePerByte(snapshot);
            return tx;
        }

    }
}
