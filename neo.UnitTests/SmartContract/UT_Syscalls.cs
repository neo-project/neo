using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            var snapshot = TestBlockchain.GetStore().GetSnapshot();

            using (var script = new ScriptBuilder())
            {
                script.EmitPush(block.Hash.ToArray());
                script.EmitSysCall(InteropService.System_Blockchain_GetBlock);

                // Without block

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With block

                var blocks = (TestDataCache<UInt256, TrimmedBlock>)snapshot.Blocks;
                var txs = (TestDataCache<UInt256, TransactionState>)snapshot.Transactions;
                blocks.Add(block.Hash, block.Trim());
                txs.Add(tx.Hash, new TransactionState() { Transaction = tx, BlockIndex = block.Index, VMState = VMState.HALT });

                script.EmitSysCall(InteropService.Neo_Json_Serialize);
                engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(ByteArray));
                Assert.AreEqual(engine.ResultStack.Pop().GetByteArray().ToHexString(),
                    "5b22515c7546464644795c75464646445c75464646445c75303030335c75464646445c75464646445c754646464475465c7530303046715c75303132415c625b595c75303434335c75464646445c75464646447d5d767b385c7546464644785c75303032375c75464646445c7546464644222c332c225c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c7530303030222c225c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c7530303030222c322c312c225c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c75303030305c7530303030222c315d");
                Assert.AreEqual(0, engine.ResultStack.Count);

                // Clean
                blocks.Delete(block.Hash);
                txs.Delete(tx.Hash);
            }
        }

        [TestMethod]
        public void System_ExecutionEngine_GetScriptContainer()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(InteropService.System_ExecutionEngine_GetScriptContainer);

                // Without tx

                var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsTrue(engine.ResultStack.Peek().IsNull);

                // With tx

                script.EmitSysCall(InteropService.Neo_Json_Serialize);

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
                Assert.AreEqual(engine.ResultStack.Pop().GetByteArray().ToHexString(),
                    @"5b225c7546464644445c7546464644615c7546464644732c5c75464646445c7546464644665c75303030375d5c75303030305c75464646445c75303632325c7546464644545c7546464644375c7530303133335c75303031385c7530303033655c75464646445c75464646445c75303032375a5c75464646445c2f5c7546464644222c362c342c225c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c75464646445c7546464644222c332c322c352c225c7530303031225d");
                Assert.AreEqual(0, engine.ResultStack.Count);
            }
        }

        [TestMethod]
        public void System_Runtime_GetInvocationCounter()
        {
            ContractState contractA, contractB, contractC;
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            var contracts = (TestDataCache<UInt160, ContractState>)snapshot.Contracts;

            // Create dummy contracts

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(InteropService.System_Runtime_GetInvocationCounter);

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
                script.EmitSysCall(InteropService.System_Contract_Call, contractA.ScriptHash.ToArray(), "dummyMain", 0);
                script.EmitSysCall(InteropService.System_Contract_Call, contractB.ScriptHash.ToArray(), "dummyMain", 0);
                script.EmitSysCall(InteropService.System_Contract_Call, contractB.ScriptHash.ToArray(), "dummyMain", 0);
                script.EmitSysCall(InteropService.System_Contract_Call, contractC.ScriptHash.ToArray(), "dummyMain", 0);

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
