using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;
using Array = System.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Syscalls
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
                NetworkFee = 0x02,
                SystemFee = 0x03,
                Nonce = 0x04,
                ValidUntilBlock = 0x05,
                Version = 0x06,
                Witnesses = new Witness[] { new Witness() { VerificationScript = new byte[] { 0x07 } } },
                Sender = UInt160.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
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

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // Not traceable block

                var height = snapshot.BlockHashIndex.GetAndChange();
                height.Index = block.Index + Transaction.MaxValidUntilBlockIncrement;

                var blocks = snapshot.Blocks;
                var txs = snapshot.Transactions;
                blocks.Add(block.Hash, block.Trim());
                txs.Add(tx.Hash, new TransactionState() { Transaction = tx, BlockIndex = block.Index, VMState = VMState.HALT });

                engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With block

                height.Index = block.Index;

                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);
                engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(ByteString));
                Assert.AreEqual(engine.ResultStack.Pop().GetSpan().ToHexString(),
                    "5b2261564e62466b35384f51717547373870747154766561762f48677941566a72634e41434d4e59705c7530303242366f6f3d222c332c22414141414141414141414141414141414141414141414141414141414141414141414141414141414141413d222c22414141414141414141414141414141414141414141414141414141414141414141414141414141414141413d222c322c302c224141414141414141414141414141414141414141414141414141413d222c315d");
                Assert.AreEqual(0, engine.ResultStack.Count);

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

                using (var engine = new ApplicationEngine(TriggerType.Application, null, null, 0, true))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.HALT);
                    Assert.AreEqual(2, engine.ResultStack.Count);

                    Assert.IsTrue(engine.ResultStack.TryPop<Null>(out _));
                    Assert.IsTrue(engine.ResultStack.TryPop<Integer>(out var i) && i.GetBigInteger() == 123);
                }
            }

            // Error 1 - Wrong Json

            using (var script = new ScriptBuilder())
            {
                script.EmitPush("***");
                script.EmitSysCall(ApplicationEngine.System_Json_Deserialize);

                using (var engine = new ApplicationEngine(TriggerType.Application, null, null, 0, true))
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

                using (var engine = new ApplicationEngine(TriggerType.Application, null, null, 0, true))
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

                using (var engine = new ApplicationEngine(TriggerType.Application, null, null, 0, true))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.HALT);
                    Assert.AreEqual(5, engine.ResultStack.Count);

                    Assert.IsTrue(engine.ResultStack.TryPop<ByteString>(out var m) && m.GetString() == "{\"key\":\"dmFsdWU=\"}");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteString>(out var n) && n.GetString() == "null");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteString>(out var s) && s.GetString() == "\"dGVzdA==\"");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteString>(out var b) && b.GetString() == "true");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteString>(out var i) && i.GetString() == "5");
                }
            }

            // Error

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Storage_GetContext);
                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);

                using (var engine = new ApplicationEngine(TriggerType.Application, null, null, 0, true))
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

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With tx

                script.EmitSysCall(ApplicationEngine.System_Json_Serialize);

                var tx = new Transaction()
                {
                    Script = new byte[] { 0x01 },
                    Attributes = Array.Empty<TransactionAttribute>(),
                    NetworkFee = 0x02,
                    SystemFee = 0x03,
                    Nonce = 0x04,
                    ValidUntilBlock = 0x05,
                    Version = 0x06,
                    Witnesses = new Witness[] { new Witness() { VerificationScript = new byte[] { 0x07 } } },
                    Sender = UInt160.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
                };

                engine = new ApplicationEngine(TriggerType.Application, tx, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(ByteString));
                Assert.AreEqual(engine.ResultStack.Pop().GetSpan().ToHexString(),
                    @"5b224435724a376f755c753030324256574845456c5c75303032426e74486b414a424f614c4a6737496776303356337a4953646d6750413d222c362c342c222f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f383d222c332c322c352c2241513d3d225d");
                Assert.AreEqual(0, engine.ResultStack.Count);
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

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 100_000_000, false);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(engine.Execute(), VMState.HALT);

                // Check the results

                CollectionAssert.AreEqual
                    (
                    engine.ResultStack.Select(u => (int)((VM.Types.Integer)u).GetBigInteger()).ToArray(),
                    new int[] { 99_999_570, 99_999_140, 99_998_650 }
                    );
            }

            // Check test mode

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);

                // Execute

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                // Check the results

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(Integer));
                Assert.AreEqual(-1, engine.ResultStack.Pop().GetBigInteger());
            }
        }

        [TestMethod]
        public void System_Runtime_GetInvocationCounter()
        {
            ContractState contractA, contractB, contractC;
            var snapshot = Blockchain.Singleton.GetSnapshot().Clone();
            var contracts = snapshot.Contracts;

            // Create dummy contracts

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetInvocationCounter);

                contractA = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP }.Concat(script.ToArray()).ToArray() };
                contractB = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() };
                contractC = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() };

                // Init A,B,C contracts
                // First two drops is for drop method and arguments

                contracts.Delete(contractA.ScriptHash);
                contracts.Delete(contractB.ScriptHash);
                contracts.Delete(contractC.ScriptHash);
                contractA.Manifest = TestUtils.CreateDefaultManifest(contractA.ScriptHash, "dummyMain");
                contractB.Manifest = TestUtils.CreateDefaultManifest(contractA.ScriptHash, "dummyMain");
                contractC.Manifest = TestUtils.CreateDefaultManifest(contractA.ScriptHash, "dummyMain");
                contracts.Add(contractA.ScriptHash, contractA);
                contracts.Add(contractB.ScriptHash, contractB);
                contracts.Add(contractC.ScriptHash, contractC);
            }

            // Call A,B,B,C

            using (var script = new ScriptBuilder())
            {
                script.EmitAppCall(contractA.ScriptHash, "dummyMain", 0, 1);
                script.EmitAppCall(contractB.ScriptHash, "dummyMain", 0, 1);
                script.EmitAppCall(contractB.ScriptHash, "dummyMain", 0, 1);
                script.EmitAppCall(contractC.ScriptHash, "dummyMain", 0, 1);

                // Execute

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());

                // Check the results

                CollectionAssert.AreEqual
                    (
                    engine.ResultStack.Select(u => (int)((VM.Types.Integer)u).GetBigInteger()).ToArray(),
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
