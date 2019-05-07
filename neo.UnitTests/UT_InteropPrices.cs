using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.VM;
using System.Reflection;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_InteropPrices
    {
        NeoService uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new NeoService(TriggerType.Application, null);
        }

        [TestMethod]
        public void NeoServiceFixedPrices()
        {
            uut.GetPrice("Neo.Blockchain.GetAccount".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Header.GetVersion".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetMerkleRoot".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetConsensusData".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Header.GetNextConsensus".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Transaction.GetWitnesses".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("Neo.InvocationTransaction.GetScript".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Witness.GetVerificationScript".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Account.GetScriptHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Account.IsStandard".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("Neo.Contract.GetScript".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Contract.IsPayable".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Storage.Find".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Create".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Next".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Value".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Enumerator.Concat".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Create".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Key".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Keys".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Values".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("Neo.Iterator.Concat".ToInteropMethodHash()).Should().Be(1);
        }

        [TestMethod]
        public void StandardServiceFixedPrices()
        {
            uut.GetPrice("System.Runtime.Platform".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Runtime.GetTrigger".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Runtime.CheckWitness".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("System.Runtime.Notify".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Runtime.Log".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Runtime.GetTime".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Runtime.Serialize".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Runtime.Deserialize".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Blockchain.GetHeight".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Blockchain.GetHeader".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("System.Blockchain.GetBlock".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("System.Blockchain.GetTransaction".ToInteropMethodHash()).Should().Be(200);
            uut.GetPrice("System.Blockchain.GetTransactionHeight".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("System.Blockchain.GetContract".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("System.Header.GetIndex".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Header.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Header.GetPrevHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Header.GetTimestamp".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Block.GetTransactionCount".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Block.GetTransactions".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Block.GetTransaction".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Transaction.GetHash".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Contract.Destroy".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Contract.GetStorageContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Storage.GetContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Storage.GetReadOnlyContext".ToInteropMethodHash()).Should().Be(1);
            uut.GetPrice("System.Storage.Get".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("System.Storage.Delete".ToInteropMethodHash()).Should().Be(100);
            uut.GetPrice("System.StorageContext.AsReadOnly".ToInteropMethodHash()).Should().Be(1);
        }

        [TestMethod]
        public void ApplicationEngineFixedPrices()
        {
            // ApplicationEngine.GetPriceForSysCall is protected, so we will access through reflection
            MethodInfo GetPriceForSysCall = typeof(ApplicationEngine).GetMethod("GetPriceForSysCall", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[]{}, null);

            // System.Runtime.CheckWitness: f827ec8c (price is 200)
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[]{0x68, 0x04, 0xf8, 0x27, 0xec, 0x8c};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                ae.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(200L);
            }

            // "System.Runtime.CheckWitness" (27 bytes -> 0x1b) - (price is 200)
            byte[] SyscallSystemRuntimeCheckWitnessString = new byte[]{0x68, 0x1b, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6d, 0x2e, 0x52, 0x75, 0x6e, 0x74, 0x69, 0x6d, 0x65, 0x2e, 0x43, 0x68, 0x65, 0x63, 0x6b, 0x57, 0x69, 0x74, 0x6e, 0x65, 0x73, 0x73};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                ae.LoadScript(SyscallSystemRuntimeCheckWitnessString);
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(200L);
            }

            // System.Storage.GetContext: 9bf667ce (price is 1)
            byte[] SyscallSystemStorageGetContextHash = new byte[]{0x68, 0x04, 0x9b, 0xf6, 0x67, 0xce};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                ae.LoadScript(SyscallSystemStorageGetContextHash);
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(1L);
            }

            // System.Storage.Get: 925de831 (price is 100)
            byte[] SyscallSystemStorageGetHash = new byte[]{0x68, 0x04, 0x92, 0x5d, 0xe8, 0x31};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                ae.LoadScript(SyscallSystemStorageGetHash);
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(100L);
            }
        }

        [TestMethod]
        public void ApplicationEngineVariablePrices()
        {
            // ApplicationEngine.GetPriceForSysCall is protected, so we will access through reflection
            MethodInfo GetPriceForSysCall = typeof(ApplicationEngine).GetMethod("GetPriceForSysCall", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[]{}, null);

            // Neo.Contract.Create: f66ca56e (requires push properties on fourth position)
            byte[] SyscallContractCreateHash00 = new byte[]{(byte)ContractPropertyState.NoProperty, 0x00, 0x00, 0x00, 0x68, 0x04, 0xf6, 0x6c, 0xa5, 0x6e};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractCreateHash00);
                debugger.StepInto(); // push 0 - ContractPropertyState.NoProperty
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(100L * 100000000L / 100000); // assuming private ae.ratio = 100000
            }

            // Neo.Contract.Create: f66ca56e (requires push properties on fourth position)
            byte[] SyscallContractCreateHash01 = new byte[]{0x51, 0x00, 0x00, 0x00, 0x68, 0x04, 0xf6, 0x6c, 0xa5, 0x6e};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractCreateHash01);
                debugger.StepInto(); // push 01 - ContractPropertyState.HasStorage
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(500L * 100000000L / 100000); // assuming private ae.ratio = 100000
            }

            // Neo.Contract.Migrate: 471b6290 (requires push properties on fourth position)
            byte[] SyscallContractMigrateHash00 = new byte[]{(byte)ContractPropertyState.NoProperty, 0x00, 0x00, 0x00, 0x68, 0x04, 0x47, 0x1b, 0x62, 0x90};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractMigrateHash00);
                debugger.StepInto(); // push 0 - ContractPropertyState.NoProperty
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(100L * 100000000L / 100000); // assuming private ae.ratio = 100000
            }

            // System.Storage.Put: e63f1884 (requires push key and value)
            byte[] SyscallStoragePutHash = new byte[]{0x53, 0x53, 0x00, 0x68, 0x04, 0xe6, 0x3f, 0x18, 0x84};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallStoragePutHash);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(1000L); //((1+1-1) / 1024 + 1) * 1000);
            }

            // System.Storage.PutEx: 73e19b3a (requires push key and value)
            byte[] SyscallStoragePutExHash = new byte[]{0x53, 0x53, 0x00, 0x68, 0x04, 0x73, 0xe1, 0x9b, 0x3a};
            using ( ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, Fixed8.Zero) )
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallStoragePutExHash);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                GetPriceForSysCall.Invoke(ae, new object[]{}).Should().Be(1000L); //((1+1-1) / 1024 + 1) * 1000);
            }
        }
    }
}
