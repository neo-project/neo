// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_BoundedCall.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System.Collections.Immutable;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine_BoundedCall
    {
        [TestMethod]
        public void CallWithGasLimit_IsInactiveBeforeSmartAccountActivation()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, UInt160.Zero, "test", CallFlags.ReadOnly, 1L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }

        [TestMethod]
        public void CallWithGasLimit_RejectsNonPositiveAndExcessiveLimits()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.RET }, "value", ContractParameterType.Boolean);

            foreach (long limit in new[] { 0L, -1L })
            {
                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), settings: settings, gas: 10_000_000);
                using var script = new ScriptBuilder();
                EmitBoundedCall(script, callee.Hash, "value", CallFlags.ReadOnly, limit);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.Contains("The gas limit must be positive.", engine.FaultException?.ToString() ?? string.Empty);
            }

            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), settings: settings, gas: 10_000_000))
            using (var script = new ScriptBuilder())
            {
                EmitBoundedCall(script, callee.Hash, "value", CallFlags.ReadOnly, 10_000_001L);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.Contains("The bounded contract call gas limit exceeds the remaining transaction budget.", engine.FaultException?.ToString() ?? string.Empty);
            }
        }

        [TestMethod]
        public void CallWithGasLimit_StopsAnUnboundedCalleeWithoutConsumingTransactionBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0 }, "loop", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, callee.Hash, "loop", CallFlags.ReadOnly, 1000L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual("The bounded contract call gas limit has been exhausted.", engine.FaultException?.Message);
            Assert.IsLessThan(1000_00000000L, engine.FeeConsumed);
        }

        [TestMethod]
        public void CallWithGasLimit_PropagatesBudgetThroughAnOrdinaryNestedCall()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0 }, "loop", ContractParameterType.Void);
            using var outerScript = new ScriptBuilder();
            EmitOrdinaryCall(outerScript, callee.Hash, "loop", CallFlags.ReadOnly);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, outer.Hash, "call", CallFlags.ReadOnly, 2_000_000L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual("The bounded contract call gas limit has been exhausted.", engine.FaultException?.Message);
            Assert.AreEqual(callee.Hash, engine.CurrentScriptHash);
        }

        [TestMethod]
        public void CallWithGasLimit_WhitelistedCalleeStillConsumesItsBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0 }, "loop", ContractParameterType.Void);

            using (var setupEngine = CreateEngineWithCommitteeSigner(snapshot))
            {
                NativeContract.Policy.SetWhitelistFeeContract(setupEngine, callee.Hash, "loop", 0, 0);
                setupEngine.SnapshotCache.Commit();
            }

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, callee.Hash, "loop", CallFlags.ReadOnly, 1000L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual("The bounded contract call gas limit has been exhausted.", engine.FaultException?.Message);
        }

        [TestMethod]
        public void CallWithGasLimit_PreservesTheCalleeReturnValue()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.RET }, "value", ContractParameterType.Boolean);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, callee.Hash, "value", CallFlags.ReadOnly, 100L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
        }

        [TestMethod]
        public void CallWithGasLimit_NestedBoundedCallCannotExceedParentBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings();
            var callee = AddContract(snapshot, new byte[] { (byte)OpCode.JMP, 0 }, "loop", ContractParameterType.Void);
            using var outerScript = new ScriptBuilder();
            EmitBoundedCall(outerScript, callee.Hash, "loop", CallFlags.ReadOnly, 2_000_000L);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 1000_00000000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, outer.Hash, "call", CallFlags.ReadOnly, 2_000_000L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.Contains("The bounded contract call gas limit exceeds its parent gas limit.", engine.FaultException?.ToString() ?? string.Empty);
        }

        [TestMethod]
        public void CallWithGasLimit_PropagatesBudgetThroughRuntimeLoadScript()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            byte[] loop = [(byte)OpCode.JMP, 0];
            using var outerScript = new ScriptBuilder();
            EmitLoadedScript(outerScript, loop);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: SmartAccountSettings(), gas: 5_000_000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, outer.Hash, "call", CallFlags.ReadOnly, 2_000_000L);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual("The bounded contract call gas limit has been exhausted.", engine.FaultException?.Message);
            Assert.AreEqual(loop.ToScriptHash(), engine.CurrentScriptHash);
            Assert.IsGreaterThan(0L, engine.GasLeft);
        }

        [TestMethod]
        public void CallWithGasLimit_PreservesLoadedScriptReturnAndCallerBudget()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var outerScript = new ScriptBuilder();
            EmitLoadedScript(outerScript, [(byte)OpCode.PUSH1, (byte)OpCode.RET]);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Boolean);
            var callee = AddContract(snapshot, [(byte)OpCode.PUSH1, (byte)OpCode.RET], "value", ContractParameterType.Boolean);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: SmartAccountSettings(), gas: 10_000_000);
            using var script = new ScriptBuilder();
            EmitBoundedCall(script, outer.Hash, "call", CallFlags.ReadOnly, 2_000_000L);
            EmitOrdinaryCall(script, callee.Hash, "value", CallFlags.ReadOnly);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(2, engine.ResultStack.Count);
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
        }

        [TestMethod]
        public void CallWithGasLimit_PropagatesBudgetThroughContractInitialization()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            byte[] script = [(byte)OpCode.JMP, 0, (byte)OpCode.PUSH1, (byte)OpCode.RET];
            var manifest = new ContractManifest
            {
                Name = "Initialized",
                Groups = [],
                SupportedStandards = [],
                Abi = new ContractAbi
                {
                    Methods =
                    [
                        new ContractMethodDescriptor
                        {
                            Name = ContractBasicMethod.Initialize,
                            Parameters = [],
                            ReturnType = ContractParameterType.Void,
                            Offset = 0,
                            Safe = false
                        },
                        new ContractMethodDescriptor
                        {
                            Name = "call",
                            Parameters = [],
                            ReturnType = ContractParameterType.Void,
                            Offset = 2,
                            Safe = false
                        }
                    ],
                    Events = []
                },
                Permissions = [ContractPermission.DefaultPermission],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                Extra = null
            };
            var callee = AddContract(snapshot, script, manifest);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: SmartAccountSettings(), gas: 5_000_000);
            using var caller = new ScriptBuilder();
            EmitBoundedCall(caller, callee.Hash, "call", CallFlags.ReadOnly, 2_000_000L);
            engine.LoadScript(caller.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual("The bounded contract call gas limit has been exhausted.", engine.FaultException?.Message);
            Assert.IsGreaterThan(0L, engine.GasLeft);
        }

        [TestMethod]
        public void CallWithGasLimit_PropagatesBudgetThroughNativeCallback()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var from = UInt160.Parse("0x0101010101010101010101010101010101010101");
            var callbackManifest = new ContractManifest
            {
                Name = "NativeCallback",
                Groups = [],
                SupportedStandards = [],
                Abi = new ContractAbi
                {
                    Methods =
                    [
                        new ContractMethodDescriptor
                        {
                            Name = "onNEP17Payment",
                            Parameters =
                            [
                                new ContractParameterDefinition { Name = "from", Type = ContractParameterType.Hash160 },
                                new ContractParameterDefinition { Name = "amount", Type = ContractParameterType.Integer },
                                new ContractParameterDefinition { Name = "data", Type = ContractParameterType.Any }
                            ],
                            ReturnType = ContractParameterType.Void,
                            Offset = 0,
                            Safe = false
                        }
                    ],
                    Events = []
                },
                Permissions = [ContractPermission.DefaultPermission],
                Trusts = WildcardContainer<ContractPermissionDescriptor>.Create(),
                Extra = null
            };
            var callback = AddContract(snapshot, [(byte)OpCode.JMP, 0], callbackManifest);
            snapshot.Add(new KeyBuilder(NativeContract.GAS.Id, 20).Add(from), new StorageItem(new AccountState { Balance = 1 }));

            using var outerScript = new ScriptBuilder();
            outerScript.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", from, callback.Hash, 1, null);
            outerScript.Emit(OpCode.DROP);
            outerScript.Emit(OpCode.RET);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Void);

            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(from),
                snapshot,
                settings: SmartAccountSettings(),
                gas: 100_000_000);
            using var caller = new ScriptBuilder();
            EmitBoundedCall(caller, outer.Hash, "call", CallFlags.All, 20_000_000L);
            engine.LoadScript(caller.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.Contains("The bounded contract call gas limit has been exhausted.", engine.FaultException?.ToString() ?? string.Empty);
            Assert.AreEqual(callback.Hash, engine.CurrentScriptHash, engine.FaultException?.ToString());
            Assert.IsGreaterThan(0L, engine.GasLeft);
            Assert.AreEqual(BigInteger.One, NativeContract.GAS.BalanceOf(snapshot, from));
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, callback.Hash));
        }

        [TestMethod]
        public void CallWithGasLimit_WhitelistedCallerCannotEscapeViaRuntimeLoadScript()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var outerScript = new ScriptBuilder();
            EmitLoadedScript(outerScript, [(byte)OpCode.JMP, 0]);
            var outer = AddContract(snapshot, outerScript.ToArray(), "call", ContractParameterType.Void);

            using (var setupEngine = CreateEngineWithCommitteeSigner(snapshot))
            {
                NativeContract.Policy.SetWhitelistFeeContract(setupEngine, outer.Hash, "call", 0, 0);
                setupEngine.SnapshotCache.Commit();
            }

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: SmartAccountSettings(), gas: 10_000_000);
            using var caller = new ScriptBuilder();
            EmitBoundedCall(caller, outer.Hash, "call", CallFlags.ReadOnly, 2_000_000L);
            engine.LoadScript(caller.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual("The bounded contract call gas limit has been exhausted.", engine.FaultException?.Message);
            Assert.AreEqual(new byte[] { (byte)OpCode.JMP, 0 }.ToScriptHash(), engine.CurrentScriptHash);
            Assert.IsGreaterThan(0L, engine.GasLeft);
        }

        [TestMethod]
        public void CallWithGasLimit_BoundsReturnInstructionAfterContextSwitch()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = SmartAccountSettings() with
            {
                Hardforks = SmartAccountSettings().Hardforks.SetItem(Hardfork.HF_Huyao, 0)
            };
            var callee = AddContract(snapshot, [(byte)OpCode.RET], "return", ContractParameterType.Void);
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 10_000_000);
            using var caller = new ScriptBuilder();
            EmitBoundedCall(caller, callee.Hash, "return", CallFlags.ReadOnly, 1L);
            engine.LoadScript(caller.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.AreEqual("The bounded contract call gas limit has been exhausted.", engine.FaultException?.Message);
            Assert.IsGreaterThan(0L, engine.GasLeft);
        }

        private static void EmitLoadedScript(ScriptBuilder script, byte[] loadedScript)
        {
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.PACK);
            script.EmitPush(CallFlags.ReadOnly);
            script.EmitPush(loadedScript);
            script.EmitSysCall(ApplicationEngine.System_Runtime_LoadScript);
            script.Emit(OpCode.RET);
        }

        private static ProtocolSettings SmartAccountSettings()
        {
            return TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_SmartAccountV1, 0)
            };
        }

        private static ContractState AddContract(DataCache snapshot, byte[] script, string method, ContractParameterType returnType)
        {
            var contract = TestUtils.GetContract(script, TestUtils.CreateManifest(method, returnType));
            snapshot.DeleteContract(contract.Hash);
            snapshot.AddContract(contract.Hash, contract);
            return contract;
        }

        private static ContractState AddContract(DataCache snapshot, byte[] script, ContractManifest manifest)
        {
            var contract = TestUtils.GetContract(script, manifest);
            snapshot.DeleteContract(contract.Hash);
            snapshot.AddContract(contract.Hash, contract);
            return contract;
        }

        private static void EmitBoundedCall(ScriptBuilder script, UInt160 contract, string method, CallFlags flags, long gasLimit)
        {
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.PACK);
            script.EmitPush(gasLimit);
            script.EmitPush(flags);
            script.EmitPush(method);
            script.EmitPush(contract);
            script.EmitSysCall(ApplicationEngine.System_Contract_CallWithGasLimit);
        }

        private static void EmitOrdinaryCall(ScriptBuilder script, UInt160 contract, string method, CallFlags flags)
        {
            script.Emit(OpCode.PUSH0);
            script.Emit(OpCode.PACK);
            script.EmitPush(flags);
            script.EmitPush(method);
            script.EmitPush(contract);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);
        }

        private static ApplicationEngine CreateEngineWithCommitteeSigner(DataCache snapshot)
        {
            var committee = NativeContract.NEO.GetCommittee(snapshot);
            var committeeContract = Contract.CreateMultiSigContract((committee.Length / 2) + 1, committee);
            var tx = new Neo.Network.P2P.Payloads.Transaction
            {
                Version = 0,
                Nonce = 1,
                Signers = [new() { Account = committeeContract.ScriptHash, Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = [new Witness { InvocationScript = new byte[1], VerificationScript = committeeContract.Script }],
                Script = new byte[] { (byte)OpCode.NOP },
                NetworkFee = 0,
                SystemFee = 0,
                ValidUntilBlock = 0
            };
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Gorgon, 1).SetItem(Hardfork.HF_Huyao, uint.MaxValue)
            };
            var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot, settings: settings);
            engine.LoadScript(tx.Script);
            return engine;
        }
    }
}
