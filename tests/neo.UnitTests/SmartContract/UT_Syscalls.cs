using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;

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
                Attributes = new TransactionAttribute[0],
                Cosigners = new Cosigner[0],
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
                Index = 1,
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
                script.EmitSysCall(InteropService.Blockchain.GetBlock);

                // Without block

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With block

                var blocks = snapshot.Blocks;
                var txs = snapshot.Transactions;
                blocks.Add(block.Hash, block.Trim());
                txs.Add(tx.Hash, new TransactionState() { Transaction = tx, BlockIndex = block.Index, VMState = VMState.HALT });

                script.EmitSysCall(InteropService.Json.Serialize);
                engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(ByteArray));
                Assert.AreEqual(engine.ResultStack.Pop().GetSpan().ToHexString(),
                    "5b22556168352f4b6f446d39723064555950636353714346745a30594f726b583164646e7334366e676e3962383d222c332c22414141414141414141414141414141414141414141414141414141414141414141414141414141414141413d222c22414141414141414141414141414141414141414141414141414141414141414141414141414141414141413d222c322c312c224141414141414141414141414141414141414141414141414141413d222c315d");
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
                script.EmitSysCall(InteropService.Json.Deserialize);
                script.EmitPush("null");
                script.EmitSysCall(InteropService.Json.Deserialize);

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
                script.EmitSysCall(InteropService.Json.Deserialize);

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
                script.EmitSysCall(InteropService.Json.Deserialize);

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
                script.EmitSysCall(InteropService.Json.Serialize);
                script.Emit(OpCode.PUSH0);
                script.Emit(OpCode.NOT);
                script.EmitSysCall(InteropService.Json.Serialize);
                script.EmitPush("test");
                script.EmitSysCall(InteropService.Json.Serialize);
                script.Emit(OpCode.PUSHNULL);
                script.EmitSysCall(InteropService.Json.Serialize);
                script.Emit(OpCode.NEWMAP);
                script.Emit(OpCode.DUP);
                script.EmitPush("key");
                script.EmitPush("value");
                script.Emit(OpCode.SETITEM);
                script.EmitSysCall(InteropService.Json.Serialize);

                using (var engine = new ApplicationEngine(TriggerType.Application, null, null, 0, true))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.HALT);
                    Assert.AreEqual(5, engine.ResultStack.Count);

                    Assert.IsTrue(engine.ResultStack.TryPop<ByteArray>(out var m) && m.GetString() == "{\"key\":\"dmFsdWU=\"}");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteArray>(out var n) && n.GetString() == "null");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteArray>(out var s) && s.GetString() == "\"dGVzdA==\"");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteArray>(out var b) && b.GetString() == "true");
                    Assert.IsTrue(engine.ResultStack.TryPop<ByteArray>(out var i) && i.GetString() == "5");
                }
            }

            // Error

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(InteropService.Storage.GetContext);
                script.EmitSysCall(InteropService.Json.Serialize);

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
                script.EmitSysCall(InteropService.Runtime.GetScriptContainer);

                // Without tx

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With tx

                script.EmitSysCall(InteropService.Json.Serialize);

                var tx = new Transaction()
                {
                    Script = new byte[] { 0x01 },
                    Attributes = new TransactionAttribute[0],
                    Cosigners = new Cosigner[0],
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
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(ByteArray));
                Assert.AreEqual(engine.ResultStack.Pop().GetSpan().ToHexString(),
                    @"5b225c75303032426b53415959527a4c4b69685a676464414b50596f754655737a63544d7867445a6572584a3172784c37303d222c362c342c222f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f2f383d222c332c322c352c2241513d3d225d");
                Assert.AreEqual(0, engine.ResultStack.Count);
            }
        }

        [TestMethod]
        public void System_Runtime_GetInvocationCounter()
        {
            ContractState contractA, contractB, contractC;
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var contracts = snapshot.Contracts;

            // Create dummy contracts

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(InteropService.Runtime.GetInvocationCounter);

                contractA = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP }.Concat(script.ToArray()).ToArray() };
                contractB = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() };
                contractC = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() };

                // Init A,B,C contracts
                // First two drops is for drop method and arguments

                contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractA.ScriptHash.ToArray()));
                contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractB.ScriptHash.ToArray()));
                contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractC.ScriptHash.ToArray()));
                contracts.Add(contractA.ScriptHash, contractA);
                contracts.Add(contractB.ScriptHash, contractB);
                contracts.Add(contractC.ScriptHash, contractC);
            }

            // Call A,B,B,C

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(InteropService.Contract.Call, contractA.ScriptHash.ToArray(), "dummyMain", 0);
                script.EmitSysCall(InteropService.Contract.Call, contractB.ScriptHash.ToArray(), "dummyMain", 0);
                script.EmitSysCall(InteropService.Contract.Call, contractB.ScriptHash.ToArray(), "dummyMain", 0);
                script.EmitSysCall(InteropService.Contract.Call, contractC.ScriptHash.ToArray(), "dummyMain", 0);

                // Execute

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(engine.Execute(), VMState.HALT);

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
