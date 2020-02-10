using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.Wallets;

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
                InteropService.GetPrice(InteropService.Runtime.CheckWitness, ae.CurrentContext.EvaluationStack).Should().Be(0_00030000L);
            }

            // System.Storage.GetContext: 9bf667ce (price is 1)
            byte[] SyscallSystemStorageGetContextHash = new byte[] { 0x68, 0x9b, 0xf6, 0x67, 0xce };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetContextHash);
                InteropService.GetPrice(InteropService.Storage.GetContext, ae.CurrentContext.EvaluationStack).Should().Be(0_00000400L);
            }

            // System.Storage.Get: 925de831 (price is 100)
            byte[] SyscallSystemStorageGetHash = new byte[] { 0x68, 0x92, 0x5d, 0xe8, 0x31 };
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0))
            {
                ae.LoadScript(SyscallSystemStorageGetHash);
                InteropService.GetPrice(InteropService.Storage.Get, ae.CurrentContext.EvaluationStack).Should().Be(0_01000000L);
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
                InteropService.GetPrice(InteropService.Contract.Create, ae.CurrentContext.EvaluationStack).Should().Be(0_00300000L);
            }

            var key = new byte[] { (byte)OpCode.PUSH3 };
            var value = new byte[] { (byte)OpCode.PUSH3 };
            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(null);

            var mockedStoreView = new Mock<StoreView>();
            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            byte[] scriptPut = script;
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(scriptPut);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                InteropService.GetPrice(InteropService.Storage.Put, ae).Should().Be(200000L);
            }

            key = new byte[] { (byte)OpCode.PUSH3 };
            value = new byte[] { (byte)OpCode.PUSH3 };
            script = CreatePutExScript(key, value);

            byte[] scriptPutEx = script;
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(scriptPutEx);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                InteropService.GetPrice(InteropService.Storage.PutEx, ae).Should().Be(200000L);
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
            StorageItem sItem = TestUtils.GetStorageItem(null);

            var mockedStoreView = new Mock<StoreView>();
            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                var defaultDataPrice = InteropService.GetPrice(InteropService.Storage.Put, ae);
                defaultDataPrice.Should().Be(InteropService.Storage.GasPerByte * (key.Length + value.Length));
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

            var mockedStoreView = new Mock<StoreView>();

            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(value);

            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(applicationEngine);
                applicationEngine.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = applicationEngine.GasConsumed;
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Put, applicationEngine);
                reusedDataPrice.Should().Be(0);
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

            var mockedStoreView = new Mock<StoreView>();
            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Put, ae);
                reusedDataPrice.Should().Be(1 * InteropService.Storage.GasPerByte);
                debugger.StepInto();
                var expectedCost = reusedDataPrice + setupPrice;
                debugger.StepInto();
                ae.GasConsumed.Should().Be(expectedCost);
            }
        }

        /// <summary>
        /// Use put for the same key twice.
        /// Pays for 1 extra byte for the first Put and 3 extra bytes for the second put (key + value).
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

            var mockedStoreView = new Mock<StoreView>();
            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                ae.Execute();
                //1 reused + 1 from key + 2 from value (no discount on second use)
                ae.GasConsumed.Should().BeGreaterThan(4 * InteropService.Storage.GasPerByte);
            }
        }

        /// <summary>
        /// Releases 1 byte from the storage receiving Gas credit using implicit delete
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReleaseStorage_ImplicitDelete()
        {
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };

            byte[] script = CreateImplicitDeleteScript(key);

            ContractState contractState = TestUtils.GetContract(script);
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            var mockedStoreView = new Mock<StoreView>();
            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Put, ae);
                reusedDataPrice.Should().Be(1 * InteropService.Storage.GasPerReleasedByte);
                debugger.StepInto();
                var expectedCost = setupPrice;
                var expectedReward = -reusedDataPrice;
                ae.GasConsumed.Should().Be(expectedCost);
                ae.RecyclingRewardGas.Should().Be(expectedReward);
            }
        }

        /// <summary>
        /// Releases 1 byte from the storage receiving Gas credit using explicit delete
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReleaseStorage_ExplicitDelete()
        {
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };

            byte[] script = CreateExplicitDeleteScript(key);

            ContractState contractState = TestUtils.GetContract(script);
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            var mockedStoreView = new Mock<StoreView>();
            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                var setupPrice = ae.GasConsumed;
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Delete, ae);
                reusedDataPrice.Should().Be((skey.Key.Length + sItem.Value.Length) * InteropService.Storage.GasPerReleasedByte);
                debugger.StepInto();
                var expectedCost = setupPrice;
                var expectedReward = -reusedDataPrice;
                ae.GasConsumed.Should().Be(expectedCost);
                ae.RecyclingRewardGas.Should().Be(expectedReward);
            }
        }

        [TestMethod]
        public void TestCalculateMinimumRequiredToRun()
        {
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };

            byte[] script = CreateExplicitDeleteScript(key);

            ContractState contractState = TestUtils.GetContract(script);
            contractState.Manifest.Features = ContractFeatures.HasStorage;

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            var mockedStoreView = new Mock<StoreView>();
            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            long ConsumedGas = 0;
            long rewardGas = 0;
            long minimumRequiredToRun = 0;
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                ae.LoadScript(script);
                ae.Execute();
                ConsumedGas = ae.GasConsumed;
                rewardGas = ae.RecyclingRewardGas;
            }
            rewardGas.Should().Be(2 * InteropService.Storage.GasPerByte);

            minimumRequiredToRun = ConsumedGas;
            //If you send GasConsumed, it should HALT because GasConsumed is enough to cover the cost.
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, minimumRequiredToRun, testMode: false))
            {
                ae.LoadScript(script);
                ae.Execute();
                ae.State.Should().Be(VMState.HALT);
            }

            minimumRequiredToRun = ConsumedGas - 1;
            //If you send GasConsumed-1, it should FAULT because it isn't enough to cover the cost.
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, minimumRequiredToRun, testMode: false))
            {
                ae.LoadScript(script);
                ae.Execute();
                ae.State.Should().Be(VMState.FAULT);
            }
        }

        private byte[] CreateExplicitDeleteScript(byte[] key)
        {
            var scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.Delete);
            return scriptBuilder.ToArray();
        }

        private byte[] CreateImplicitDeleteScript(byte[] key)
        {
            var emptyArray = new byte[0];
            var scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitPush(emptyArray);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.Put);
            return scriptBuilder.ToArray();
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

        private byte[] CreatePutExScript(byte[] key, byte[] value)
        {
            var scriptBuilder = new ScriptBuilder();
            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.PutEx);
            return scriptBuilder.ToArray();
        }
    }
}
