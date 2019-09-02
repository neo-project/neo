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
        public void Neo_Transaction_GetScript()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();

            var script = new ScriptBuilder();
            script.EmitSysCall(InteropService.Neo_Transaction_GetScript);

            // With tx

            var tx = new Transaction() { Script = new byte[] { 0x01 } };

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(tx));

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            CollectionAssert.AreEqual(engine.ResultStack.Pop().GetByteArray(), tx.Script);

            // Without push

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);

            // Without tx

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(new object()));

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);
        }

        [TestMethod]
        public void Neo_Transaction_GetHash()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();

            var script = new ScriptBuilder();
            script.EmitSysCall(InteropService.System_Transaction_GetHash);

            // With tx

            var tx = new Transaction()
            {
                Sender = UInt160.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
                Attributes = new TransactionAttribute[0],
                Cosigners = new Cosigner[0],
                Script = new byte[0],
                Witnesses = new Witness[0],
            };

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(tx));

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            CollectionAssert.AreEqual(engine.ResultStack.Pop().GetByteArray(), tx.Hash.ToArray());

            // Without push

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);

            // Without tx

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(new object()));

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);
        }

        [TestMethod]
        public void Neo_Transaction_GetWitnesses()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();

            var script = new ScriptBuilder();
            script.EmitSysCall(InteropService.Neo_Transaction_GetWitnesses);

            // With tx

            var tx = new Transaction()
            {
                Witnesses = new Witness[]
                {
                     new Witness()
                     {
                         InvocationScript = new byte[] { 0x01 },
                         VerificationScript = new byte[] { 0x02 }
                     }
                }
            };

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(tx));

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            var item = (Array)engine.ResultStack.Pop();
            CollectionAssert.AreEqual((item[0] as InteropInterface)
                .GetInterface<WitnessWrapper>().VerificationScript, tx.Witnesses[0].VerificationScript.ToArray());

            // More than expected

            tx.Witnesses = new Witness[engine.MaxArraySize + 1];

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(tx));

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);

            // Without push

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);

            // Without tx

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(new object()));

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);
        }

        [TestMethod]
        public void Neo_Transaction_GetNonce()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();

            var script = new ScriptBuilder();
            script.EmitSysCall(InteropService.System_Transaction_GetNonce);

            // With tx

            var tx = new Transaction() { Nonce = 0x505152 };

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(tx));

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(engine.ResultStack.Pop().GetBigInteger(), tx.Nonce);

            // Without push

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);

            // Without tx

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(new object()));

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);
        }

        [TestMethod]
        public void Neo_Transaction_GetSender()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();

            var script = new ScriptBuilder();
            script.EmitSysCall(InteropService.System_Transaction_GetSender);

            // With tx

            var tx = new Transaction() { Sender = UInt160.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF") };

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(tx));

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            CollectionAssert.AreEqual(engine.ResultStack.Pop().GetByteArray(), tx.Sender.ToArray());

            // Without push

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);

            // Without tx

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.EvaluationStack.Push(InteropInterface.FromInterface(new object()));

            Assert.AreEqual(engine.Execute(), VMState.FAULT);
            Assert.AreEqual(0, engine.ResultStack.Count);
        }

        [TestMethod]
        public void System_Runtime_GetInvocationCounter()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            var contracts = (TestDataCache<UInt160, ContractState>)snapshot.Contracts;

            // Call System.Runtime.GetInvocationCounter syscall

            var script = new ScriptBuilder();
            script.EmitSysCall(InteropService.System_Runtime_GetInvocationCounter);

            // Init A,B,C contracts
            // First two drops is for drop method and arguments

            var contractA = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP }.Concat(script.ToArray()).ToArray() };
            var contractB = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() };
            var contractC = new ContractState() { Script = new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray() };

            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractA.ScriptHash.ToArray()));
            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractB.ScriptHash.ToArray()));
            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractC.ScriptHash.ToArray()));
            contracts.Add(contractA.ScriptHash, contractA);
            contracts.Add(contractB.ScriptHash, contractB);
            contracts.Add(contractC.ScriptHash, contractC);

            // Call A,B,B,C

            script = new ScriptBuilder();
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
