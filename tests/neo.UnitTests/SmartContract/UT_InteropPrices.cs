using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropPrices
    {
        [TestMethod]
        public void ApplicationEngineFixedPrices()
        {
            // System.Runtime.CheckWitness: f827ec8c (price is 200)
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
                InteropService.GetPrice(InteropService.System_Runtime_CheckWitness, ae.CurrentContext.EvaluationStack).Should().Be(0_00030000L);
            }

            // System.Storage.GetContext: 9bf667ce (price is 1)
            byte[] SyscallSystemStorageGetContextHash = new byte[] { 0x68, 0x9b, 0xf6, 0x67, 0xce };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetContextHash);
                InteropService.GetPrice(InteropService.System_Storage_GetContext, ae.CurrentContext.EvaluationStack).Should().Be(0_00000400L);
            }

            // System.Storage.Get: 925de831 (price is 100)
            byte[] SyscallSystemStorageGetHash = new byte[] { 0x68, 0x92, 0x5d, 0xe8, 0x31 };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetHash);
                InteropService.GetPrice(InteropService.System_Storage_Get, ae.CurrentContext.EvaluationStack).Should().Be(0_01000000L);
            }
        }

        [TestMethod]
        public void ApplicationEngineVariablePrices()
        {
            // Neo.Contract.Create: f66ca56e (requires push properties on fourth position)
            byte[] SyscallContractCreateHash00 = new byte[] { 0x01, 0x00, 0x02, 0x00, 0x00, 0x68, 0xf6, 0x6c, 0xa5, 0x6e };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractCreateHash00);
                debugger.StepInto(); // PUSHBYTES1
                debugger.StepInto(); // PUSHBYTES2
                InteropService.GetPrice(InteropService.Neo_Contract_Create, ae.CurrentContext.EvaluationStack).Should().Be(0_00300000L);
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
                InteropService.GetPrice(InteropService.System_Storage_Put, ae.CurrentContext.EvaluationStack).Should().Be(200000L);
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
                InteropService.GetPrice(InteropService.System_Storage_PutEx, ae.CurrentContext.EvaluationStack).Should().Be(200000L);
            }
        }
    }
}
