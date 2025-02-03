// Copyright (C) 2015-2025 The Neo Project.
//
// UT_InteropPrices.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.UnitTests.Extensions;
using Neo.VM;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropPrices
    {
        private StorageCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void ApplicationEngineFixedPrices()
        {
            var snapshot = _snapshotCache.CloneCache();
            // System.Runtime.CheckWitness: f827ec8c (price is 200)
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot, gas: 0))
            {
                ae.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
                Assert.AreEqual(0_00001024L, ApplicationEngine.System_Runtime_CheckWitness.FixedPrice);
            }

            // System.Storage.GetContext: 9bf667ce (price is 1)
            byte[] SyscallSystemStorageGetContextHash = new byte[] { 0x68, 0x9b, 0xf6, 0x67, 0xce };
            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot, gas: 0))
            {
                ae.LoadScript(SyscallSystemStorageGetContextHash);
                Assert.AreEqual(0_00000016L, ApplicationEngine.System_Storage_GetContext.FixedPrice);
            }

            // System.Storage.Get: 925de831 (price is 100)
            byte[] SyscallSystemStorageGetHash = new byte[] { 0x68, 0x92, 0x5d, 0xe8, 0x31 };
            using (ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot, gas: 0))
            {
                ae.LoadScript(SyscallSystemStorageGetHash);
                Assert.AreEqual(32768L, ApplicationEngine.System_Storage_Get.FixedPrice);
            }
        }

        /// <summary>
        /// Put without previous content (should charge per byte used)
        /// </summary>
        [TestMethod]
        public void ApplicationEngineRegularPut()
        {
            var snapshot = _snapshotCache.CloneCache();
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1 };

            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(System.Array.Empty<byte>());

            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            Debugger debugger = new(ae);
            ae.LoadScript(script);
            debugger.StepInto();
            debugger.StepInto();
            debugger.StepInto();
            var setupPrice = ae.FeeConsumed;
            debugger.Execute();
            Assert.AreEqual(ae.StoragePrice * value.Length + (1 << 15) * 30, ae.FeeConsumed - setupPrice);
        }

        /// <summary>
        /// Reuses the same amount of storage. Should cost 0.
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReusedStorage_FullReuse()
        {
            var snapshot = _snapshotCache.CloneCache();
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1 };

            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(value);

            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using ApplicationEngine applicationEngine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            Debugger debugger = new(applicationEngine);
            applicationEngine.LoadScript(script);
            debugger.StepInto();
            debugger.StepInto();
            debugger.StepInto();
            var setupPrice = applicationEngine.FeeConsumed;
            debugger.Execute();
            Assert.AreEqual(1 * applicationEngine.StoragePrice + (1 << 15) * 30, applicationEngine.FeeConsumed - setupPrice);
        }

        /// <summary>
        /// Reuses one byte and allocates a new one
        /// It should only pay for the second byte.
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReusedStorage_PartialReuse()
        {
            var snapshot = _snapshotCache.CloneCache();
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH1 };

            byte[] script = CreatePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            Debugger debugger = new(ae);
            ae.LoadScript(script);
            debugger.StepInto();
            debugger.StepInto();
            debugger.StepInto();
            var setupPrice = ae.FeeConsumed;
            debugger.StepInto();
            debugger.StepInto();
            Assert.AreEqual((1 + (oldValue.Length / 4) + value.Length - oldValue.Length) * ae.StoragePrice + (1 << 15) * 30, ae.FeeConsumed - setupPrice);
        }

        /// <summary>
        /// Use put for the same key twice.
        /// Pays for 1 extra byte for the first Put and 1 byte for the second basic fee (as value2.length == value1.length).
        /// </summary>
        [TestMethod]
        public void ApplicationEngineReusedStorage_PartialReuseTwice()
        {
            var snapshot = _snapshotCache.CloneCache();
            var key = new byte[] { (byte)OpCode.PUSH1 };
            var oldValue = new byte[] { (byte)OpCode.PUSH1 };
            var value = new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH1 };

            byte[] script = CreateMultiplePutScript(key, value);

            ContractState contractState = TestUtils.GetContract(script);

            StorageKey skey = TestUtils.GetStorageKey(contractState.Id, key);
            StorageItem sItem = TestUtils.GetStorageItem(oldValue);

            snapshot.Add(skey, sItem);
            snapshot.AddContract(script.ToScriptHash(), contractState);

            using ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            Debugger debugger = new(ae);
            ae.LoadScript(script);
            debugger.StepInto(); //push value
            debugger.StepInto(); //push key
            debugger.StepInto(); //syscall Storage.GetContext
            debugger.StepInto(); //syscall Storage.Put
            debugger.StepInto(); //push value
            debugger.StepInto(); //push key
            debugger.StepInto(); //syscall Storage.GetContext
            var setupPrice = ae.FeeConsumed;
            debugger.StepInto(); //syscall Storage.Put
            Assert.AreEqual((sItem.Value.Length / 4 + 1) * ae.StoragePrice + (1 << 15) * 30, ae.FeeConsumed - setupPrice); // = PUT basic fee
        }

        private static byte[] CreateMultiplePutScript(byte[] key, byte[] value, int times = 2)
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

        private static byte[] CreatePutScript(byte[] key, byte[] value)
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
