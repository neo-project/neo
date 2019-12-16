using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
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

            var mockedStoreView = new Mock<StoreView>();

            var manifest = ContractManifest.CreateDefault(UInt160.Zero);
            manifest.Features = ContractFeatures.HasStorage;

            var scriptBuilder = new ScriptBuilder();
            var key = new byte[] { (byte)OpCode.PUSH3 };
            var value = new byte[] { (byte)OpCode.PUSH3 };
            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.Put);
            byte[] script = scriptBuilder.ToArray();

            ContractState contractState = new ContractState
            {
                Script = script,
                Manifest = manifest
            };

            StorageKey skey = new StorageKey
            {
                ScriptHash = script.ToScriptHash(),
                Key = key
            };

            StorageItem sItem = null;

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

            scriptBuilder = new ScriptBuilder();
            key = new byte[] { (byte)OpCode.PUSH3 };
            value = new byte[] { (byte)OpCode.PUSH3 };
            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.PutEx);
            script = scriptBuilder.ToArray();

            byte[] scriptPutEx = script;
            using (ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(ae);
                ae.LoadScript(scriptPut);
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 03 (length 1)
                debugger.StepInto(); // push 00
                InteropService.GetPrice(InteropService.Storage.PutEx, ae).Should().Be(200000L);
            }
        }

        [TestMethod]
        public void ApplicationEngineRegularPut()
        {
            var scriptBuilder = new ScriptBuilder();

            var key = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1 };

            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.Put);

            var mockedStoreView = new Mock<StoreView>();

            byte[] script = scriptBuilder.ToArray();

            var manifest = ContractManifest.CreateDefault(UInt160.Zero);
            manifest.Features = ContractFeatures.HasStorage;

            ContractState contractState = new ContractState
            {
                Script = script,
                Manifest = manifest
            };

            StorageKey skey = new StorageKey
            {
                ScriptHash = script.ToScriptHash(),
                Key = key
            };

            StorageItem sItem = null;

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
                debugger.StepInto();
                var expectedCost = defaultDataPrice + setupPrice;
                debugger.StepInto();
                ae.GasConsumed.Should().Be(expectedCost);
            }
        }

        [TestMethod]
        public void ApplicationEngineReusedStorage_FullReuse()
        {
            var scriptBuilder = new ScriptBuilder();

            var key = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1 };

            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.Put);

            var mockedStoreView = new Mock<StoreView>();

            byte[] script = scriptBuilder.ToArray();

            var manifest = ContractManifest.CreateDefault(UInt160.Zero);
            manifest.Features = ContractFeatures.HasStorage;

            ContractState contractState = new ContractState
            {
                Script = script,
                Manifest = manifest
            };

            StorageKey skey = new StorageKey
            {
                ScriptHash = script.ToScriptHash(),
                Key = key
            };

            StorageItem sItem = new StorageItem
            {
                Value = value
            };

            mockedStoreView.Setup(p => p.Storages.TryGet(skey)).Returns(sItem);
            mockedStoreView.Setup(p => p.Contracts.TryGet(script.ToScriptHash())).Returns(contractState);

            using (ApplicationEngine applicationEngine = new ApplicationEngine(TriggerType.Application, null, mockedStoreView.Object, 0, testMode: true))
            {
                Debugger debugger = new Debugger(applicationEngine);
                applicationEngine.LoadScript(script);
                debugger.StepInto();
                debugger.StepInto();
                debugger.StepInto();
                var reusedDataPrice = InteropService.GetPrice(InteropService.Storage.Put, applicationEngine);
                reusedDataPrice.Should().Be(0);
            }
        }

        [TestMethod]
        public void ApplicationEngineReusedStorage_PartialReuse()
        {
            var scriptBuilder = new ScriptBuilder();

            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH1 };

            scriptBuilder.EmitPush(value);
            scriptBuilder.EmitPush(key);
            scriptBuilder.EmitSysCall(InteropService.Storage.GetContext);
            scriptBuilder.EmitSysCall(InteropService.Storage.Put);

            var mockedStoreView = new Mock<StoreView>();

            byte[] script = scriptBuilder.ToArray();

            var manifest = ContractManifest.CreateDefault(UInt160.Zero);
            manifest.Features = ContractFeatures.HasStorage;

            ContractState contractState = new ContractState
            {
                Script = script,
                Manifest = manifest
            };

            StorageKey skey = new StorageKey
            {
                ScriptHash = script.ToScriptHash(),
                Key = key
            };

            StorageItem sItem = new StorageItem
            {
                Value = oldValue
            };

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
    }

}
