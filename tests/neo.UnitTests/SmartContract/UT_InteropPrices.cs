using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.UnitTests.Extensions;
using Neo.VM;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropPrices
    {
        [TestInitialize]
        public void Initialize()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void ApplicationEngineFixedPrices()
        {
            // System.Runtime.CheckWitness: f827ec8c (price is 200)
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, null, gas: 0))
            {
                ae.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
                ApplicationEngine.System_Runtime_CheckWitness.FixedPrice.Should().Be(0_00001024L);
            }

            // System.Storage.GetContext: 9bf667ce (price is 1)
            byte[] SyscallSystemStorageGetContextHash = new byte[] { 0x68, 0x9b, 0xf6, 0x67, 0xce };
            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, null, gas: 0))
            {
                ae.LoadScript(SyscallSystemStorageGetContextHash);
                ApplicationEngine.System_Storage_GetContext.FixedPrice.Should().Be(0_00000016L);
            }

            // System.Storage.Get: 925de831 (price is 100)
            byte[] SyscallSystemStorageGetHash = new byte[] { 0x68, 0x92, 0x5d, 0xe8, 0x31 };
            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, null, gas: 0))
            {
                ae.LoadScript(SyscallSystemStorageGetHash);
                ApplicationEngine.System_Storage_Get.FixedPrice.Should().Be(32768L);
            }
        }

        /// <summary>
        /// Put without previous content (should charge per byte used)
        /// </summary>
        [TestMethod]
        public void ApplicationEngineRegularPut()
        {
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1 };

            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(new byte[0] { });

            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                debugger.Execute();
                (ae.GasConsumed - setupPrice).Should().Be(ae.StoragePrice * (1 + value.Length));
            }
        }

        /// <summary>
        /// Reuses the same amount of storage. Should cost 0.
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReusedStorage_FullReuse()
        {
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1 };

            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(value);

            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using (ApplicationEngine applicationEngine = ApplicationEngine.Create(TriggerType.Application, null, snapshot))
            {
                Debugger debugger = new Debugger(applicationEngine);
                applicationEngine.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = applicationEngine.GasConsumed;
                debugger.Execute();
                (applicationEngine.GasConsumed - setupPrice).Should().Be(1 * applicationEngine.StoragePrice);
            }
        }

        /// <summary>
        /// Reuses one byte and allocates a new one
        /// It should only pay for the second byte.
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReusedStorage_PartialReuse()
        {
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH1 };

            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                debugger.StepInto();
                debugger.StepInto();
                (ae.GasConsumed - setupPrice).Should().Be((1 + (oldValue.Length / 4) + value.Length - oldValue.Length) * ae.StoragePrice);
            }
        }

        /// <summary>
        /// Use put for the same key twice.
        /// Pays for 1 extra byte for the first Put and 1 byte for the second basic fee (as value2.length == value1.length).
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReusedStorage_PartialReuseTwice()
        {
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH1 };

            byte[] script = CreateMultiplePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            var snapshot = TestBlockchain.GetTestSnapshot();
            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto(); //push value
                debugger.StepInto(); //push key
                debugger.StepInto(); //syscall Storage.GetContext
                debugger.StepInto(); //syscall Storage.Put
                debugger.StepInto(); //push value
                debugger.StepInto(); //push key
                debugger.StepInto(); //syscall Storage.GetContext
                var setupPrice = ae.GasConsumed;
                debugger.StepInto(); //syscall Storage.Put
                (ae.GasConsumed - setupPrice).Should().Be((sItem.Value.Length / 4 + 1) * ae.StoragePrice); // = PUT basic fee
            }
        }

        private byte[] CreateMultiplePutScript(byte[] key, byte[] value, int times = 2)
        {
            var scriptBuilder = new ScriptBuilder();

            for (int i = 0; i < times; i++)
            {
                scriptBuilder.EmitPush(value);
                scriptBuilder.EmitPush(key);
                scriptBuilder.EmitSysCall(ApplicationEngine.System_Storage_GetContext);
                scriptBuilder.EmitSysCall(ApplicationEngine.System_Storage_Put);
            }

            return scriptBuilder.ToArray();
        }

        private byte[] CreatePutScript(byte[] key, byte[] value)
        {
            var scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(ApplicationEngine.System_Storage_GetContext);
            scriptBuilder.EmitSysCall(ApplicationEngine.System_Storage_Put);
            return scriptBuilder.ToArray();
        }
    }
}
