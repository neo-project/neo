using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;

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
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
                InteropService.GetPrice(InteropService.Runtime.CheckWitness, ae.CurrentContext.EvaluationStack, ae.Snapshot).Should().Be(0_00030000L);
            }

            // System.Storage.GetContext: 9bf667ce (price is 1)
            byte[] SyscallSystemStorageGetContextHash = new byte[] { 0x68, 0x9b, 0xf6, 0x67, 0xce };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetContextHash);
                InteropService.GetPrice(InteropService.Storage.GetContext, ae.CurrentContext.EvaluationStack, ae.Snapshot).Should().Be(0_00000400L);
            }

            // System.Storage.Get: 925de831 (price is 100)
            byte[] SyscallSystemStorageGetHash = new byte[] { 0x68, 0x92, 0x5d, 0xe8, 0x31 };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetHash);
                InteropService.GetPrice(InteropService.Storage.Get, ae.CurrentContext.EvaluationStack, ae.Snapshot).Should().Be(0_01000000L);
            }
        }

        [TestMethod]
        public void ApplicationEngineVariablePrices()
        {
            // Neo.Contract.Create: f66ca56e (requires push properties on fourth position)
            byte[] SyscallContractCreateHash00 = new byte[] { (byte)OpCode.PUSHDATA1, 0x01, 0x00, (byte)OpCode.PUSHDATA1, 0x02, 0x00, 0x00, (byte)OpCode.SYSCALL, 0xf6, 0x6c, 0xa5, 0x6e };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallContractCreateHash00);
                debugger.StepInto(); // PUSHDATA1
                debugger.StepInto(); // PUSHDATA1
                InteropService.GetPrice(InteropService.Contract.Create, ae.CurrentContext.EvaluationStack, ae.Snapshot).Should().Be(0_00300000L);
            }

            // System.Storage.Put: e63f1884 (requires push key and value)
            byte[] SyscallStoragePutHash = new byte[] { (byte)OpCode.PUSH3, (byte)OpCode.PUSH3, (byte)OpCode.PUSH0, (byte)OpCode.SYSCALL, 0xe6, 0x3f, 0x18, 0x84 };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallStoragePutHash);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                Action act = () => InteropService.GetPrice(InteropService.Storage.Put, ae.CurrentContext.EvaluationStack, ae.Snapshot);
                act.Should().Throw<InvalidCastException>();
            }

            // System.Storage.PutEx: 73e19b3a (requires push key and value)
            byte[] SyscallStoragePutExHash = new byte[] { (byte)OpCode.PUSH3, (byte)OpCode.PUSH3, (byte)OpCode.PUSH0, (byte)OpCode.SYSCALL, 0x73, 0xe1, 0x9b, 0x3a };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(SyscallStoragePutExHash);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                Action act = () => InteropService.GetPrice(InteropService.Storage.Put, ae.CurrentContext.EvaluationStack, ae.Snapshot);
                act.Should().Throw<InvalidCastException>();
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
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(new byte[0] { });

            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.Storages.Add(skey, sItem);
            snapshot.Contracts.Add(script.ToScriptHash(), contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                var defaultDataPrice = InteropService.GetPrice(InteropService.Storage.Put, ae.CurrentContext.EvaluationStack, ae.Snapshot);
                defaultDataPrice.Should().Be(InteropService.Storage.GasPerByte * value.Length);
                var expectedCost = defaultDataPrice + setupPrice;
                debugger.Execute();
                ae.GasConsumed.Should().Be(expectedCost);
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
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(value);

            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.Storages.Add(skey, sItem);
            snapshot.Contracts.Add(script.ToScriptHash(), contractState);

            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, testMode: true))
            {
                Debugger debugger = new Debugger(applicationEngine);
                applicationEngine.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = applicationEngine.GasConsumed;
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Put, applicationEngine.CurrentContext.EvaluationStack, applicationEngine.Snapshot);
                reusedDataPrice.Should().Be(1 * InteropService.Storage.GasPerByte);
                debugger.Execute();
                var expectedCost = reusedDataPrice + setupPrice;
                applicationEngine.GasConsumed.Should().Be(expectedCost);
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
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.Storages.Add(skey, sItem);
            snapshot.Contracts.Add(script.ToScriptHash(), contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Put, ae.CurrentContext.EvaluationStack, ae.Snapshot);
                reusedDataPrice.Should().Be(1 * InteropService.Storage.GasPerByte);
                debugger.StepInto();
                var expectedCost = reusedDataPrice + setupPrice;
                debugger.StepInto();
                ae.GasConsumed.Should().Be(expectedCost);
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
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            var snapshot = Blockchain.Singleton.GetSnapshot();
            snapshot.Storages.Add(skey, sItem);
            snapshot.Contracts.Add(script.ToScriptHash(), contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto(); //push key
                debugger.StepInto(); //push value
                debugger.StepInto(); //syscall Storage.GetContext
                var setupPrice = ae.GasConsumed;
                var incrementDataPrice = InteropService.GetPrice(InteropService.Storage.Put, ae.CurrentContext.EvaluationStack, ae.Snapshot);
                incrementDataPrice.Should().Be(1 * InteropService.Storage.GasPerByte);
                debugger.StepInto(); // syscall Storage.Put

                debugger.StepInto(); //push key
                debugger.StepInto(); //push value
                debugger.StepInto();
                setupPrice = ae.GasConsumed;
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Put, ae.CurrentContext.EvaluationStack, ae.Snapshot);
                reusedDataPrice.Should().Be(1 * InteropService.Storage.GasPerByte); // = PUT basic fee
            }
        }

        private byte[] CreateMultiplePutScript(byte[] key, byte[] value, int times = 2)
        {
            var scriptBuilder = new ScriptBuilder();

            for (int i = 0; i < times; i++)
            {
                scriptBuilder.EmitPush(value);
                scriptBuilder.EmitPush(key);
                scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
                scriptBuilder.EmitSysCall(InteropService.Storage.Put);
            }

            return scriptBuilder.ToArray();
        }

        private byte[] CreatePutScript(byte[] key, byte[] value)
        {
            var scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.Put);
            return scriptBuilder.ToArray();
        }
    }
}
