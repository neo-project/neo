// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Collections.Immutable;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_ApplicationEngine
    {
        private string eventName = null;

        [TestMethod]
        public void TestNotify()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(System.Array.Empty<byte>());
            ApplicationEngine.Notify += Test_Notify1;
            const string notifyEvent = "TestEvent";

            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            Assert.AreEqual(notifyEvent, eventName);

            ApplicationEngine.Notify += Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            Assert.AreEqual(null, eventName);

            eventName = notifyEvent;
            ApplicationEngine.Notify -= Test_Notify1;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            Assert.AreEqual(null, eventName);

            ApplicationEngine.Notify -= Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            Assert.AreEqual(null, eventName);
        }

        private void Test_Notify1(object sender, NotifyEventArgs e)
        {
            eventName = e.EventName;
        }

        private void Test_Notify2(object sender, NotifyEventArgs e)
        {
            eventName = null;
        }

        [TestMethod]
        public void TestCreateDummyBlock()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            ApplicationEngine engine = ApplicationEngine.Run(SyscallSystemRuntimeCheckWitnessHash, snapshotCache, settings: TestProtocolSettings.Default);
            Assert.AreEqual(0u, engine.PersistingBlock.Version);
            Assert.AreEqual(TestBlockchain.TheNeoSystem.GenesisBlock.Hash, engine.PersistingBlock.PrevHash);
            Assert.AreEqual(new UInt256(), engine.PersistingBlock.MerkleRoot);
        }

        [TestMethod]
        public void TestCheckingHardfork()
        {
            var allHardforks = Enum.GetValues(typeof(Hardfork)).Cast<Hardfork>().ToList();

            var builder = ImmutableDictionary.CreateBuilder<Hardfork, uint>();
            builder.Add(Hardfork.HF_Aspidochelone, 0);
            builder.Add(Hardfork.HF_Basilisk, 1);

            var setting = builder.ToImmutable();

            // Check for continuity in configured hardforks
            var sortedHardforks = setting.Keys
                .OrderBy(h => allHardforks.IndexOf(h))
                .ToList();

            for (int i = 0; i < sortedHardforks.Count - 1; i++)
            {
                int currentIndex = allHardforks.IndexOf(sortedHardforks[i]);
                int nextIndex = allHardforks.IndexOf(sortedHardforks[i + 1]);

                // If they aren't consecutive, return false.
                var inc = nextIndex - currentIndex;
                Assert.AreEqual(1, inc);
            }

            // Check that block numbers are not higher in earlier hardforks than in later ones
            for (int i = 0; i < sortedHardforks.Count - 1; i++)
            {
                Assert.IsFalse(setting[sortedHardforks[i]] > setting[sortedHardforks[i + 1]]);
            }
        }

        [TestMethod]
        public void TestSystem_Contract_Call_Permissions()
        {
            UInt160 scriptHash;
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();

            // Setup: put a simple contract to the storage.
            using (var script = new ScriptBuilder())
            {
                // Push True on stack and return.
                script.EmitPush(true);
                script.Emit(OpCode.RET);

                // Mock contract and put it to the Managemant's storage.
                scriptHash = script.ToArray().ToScriptHash();

                snapshotCache.DeleteContract(scriptHash);
                var contract = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("test", ContractParameterType.Any));
                contract.Manifest.Abi.Methods = new[]
                {
                    new ContractMethodDescriptor
                    {
                        Name = "disallowed",
                        Parameters = new ContractParameterDefinition[]{}
                    },
                    new ContractMethodDescriptor
                    {
                        Name = "test",
                        Parameters = new ContractParameterDefinition[]{}
                    }
                };
                snapshotCache.AddContract(scriptHash, contract);
            }

            // Disallowed method call.
            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, null, ProtocolSettings.Default))
            using (var script = new ScriptBuilder())
            {
                // Build call script calling disallowed method.
                script.EmitDynamicCall(scriptHash, "disallowed");

                // Mock executing state to be a contract-based.
                engine.LoadScript(script.ToArray());
                engine.CurrentContext.GetState<ExecutionContextState>().Contract = new()
                {
                    Manifest = new()
                    {
                        Abi = new() { },
                        Permissions = new ContractPermission[]
                        {
                            new ContractPermission
                            {
                                Contract = ContractPermissionDescriptor.Create(scriptHash),
                                Methods = WildcardContainer<string>.Create(new string[]{"test"}) // allowed to call only "test" method of the target contract.
                            }
                        }
                    }
                };
                var currentScriptHash = engine.EntryScriptHash;

                Assert.AreEqual("", engine.GetEngineStackInfoOnFault());
                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.IsTrue(engine.FaultException.ToString().Contains($"Cannot Call Method disallowed Of Contract {scriptHash.ToString()}"));
                string traceback = engine.GetEngineStackInfoOnFault();
                Assert.IsTrue(traceback.Contains($"Cannot Call Method disallowed Of Contract {scriptHash.ToString()}"));
                Assert.IsTrue(traceback.Contains("CurrentScriptHash"));
                Assert.IsTrue(traceback.Contains("EntryScriptHash"));
                Assert.IsTrue(traceback.Contains("InstructionPointer"));
                Assert.IsTrue(traceback.Contains("OpCode SYSCALL, Script Length="));
            }

            // Allowed method call.
            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, null, ProtocolSettings.Default))
            using (var script = new ScriptBuilder())
            {
                // Build call script.
                script.EmitDynamicCall(scriptHash, "test");

                // Mock executing state to be a contract-based.
                engine.LoadScript(script.ToArray());
                engine.CurrentContext.GetState<ExecutionContextState>().Contract = new()
                {
                    Manifest = new()
                    {
                        Abi = new() { },
                        Permissions = new ContractPermission[]
                        {
                            new ContractPermission
                            {
                                Contract = ContractPermissionDescriptor.Create(scriptHash),
                                Methods = WildcardContainer<string>.Create(new string[]{"test"}) // allowed to call only "test" method of the target contract.
                            }
                        }
                    }
                };
                var currentScriptHash = engine.EntryScriptHash;

                Assert.AreEqual(VMState.HALT, engine.Execute());
                Assert.AreEqual(1, engine.ResultStack.Count);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(VM.Types.Boolean));
                var res = (VM.Types.Boolean)engine.ResultStack.Pop();
                Assert.IsTrue(res.GetBoolean());
            }
        }
    }
}
