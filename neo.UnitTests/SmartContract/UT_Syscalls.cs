using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Syscalls
    {
        [TestMethod]
        public void System_Storage_GetContext()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            var contracts = (TestDataCache<UInt160, ContractState>)snapshot.Contracts;

            // Call System_Storage_GetContext syscall

            var script = new ScriptBuilder();
            script.EmitSysCall(InteropService.System_Storage_GetContext);

            var contract = new ContractState() { Script = script.ToArray() };
            contract.Manifest = ContractManifest.CreateDefault(contract.ScriptHash);

            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contract.ScriptHash.ToArray()));
            contracts.Add(contract.ScriptHash, contract);

            // Call Contract

            script = new ScriptBuilder();
            script.EmitSysCall(InteropService.System_Contract_Call, contract.ScriptHash.ToArray(), "", 0);

            // Execute

            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(engine.Execute(), VMState.HALT);

            // Check the results

            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(VM.Types.InteropInterface));

            var context = ((VM.Types.InteropInterface)engine.ResultStack.Pop()).GetInterface<StorageContext>();
            Assert.AreEqual(context.ScriptHash, contract.ScriptHash);

            // Change to ReadOnly Abi

            contract.Manifest.Abi.EntryPoint.ReadOnly = true;
            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contract.ScriptHash.ToArray()));
            contracts.Add(contract.ScriptHash, contract);

            // Execute

            engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(engine.Execute(), VMState.FAULT);

            // Check the results

            Assert.AreEqual(0, engine.ResultStack.Count);

            // Clean
            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contract.ScriptHash.ToArray()));
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

            contractA.Manifest = ContractManifest.CreateDefault(contractA.ScriptHash);
            contractB.Manifest = ContractManifest.CreateDefault(contractB.ScriptHash);
            contractC.Manifest = ContractManifest.CreateDefault(contractC.ScriptHash);

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

            // Clean
            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractA.ScriptHash.ToArray()));
            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractB.ScriptHash.ToArray()));
            contracts.DeleteWhere((a, b) => a.ToArray().SequenceEqual(contractC.ScriptHash.ToArray()));
        }
    }
}
