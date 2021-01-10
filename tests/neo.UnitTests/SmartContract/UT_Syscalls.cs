using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using Array = System.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_Syscalls
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void System_Blockchain_GetBlock()
        {
            var tx = new Transaction()
            {
                Script = new byte[] { 0x01 },
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = Array.Empty<Signer>(),
                NetworkFee = 0x02,
                SystemFee = 0x03,
                Nonce = 0x04,
                ValidUntilBlock = 0x05,
                Version = 0x06,
                Witnesses = new Witness[] { new Witness() { VerificationScript = new byte[] { 0x07 } } },
            };

            var block = new Block()
            {
                Index = 0,
                Timestamp = 2,
                Version = 3,
                Witness = new Witness()
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                },
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                ConsensusData = new ConsensusData() { Nonce = 1, PrimaryIndex = 1 },
                Transactions = new Transaction[] { tx }
            };

            var snapshot = Blockchain.Singleton.GetSnapshot();

            using (var script = new ScriptBuilder())
            {
                script.EmitPush(block.Hash.ToArray());
                script.EmitSysCall(ApplicationEngine.System_Blockchain_GetBlock);

                // Without block

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // Not traceable block

                var height = snapshot.BlockHashIndex.GetAndChange();
                height.Index = block.Index + ProtocolSettings.Default.MaxTraceableBlocks;

                var blocks = snapshot.Blocks;
                var txs = snapshot.Transactions;
                blocks.Add(block.Hash, block.Trim());
                txs.Add(tx.Hash, new TransactionState() { Transaction = tx, BlockIndex = block.Index, VMState = VMState.HALT });

                engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With block

                height.Index = block.Index;

                engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                var array = engine.ResultStack.Pop<VM.Types.Array>();
                Assert.AreEqual(block.Hash, new UInt256(array[0].GetSpan()));

                // Clean

                blocks.Delete(block.Hash);
                txs.Delete(tx.Hash);
            }
        }

        [TestMethod]
        public void Json_Deserialize()
        {
            // Good

            using (var script = new ScriptBuilder())
            {
                script.EmitPush("123");
                script.EmitSysCall(ApplicationEngine.System_Json_Deserialize);
                script.EmitPush("null");
                script.EmitSysCall(ApplicationEngine.System_Json_Deserialize);

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, null))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.HALT);
                    Assert.AreEqual(2, engine.ResultStack.Count);

                    engine.ResultStack.Pop<Null>();
                    Assert.IsTrue(engine.ResultStack.Pop().GetInteger() == 123);
                }
            }

            // Error 1 - Wrong Json

            using (var script = new ScriptBuilder())
            {
                script.EmitPush("***");
                script.EmitSysCall(ApplicationEngine.System_Json_Deserialize);

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, null))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.FAULT);
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
            }

            // Error 2 - No decimals

            using (var script = new ScriptBuilder())
            {
                script.EmitPush("123.45");
                script.EmitSysCall(ApplicationEngine.System_Json_Deserialize);

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, null))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.FAULT);
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
            }
        }

        [TestMethod]
        public void Json_Serialize()
        {
            // Good

            using (var script = new ScriptBuilder())
            {
                script.EmitPush(5);
                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);
                script.Emit(OpCode.PUSH0);
                script.Emit(OpCode.NOT);
                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);
                script.EmitPush("test");
                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);
                script.Emit(OpCode.PUSHNULL);
                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);
                script.Emit(OpCode.NEWMAP);
                script.Emit(OpCode.DUP);
                script.EmitPush("key");
                script.EmitPush("value");
                script.Emit(OpCode.SETITEM);
                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, null))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.HALT);
                    Assert.AreEqual(5, engine.ResultStack.Count);

                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "{\"key\":\"value\"}");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "null");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "\"test\"");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "true");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "5");
                }
            }

            // Error

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Storage_GetContext);
                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, null))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.FAULT);
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
            }
        }

        [TestMethod]
        public void System_ExecutionEngine_GetScriptContainer()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetScriptContainer);

                // Without tx

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With tx

                var tx = new Transaction()
                {
                    Script = new byte[] { 0x01 },
                    Signers = new Signer[] { new Signer() { Account = UInt160.Zero, Scopes = WitnessScope.None } },
                    Attributes = Array.Empty<TransactionAttribute>(),
                    NetworkFee = 0x02,
                    SystemFee = 0x03,
                    Nonce = 0x04,
                    ValidUntilBlock = 0x05,
                    Version = 0x06,
                    Witnesses = new Witness[] { new Witness() { VerificationScript = new byte[] { 0x07 } } },
                };

                engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                var array = engine.ResultStack.Pop<VM.Types.Array>();
                Assert.AreEqual(tx.Hash, new UInt256(array[0].GetSpan()));
            }
        }

        [TestMethod]
        public void System_Runtime_GasLeft()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            using (var script = new ScriptBuilder())
            {
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);

                // Execute

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, 100_000_000);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(engine.Execute(), VMState.HALT);

                // Check the results

                CollectionAssert.AreEqual
                    (
                    engine.ResultStack.Select(u => (int)u.GetInteger()).ToArray(),
                    new int[] { 99_999_490, 99_998_980, 99_998_410 }
                    );
            }

            // Check test mode

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);

                // Execute

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
                engine.LoadScript(script.ToArray());

                // Check the results

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(Integer));
                Assert.AreEqual(1999999520, engine.ResultStack.Pop().GetInteger());
            }
        }

        [TestMethod]
        public void System_Runtime_GetInvocationCounter()
        {
            ContractState contractA, contractB, contractC;
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();

            // Create dummy contracts

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetInvocationCounter);

                contractA = new ContractState() { Nef = new NefFile { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP }.Concat(script.ToArray()).ToArray() } };
                contractB = new ContractState() { Nef = new NefFile { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() } };
                contractC = new ContractState() { Nef = new NefFile { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() } };
                contractA.Hash = contractA.Script.ToScriptHash();
                contractB.Hash = contractB.Script.ToScriptHash();
                contractC.Hash = contractC.Script.ToScriptHash();

                // Init A,B,C contracts
                // First two drops is for drop method and arguments

                snapshot.DeleteContract(contractA.Hash);
                snapshot.DeleteContract(contractB.Hash);
                snapshot.DeleteContract(contractC.Hash);
                contractA.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.Integer, ContractParameterType.Integer);
                contractB.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.Integer, ContractParameterType.Integer);
                contractC.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.Integer, ContractParameterType.Integer);
                snapshot.AddContract(contractA.Hash, contractA);
                snapshot.AddContract(contractB.Hash, contractB);
                snapshot.AddContract(contractC.Hash, contractC);
            }

            // Call A,B,B,C

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(contractA.Hash, "dummyMain", true, 0, 1);
                script.EmitDynamicCall(contractB.Hash, "dummyMain", true, 0, 1);
                script.EmitDynamicCall(contractB.Hash, "dummyMain", true, 0, 1);
                script.EmitDynamicCall(contractC.Hash, "dummyMain", true, 0, 1);

                // Execute

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());

                // Check the results

                CollectionAssert.AreEqual
                    (
                    engine.ResultStack.Select(u => (int)u.GetInteger()).ToArray(),
                    new int[]
                        {
                        1, /* A */
                        1, /* B */
                        2, /* B */
                        1  /* C */
                        }
                    );
            }
        }
    }
}
