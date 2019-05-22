using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.VM;
using System.Reflection;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_InteropPrices
    {
        [TestMethod]
        public void ApplicationEngineFixedPrices()
        {
            MethodInfo GetPriceForSysCall = typeof(ApplicationEngine).GetMethod("GetPriceForSysCall", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(uint) }, null);

            // System.Runtime.CheckWitness: f827ec8c (price is 200)
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.System_Runtime_CheckWitness }).Should().Be(0_00030000L);
            }

            // System.Storage.GetContext: 9bf667ce (price is 1)
            byte[] SyscallSystemStorageGetContextHash = new byte[] { 0x68, 0x9b, 0xf6, 0x67, 0xce };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetContextHash);
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.System_Storage_GetContext }).Should().Be(0_00000400L);
            }

            // System.Storage.Get: 925de831 (price is 100)
            byte[] SyscallSystemStorageGetHash = new byte[] { 0x68, 0x92, 0x5d, 0xe8, 0x31 };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetHash);
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.System_Storage_Get }).Should().Be(0_01000000L);
            }
        }

        [TestMethod]
        public void ApplicationEngineVariablePrices()
        {
            MethodInfo GetPriceForSysCall = typeof(ApplicationEngine).GetMethod("GetPriceForSysCall", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(uint) }, null);

            // Neo.Contract.Create: f66ca56e (requires push properties on fourth position)
            byte[] SyscallContractCreateHash00 = new byte[] { (byte)ContractPropertyState.NoProperty, 0x00, 0x00, 0x00, 0x68, 0xf6, 0x6c, 0xa5, 0x6e };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractCreateHash00);
                debugger.StepInto(); // push 0 - ContractPropertyState.NoProperty
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.Neo_Contract_Create }).Should().Be(100_00000000L);
            }

            // Neo.Contract.Create: f66ca56e (requires push properties on fourth position)
            byte[] SyscallContractCreateHash01 = new byte[] { 0x51, 0x00, 0x00, 0x00, 0x68, 0xf6, 0x6c, 0xa5, 0x6e };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractCreateHash01);
                debugger.StepInto(); // push 01 - ContractPropertyState.HasStorage
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.Neo_Contract_Create }).Should().Be(100_00000000L); // assuming private ae.ratio = 100000
            }

            // Neo.Contract.Migrate: 471b6290 (requires push properties on fourth position)
            byte[] SyscallContractMigrateHash00 = new byte[] { (byte)ContractPropertyState.NoProperty, 0x00, 0x00, 0x00, 0x68, 0x47, 0x1b, 0x62, 0x90 };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractMigrateHash00);
                debugger.StepInto(); // push 0 - ContractPropertyState.NoProperty
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                debugger.StepInto(); // push 0
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.Neo_Contract_Migrate }).Should().Be(10_00000000L); // assuming private ae.ratio = 100000
            }

            // System.Storage.Put: e63f1884 (requires push key and value)
            byte[] SyscallStoragePutHash = new byte[] { 0x53, 0x53, 0x00, 0x68, 0xe6, 0x3f, 0x18, 0x84 };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallStoragePutHash);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.System_Storage_Put }).Should().Be(200000L); //(1+1) * 100000;
            }

            // System.Storage.PutEx: 73e19b3a (requires push key and value)
            byte[] SyscallStoragePutExHash = new byte[] { 0x53, 0x53, 0x00, 0x68, 0x73, 0xe1, 0x9b, 0x3a };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallStoragePutExHash);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                GetPriceForSysCall.Invoke(ae, new object[] { InteropService.System_Storage_PutEx }).Should().Be(200000L); //(1+1) * 100000;
            }
        }
    }
}
