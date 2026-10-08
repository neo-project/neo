// Copyright (C) 2015-2026 The Neo Project.
//
// UT_SmartAccountModulePolicy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Security.Cryptography;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_SmartAccountModulePolicy
    {
        private static readonly UInt160 Account = UInt160.Parse("0x0101010101010101010101010101010101010101");
        private static ContractMethodDescriptor Method(string name, ContractParameterType result, bool safe, params ContractParameterType[] parameters) =>
            new()
            {
                Name = name,
                ReturnType = result,
                Safe = safe,
                Offset = 0,
                Parameters = parameters.Select((type, i) => new ContractParameterDefinition { Name = $"p{i}", Type = type }).ToArray()
            };

        private static ContractState Module(SmartAccountModuleKind kind = SmartAccountModuleKind.Verifier,
            byte[] marker = null, Action<ScriptBuilder> domains = null)
        {
            using var script = new ScriptBuilder();
            foreach (byte op in marker ?? [(byte)OpCode.PUSHF, (byte)OpCode.RET]) script.Emit((OpCode)op);
            int domainsOffset = script.Length;
            script.Emit(OpCode.DROP);
            if (domains is null) script.EmitPush(new byte[32]).EmitPush(1).Emit(OpCode.PACK);
            else domains(script);
            script.Emit(OpCode.RET);
            var contract = TestUtils.GetContract(script.ToArray());
            var abi = new[] {
                Method("supportsComposition", ContractParameterType.Boolean, true),
                Method("clearAccount", ContractParameterType.Void, false, ContractParameterType.Hash160),
                Method("postExecute", ContractParameterType.Void, false, ContractParameterType.Hash160, ContractParameterType.Array, ContractParameterType.Any),
                Method(kind == SmartAccountModuleKind.Verifier ? "validateSignature" : "preExecute",
                    kind == SmartAccountModuleKind.Verifier ? ContractParameterType.Boolean : ContractParameterType.Void,
                    false, ContractParameterType.Hash160, ContractParameterType.Array)
            };
            if (kind == SmartAccountModuleKind.Verifier)
            {
                var domainMethod = Method("getSignerDomains", ContractParameterType.Array, true, ContractParameterType.Hash160);
                domainMethod.Offset = domainsOffset;
                abi = [.. abi, domainMethod];
            }
            contract.Manifest.Abi.Methods = abi;
            return contract;
        }

        private static void Install(DataCache snapshot, ContractState contract)
        {
            snapshot.DeleteContract(contract.Hash); snapshot.AddContract(contract.Hash, contract);
        }
        private static ApplicationEngine Engine(DataCache snapshot, bool huyao = true, long gas = 2_000_000_000)
        {
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_SmartAccountV1, 0)
                    .SetItem(Hardfork.HF_Huyao, huyao ? 0U : uint.MaxValue)
            };
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: gas);
            engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => state.ScriptHash = NativeContract.ContractManagement.Hash);
            return engine;
        }
        private static void Propagate<T>(ApplicationEngine engine, ContractTask<T> task)
        {
            void Finish() { try { _ = task.GetAwaiter().GetResult(); } catch (Exception error) { engine.Throw(error); } }
            if (task.GetAwaiter().IsCompleted) Finish(); else task.GetAwaiter().OnCompleted(Finish);
        }

        [TestMethod]
        public void MatchesIndependentNefManifestAndHashVector()
        {
            var vector = (JObject)JToken.Parse(System.IO.File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory,
                "SmartContract", "Native", "TestFile", "smartaccount-binding-v1.json")));
            var contract = new ContractState
            {
                Hash = Account,
                Id = 1,
                Nef = NefFile.Parse(Convert.FromBase64String(vector["nefBase64"].GetString())),
                Manifest = ContractManifest.FromJson((JObject)vector["manifest"])
            };
            Assert.AreEqual(vector["canonicalManifestUtf8"].GetString(), System.Text.Encoding.UTF8.GetString(
                SmartAccountCanonicalJson.Serialize(contract.Manifest.ToJson())));
            Assert.AreEqual(vector["codeHashWireHex"].GetString(), Convert.ToHexStringLower(SmartAccountModulePolicy.GetCodeHash(contract).ToArray()));
        }

        [TestMethod]
        public void AcceptsSafeOperationCallbacksButNotSafeCleanup()
        {
            foreach (var kind in new[] { SmartAccountModuleKind.Verifier, SmartAccountModuleKind.Hook })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module(kind);
                foreach (var method in contract.Manifest.Abi.Methods.Where(m => m.Name is "preExecute" or "postExecute" or "validateSignature"))
                    method.Safe = true;
                Install(snapshot, contract);
                Assert.IsNotNull(SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, kind));
            }
        }

        [TestMethod]
        public void RechecksIdentityAndRejectsInvalidInputsBeforeScheduling()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module(); Install(snapshot, contract);
            var binding = SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, SmartAccountModuleKind.Verifier);
            contract.Manifest.Extra = new JObject { ["changed"] = true }; Install(snapshot, contract);
            using var engine = Engine(snapshot);
            var original = engine.CurrentContext;
            var task = SmartAccountModulePolicy.DiscoverAsync(engine, binding, SmartAccountModuleKind.Verifier, false);
            Assert.IsTrue(task.GetAwaiter().IsCompleted);
            Assert.ThrowsExactly<InvalidOperationException>(() => task.GetAwaiter().GetResult());
            Assert.AreSame(original, engine.CurrentContext);
            var missingEngine = SmartAccountModulePolicy.DiscoverAsync(null, binding, SmartAccountModuleKind.Verifier, false);
            Assert.ThrowsExactly<ArgumentNullException>(() => missingEngine.GetAwaiter().GetResult());
            var missingBinding = SmartAccountModulePolicy.DiscoverAsync(engine, null, SmartAccountModuleKind.Verifier, false);
            Assert.ThrowsExactly<ArgumentNullException>(() => missingBinding.GetAwaiter().GetResult());
            var missingId = SmartAccountModulePolicy.ReadSignerDomainsAsync(engine, null, binding);
            Assert.ThrowsExactly<ArgumentNullException>(() => missingId.GetAwaiter().GetResult());
            var zeroId = SmartAccountModulePolicy.ReadSignerDomainsAsync(engine, UInt160.Zero, binding);
            Assert.ThrowsExactly<ArgumentException>(() => zeroId.GetAwaiter().GetResult());
            Assert.ThrowsExactly<ArgumentNullException>(() => SmartAccountModulePolicy.GetCodeHash(null));
            Assert.ThrowsExactly<ArgumentNullException>(() => SmartAccountModulePolicy.Inspect(null, Account, SmartAccountModuleKind.Verifier));
            Assert.ThrowsExactly<ArgumentNullException>(() => SmartAccountModulePolicy.Inspect(snapshot, null, SmartAccountModuleKind.Verifier));
        }

        [TestMethod]
        public void ComputesCompleteNefSeparatorAndCanonicalManifestDigest()
        {
            var contract = Module();
            contract.Manifest.Extra = new JObject { ["z"] = -0.0, ["a"] = new JObject { ["2"] = "é", ["10"] = 1e-6 } };
            var nef = contract.Nef.ToArray();
            byte[] expected = SHA256.HashData([.. nef, 0, .. SmartAccountCanonicalJson.Serialize(contract.Manifest.ToJson())]);
            var original = SmartAccountModulePolicy.GetCodeHash(contract);
            Assert.AreSequenceEqual(expected, original.ToArray());
            contract.Manifest.Extra = new JObject { ["a"] = new JObject { ["10"] = 1e-6, ["2"] = "é" }, ["z"] = 0.0 };
            Assert.AreEqual(original, SmartAccountModulePolicy.GetCodeHash(contract));
            contract.Manifest.Extra["z"] = 1;
            Assert.AreNotEqual(original, SmartAccountModulePolicy.GetCodeHash(contract));
            var before = SmartAccountModulePolicy.GetCodeHash(contract);
            contract.Nef.Source = "changed";
            contract.Nef.CheckSum = NefFile.ComputeChecksum(contract.Nef);
            Assert.AreNotEqual(before, SmartAccountModulePolicy.GetCodeHash(contract));
            before = SmartAccountModulePolicy.GetCodeHash(contract);
            contract.Manifest.Abi.Methods = contract.Manifest.Abi.Methods.Reverse().ToArray();
            Assert.AreNotEqual(before, SmartAccountModulePolicy.GetCodeHash(contract));
        }

        [TestMethod]
        public void InspectsBothRolesAndRechecksEveryStoredBinding()
        {
            foreach (var kind in new[] { SmartAccountModuleKind.Verifier, SmartAccountModuleKind.Hook })
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module(kind); Install(snapshot, contract);
                var binding = SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, kind);
                Assert.AreEqual(contract.Hash, binding.Contract);
                Assert.AreEqual(binding.CodeHash, SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, kind, binding).CodeHash);
                Assert.ThrowsExactly<InvalidOperationException>(() => SmartAccountModulePolicy.Inspect(snapshot, Account, kind, binding));
                contract.Manifest.Extra = new JObject { ["changed"] = true }; Install(snapshot, contract);
                Assert.ThrowsExactly<InvalidOperationException>(() => SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, kind, binding));
            }
        }

        [TestMethod]
        public void RejectsMissingBlockedNativeAndInvalidRolesBeforeDiscovery()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module(); Install(snapshot, contract);
            foreach (var hash in new[] { UInt160.Zero, Account, NativeContract.GAS.Hash, SmartAccountProtocol.ServiceHash })
                Assert.ThrowsExactly<InvalidOperationException>(() => SmartAccountModulePolicy.Inspect(snapshot, hash, SmartAccountModuleKind.Verifier));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, (SmartAccountModuleKind)77));
            snapshot.Add(new KeyBuilder(NativeContract.Policy.Id, 15).Add(contract.Hash), new StorageItem(System.Array.Empty<byte>()));
            Assert.ThrowsExactly<InvalidOperationException>(() => SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, SmartAccountModuleKind.Verifier));
        }

        [TestMethod]
        public void RejectsEveryLifecycleSignatureSafetyAndDuplicateArityDefect()
        {
            foreach (var kind in new[] { SmartAccountModuleKind.Verifier, SmartAccountModuleKind.Hook })
                foreach (string name in new[] { "supportsComposition", "clearAccount", "postExecute", kind == SmartAccountModuleKind.Verifier ? "validateSignature" : "preExecute" })
                    foreach (int defect in Enumerable.Range(0, 6))
                    {
                        var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module(kind);
                        var descriptor = contract.Manifest.Abi.Methods.Single(m => m.Name == name);
                        switch (defect)
                        {
                            case 0: contract.Manifest.Abi.Methods = contract.Manifest.Abi.Methods.Where(m => m != descriptor).ToArray(); break;
                            case 1: descriptor.ReturnType = ContractParameterType.Any; break;
                            case 2: descriptor.Parameters = [.. descriptor.Parameters, new() { Name = "extra", Type = ContractParameterType.Any }]; break;
                            case 3:
                                if (descriptor.Parameters.Length == 0) descriptor.Safe = false;
                                else descriptor.Parameters[0].Type = ContractParameterType.ByteArray;
                                break;
                            case 4:
                                if (name is "validateSignature" or "preExecute" or "postExecute") descriptor.Offset = contract.Script.Length;
                                else descriptor.Safe = !descriptor.Safe;
                                break;
                            case 5: contract.Manifest.Abi.Methods = [.. contract.Manifest.Abi.Methods, descriptor]; break;
                        }
                        Install(snapshot, contract);
                        Assert.ThrowsExactly<InvalidOperationException>(() => SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, kind), $"{kind}/{name}/{defect}");
                    }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DiscoversRealLeafAndCompositeCallbacksWithoutCoercion(bool huyao)
        {
            foreach (bool composite in new[] { false, true })
                foreach (var kind in new[] { SmartAccountModuleKind.Verifier, SmartAccountModuleKind.Hook })
                {
                    var snapshot = TestBlockchain.GetTestSnapshotCache();
                    var contract = Module(kind, [(byte)(composite ? OpCode.PUSHT : OpCode.PUSHF), (byte)OpCode.RET]); Install(snapshot, contract);
                    var binding = SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, kind);
                    using var engine = Engine(snapshot, huyao);
                    var task = SmartAccountModulePolicy.DiscoverAsync(engine, binding, kind, false); Propagate(engine, task);
                    Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
                    Assert.AreEqual(composite, task.GetAwaiter().GetResult());
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
        }

        [TestMethod]
        public void RejectsTruthyMarkerNestedCompositeAndMissingLeafDiscovery()
        {
            foreach (int defect in Enumerable.Range(0, 5))
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache();
                var marker = defect switch { 0 => OpCode.PUSH1, 1 => OpCode.PUSHNULL, 2 => OpCode.PUSHT, _ => OpCode.PUSHF };
                var contract = Module(marker: [(byte)marker, (byte)OpCode.RET]);
                var domains = contract.Manifest.Abi.Methods.Single(m => m.Name == "getSignerDomains");
                if (defect == 3) contract.Manifest.Abi.Methods = contract.Manifest.Abi.Methods.Where(m => m != domains).ToArray();
                if (defect == 4) domains.Safe = false;
                Install(snapshot, contract);
                var binding = SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, SmartAccountModuleKind.Verifier);
                using var engine = Engine(snapshot);
                var task = SmartAccountModulePolicy.DiscoverAsync(engine, binding, SmartAccountModuleKind.Verifier, true); Propagate(engine, task);
                Assert.AreEqual(VMState.FAULT, engine.Execute(), $"defect {defect}");
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ReadsOwnedSignerDomainsFromRealVmCallbacks(bool huyao)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module(); Install(snapshot, contract);
            var binding = SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, SmartAccountModuleKind.Verifier);
            using var engine = Engine(snapshot, huyao);
            var task = SmartAccountModulePolicy.ReadSignerDomainsAsync(engine, Account, binding); Propagate(engine, task);
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            var result = task.GetAwaiter().GetResult();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(UInt256.Zero, result[0]);
            Assert.AreEqual(0, engine.ResultStack.Count);
        }

        [TestMethod]
        public void RejectsMalformedEmptyMutableOrDuplicateSignerDomains()
        {
            Action<ScriptBuilder>[] invalid = [
                s => s.Emit(OpCode.PUSHNULL), s => s.Emit(OpCode.NEWARRAY0),
                s => s.EmitPush(1).EmitPush(1).Emit(OpCode.PACK),
                s => s.EmitPush(new byte[31]).EmitPush(1).Emit(OpCode.PACK),
                s => s.EmitPush(new byte[32]).Emit(OpCode.DUP).EmitPush(2).Emit(OpCode.PACK),
                s => s.EmitPush(new byte[32]).Emit(OpCode.CONVERT, new byte[] { (byte)StackItemType.Buffer }).EmitPush(1).Emit(OpCode.PACK),
                s => s.EmitPush(new byte[32]).EmitPush(1).Emit(OpCode.PACK).Emit(OpCode.CONVERT, new byte[] { (byte)StackItemType.Struct })
            ];
            foreach (var emit in invalid)
            {
                var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module(domains: emit); Install(snapshot, contract);
                var binding = SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, SmartAccountModuleKind.Verifier);
                using var engine = Engine(snapshot);
                var task = SmartAccountModulePolicy.ReadSignerDomainsAsync(engine, Account, binding); Propagate(engine, task);
                Assert.AreEqual(VMState.FAULT, engine.Execute());
            }
        }

        [TestMethod]
        public void DiscoveryCannotWriteNotifyOrExceedTheFixedBudget()
        {
            foreach (int defect in Enumerable.Range(0, 4))
            {
                using var script = new ScriptBuilder();
                if (defect == 0) script.EmitPush(1).EmitPush("key").EmitSysCall(ApplicationEngine.System_Storage_GetContext).EmitSysCall(ApplicationEngine.System_Storage_Put);
                if (defect == 1) script.Emit(OpCode.NEWARRAY0).EmitPush("event").EmitSysCall(ApplicationEngine.System_Runtime_Notify);
                if (defect == 2) script.EmitPush(250_000_001).EmitSysCall(ApplicationEngine.System_Runtime_BurnGas);
                if (defect == 3) script.Emit(OpCode.ABORT);
                script.Emit(OpCode.PUSHF).Emit(OpCode.RET);
                var snapshot = TestBlockchain.GetTestSnapshotCache(); var contract = Module();
                contract.Nef.Script = script.ToArray(); contract.Nef.CheckSum = NefFile.ComputeChecksum(contract.Nef);
                // This fixture faults in supportsComposition; keep all other ABI offsets in range.
                contract.Manifest.Abi.Methods.Single(m => m.Name == "getSignerDomains").Offset = 0;
                Install(snapshot, contract);
                var binding = SmartAccountModulePolicy.Inspect(snapshot, contract.Hash, SmartAccountModuleKind.Verifier);
                using var engine = Engine(snapshot);
                var task = SmartAccountModulePolicy.DiscoverAsync(engine, binding, SmartAccountModuleKind.Verifier, false); Propagate(engine, task);
                Assert.AreEqual(VMState.FAULT, engine.Execute(), $"defect {defect}");
                Assert.IsNull(snapshot.TryGet(new StorageKey { Id = contract.Id, Key = "key"u8.ToArray() }));
                Assert.IsTrue(engine.Notifications is null || engine.Notifications.Count == 0);
            }
        }
    }
}
