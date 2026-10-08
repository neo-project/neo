// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.Modules.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        private static void CoreCall(ScriptBuilder script, string method, int count)
        {
            script.EmitPush(count).Emit(OpCode.PACK).EmitPush(CallFlags.All).EmitPush(method)
                .EmitPush(NativeContract.AccountManagement.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call);
        }
        private static void AssertPhase(ScriptBuilder script, string role, string phase)
        {
            script.EmitPush(phase).EmitSysCall(ApplicationEngine.System_Runtime_GetExecutingScriptHash)
                .EmitPush(role).Emit(OpCode.LDARG0);
            // Safe context queries can be called from ReadOnly verifier validation.
            script.EmitPush(4).Emit(OpCode.PACK).EmitPush(CallFlags.ReadOnly).EmitPush("hasModuleContext")
                .EmitPush(NativeContract.AccountManagement.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call).Emit(OpCode.ASSERT);
        }
        private static ContractState ModuleFixture(DataCache snapshot, int number, bool hook = false, bool composite = false,
            bool abortPost = false, bool abortCleanup = false, int domain = -1, bool allowDestroy = false, UInt160 destroyRoot = null,
            byte[] replacementManifest = null, UInt160 delegatedChild = null)
        {
            string role = hook ? "hook" : "verifier";
            using var script = new ScriptBuilder(); List<ContractMethodDescriptor> methods = new();
            void Method(string name, ContractParameterType result, bool safe, Action body, params ContractParameterType[] args)
            {
                methods.Add(new()
                {
                    Name = name,
                    ReturnType = result,
                    Safe = safe,
                    Offset = script.Length,
                    Parameters = args.Select((type, index) => new ContractParameterDefinition { Name = $"arg{index}", Type = type }).ToArray()
                });
                if (args.Length != 0) script.Emit(OpCode.INITSLOT, new byte[] { 0, (byte)args.Length });
                body(); script.Emit(OpCode.RET);
            }
            void Delegate(string method, int count, bool returnsValue)
            {
                for (int i = count - 1; i >= 0; i--) script.Emit((OpCode)((byte)OpCode.LDARG0 + i));
                script.EmitPush(count).Emit(OpCode.PACK).EmitPush(CallFlags.All).EmitPush(method)
                    .EmitPush(delegatedChild).EmitSysCall(ApplicationEngine.System_Contract_Call);
                if (!returnsValue) script.Emit(OpCode.DROP);
            }
            Method("supportsComposition", ContractParameterType.Boolean, true, () => script.EmitPush(number).Emit(OpCode.DROP).EmitPush(composite));
            Method(hook ? "preExecute" : "validateSignature", hook ? ContractParameterType.Void : ContractParameterType.Boolean, false, () =>
            {
                AssertPhase(script, role, hook ? "preExecute" : "validation");
                if (delegatedChild is not null) Delegate(hook ? "preExecute" : "validateSignature", 2, !hook);
                else if (!hook) script.EmitPush(true);
            }, ContractParameterType.Hash160, ContractParameterType.Array);
            Method("postExecute", ContractParameterType.Void, false, () =>
            {
                AssertPhase(script, role, "postExecute"); if (abortPost) script.Emit(OpCode.ABORT);
                if (delegatedChild is not null) Delegate("postExecute", 3, false);
            }, ContractParameterType.Hash160, ContractParameterType.Array, ContractParameterType.Any);
            Method("clearAccount", ContractParameterType.Void, false, () =>
            {
                AssertPhase(script, role, "cleanup");
                script.Emit(OpCode.LDARG0).EmitSysCall(ApplicationEngine.System_Storage_GetContext).EmitSysCall(ApplicationEngine.System_Storage_Delete);
                if (composite) { script.Emit(OpCode.LDARG0); CoreCall(script, hook ? "clearHookDependencies" : "clearVerifierDependencies", 1); script.Emit(OpCode.DROP); }
                if (abortCleanup) script.Emit(OpCode.ABORT);
            }, ContractParameterType.Hash160);
            if (!hook) Method("getSignerDomains", ContractParameterType.Array, true, () =>
            {
                script.EmitPush(Enumerable.Repeat((byte)(domain < 0 ? number : domain), 32).ToArray()).EmitPush(1).Emit(OpCode.PACK);
            }, ContractParameterType.Hash160);
            Method("configure", ContractParameterType.Any, false, () =>
            {
                AssertPhase(script, role, "configuration");
                script.Emit(OpCode.LDARG1).Emit(OpCode.LDARG0).EmitSysCall(ApplicationEngine.System_Storage_GetContext).EmitSysCall(ApplicationEngine.System_Storage_Put);
                if (destroyRoot is not null)
                {
                    if (replacementManifest is null) script.EmitDynamicCall(destroyRoot, "destroySelf").Emit(OpCode.DROP);
                    else script.EmitDynamicCall(destroyRoot, "replaceManifest", replacementManifest).Emit(OpCode.DROP);
                }
                script.EmitPush(false);
            }, ContractParameterType.Hash160, ContractParameterType.Integer);
            if (allowDestroy) Method("destroySelf", ContractParameterType.Void, false, () =>
                script.EmitDynamicCall(NativeContract.ContractManagement.Hash, "destroy").Emit(OpCode.DROP));
            if (allowDestroy) Method("replaceManifest", ContractParameterType.Void, false, () =>
            {
                script.Emit(OpCode.LDARG0).Emit(OpCode.PUSHNULL).EmitPush(2).Emit(OpCode.PACK).EmitPush(CallFlags.All)
                    .EmitPush("update").EmitPush(NativeContract.ContractManagement.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call).Emit(OpCode.DROP);
            }, ContractParameterType.ByteArray);
            if (composite) Method("roster", ContractParameterType.Any, false, () =>
            {
                AssertPhase(script, role, "configuration"); script.Emit(OpCode.LDARG1).Emit(OpCode.LDARG0);
                CoreCall(script, hook ? "setHookDependencies" : "setVerifierDependencies", 2);
            }, ContractParameterType.Hash160, ContractParameterType.Array);
            if (composite) Method("clearRoster", ContractParameterType.Any, false, () =>
            {
                AssertPhase(script, role, "configuration"); script.Emit(OpCode.LDARG0);
                CoreCall(script, hook ? "clearHookDependencies" : "clearVerifierDependencies", 1);
            }, ContractParameterType.Hash160);
            var result = TestUtils.GetContract(script.ToArray()); result.Id = number;
            result.Manifest.Name = composite ? (hook ? "MultiHook" : "MultiSigVerifier") : $"Leaf{number}";
            result.Manifest.Abi.Methods = methods.ToArray();
            result.Manifest.Extra = new JObject
            {
                ["smartAccount"] = new JObject
                {
                    ["abiVersion"] = 2,
                    ["configurationMethods"] = composite ? new JArray("configure", "roster", "clearRoster") : new JArray("configure")
                }
            };
            snapshot.AddContract(result.Hash, result); return result;
        }
        private static UInt160 RegisterWithModules(DataCache snapshot, ContractState verifier = null, ContractState hook = null) =>
            new(Success(snapshot, "registerAccount", [Custody, new byte[32], verifier?.Hash ?? UInt160.Zero, hook?.Hash ?? UInt160.Zero, Recovery]).GetSpan());
        private static StorageKey ConfigKey(ContractState module, UInt160 id) => new() { Id = module.Id, Key = id.ToArray() };
        private static void Fault(DataCache snapshot, string method, object[] args, ulong time = 1000, UInt160[] signers = null)
        {
            using var engine = Invoke(snapshot, method, args, time, signers);
            Assert.AreEqual(VMState.FAULT, engine.State, method);
            Assert.AreEqual(0, engine.Notifications.Count);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void ChildConfigurationCannotCommitMutatedRoot(bool hook, bool update)
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 131, hook, composite: true, allowDestroy: true);
            var originalPin = SmartAccountModulePolicy.GetCodeHash(root);
            var manifest = JObject.Parse(root.Manifest.ToJson().ToString()); manifest["extra"]["mutation"] = "post-callback";
            var leaf = ModuleFixture(snapshot, 132, hook, destroyRoot: root.Hash,
                replacementManifest: update ? System.Text.Encoding.UTF8.GetBytes(manifest.ToString()) : null);
            var id = RegisterWithModules(snapshot, hook ? null : root, hook ? root : null);
            string role = hook ? "hook" : "verifier", route = hook ? "callHookChild" : "callVerifierChild";
            object[] args = [id, leaf.Hash, "configure", new object[] { 7 }];
            Success(snapshot, route, args);
            using var engine = Invoke(snapshot, route, args, 1000 + SmartAccountState.ModuleChangeDelayMs);
            Assert.AreEqual(VMState.FAULT, engine.State, "A child must not commit a changed or destroyed root binding.");
            Assert.Contains(update ? "code identity has changed" : "module is zero, native or blocked", engine.FaultException.ToString());
            Assert.AreEqual(0, engine.Notifications.Count);
            Assert.IsNotNull(NativeContract.ContractManagement.GetContract(snapshot, root.Hash));
            Assert.AreEqual(originalPin, SmartAccountModulePolicy.GetCodeHash(NativeContract.ContractManagement.GetContract(snapshot, root.Hash)));
            Assert.IsFalse(snapshot.Contains(ConfigKey(leaf, id)));
            Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, root.Hash));
            Assert.AreEqual(BigInteger.Zero, ((Array)Success(snapshot, "getAccount", [id]))[8].GetInteger());
            Assert.IsInstanceOfType<Array>(Success(snapshot, "getPendingModuleCall", [id, role]));
            var registry = (Array)Success(snapshot, "getModuleDependencies", [id, role]);
            Assert.AreEqual(0, ((Array)registry[1]).Count);
        }

        [TestMethod]
        public void NativeModuleCallbacksReceiveExactPhaseAndPostFaultRollsBack()
        {
            foreach (bool abort in new[] { false, true })
            {
                var snapshot = Snapshot(); var verifier = ModuleFixture(snapshot, 111, abortPost: abort); var hook = ModuleFixture(snapshot, 112, hook: true);
                var id = RegisterWithModules(snapshot, verifier, hook);
                var op = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
                if (abort)
                {
                    Fault(snapshot, "executeUserOp", [id, op]);
                    Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
                }
                else
                {
                    Assert.IsInstanceOfType<ByteString>(Success(snapshot, "executeUserOp", [id, op], signers: []));
                    Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
                }
                Assert.IsFalse(Success(snapshot, "hasModuleContext", [id, "verifier", verifier.Hash, "validation"]).GetBoolean());
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DelayedConfigurationPinsIntentAndCommitsFalseResult(bool hook)
        {
            var snapshot = Snapshot(); var module = ModuleFixture(snapshot, 113, hook: hook);
            var id = RegisterWithModules(snapshot, hook ? null : module, hook ? module : null);
            string route = hook ? "callHook" : "callVerifier", role = hook ? "hook" : "verifier";
            object[] args = [id, "configure", new object[] { 7 }];
            Assert.IsFalse(Success(snapshot, route, args).GetBoolean());
            Assert.IsFalse(snapshot.Contains(ConfigKey(module, id)));
            var intent = (Array)Success(snapshot, "getPendingModuleCall", [id, role]); Assert.AreEqual(10, intent.Count);
            Fault(snapshot, route, args, 1001);
            Fault(snapshot, route, [id, "configure", new object[] { 8 }], 1000 + SmartAccountState.ModuleChangeDelayMs);
            Assert.IsFalse(Success(snapshot, route, args, 1000 + SmartAccountState.ModuleChangeDelayMs).GetBoolean());
            Assert.AreEqual(new BigInteger(7), new BigInteger(snapshot[ConfigKey(module, id)].Value.Span));
            Assert.IsInstanceOfType<Null>(Success(snapshot, "getPendingModuleCall", [id, role]));
            Assert.AreEqual(BigInteger.One, ((Array)Success(snapshot, "getAccount", [id]))[8].GetInteger());
            Fault(snapshot, route, [id, "clearAccount", new object[0]]);
            Fault(snapshot, route, args, signers: []);
            Success(snapshot, route, args);
            Success(snapshot, "cancelModuleCall", [id, role]);
            Assert.IsInstanceOfType<Null>(Success(snapshot, "getPendingModuleCall", [id, role]));
            Fault(snapshot, "cancelModuleCall", [id, role]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompositeBootstrapPublicationAndRootRemovalCleanAllEnrolledLeaves(bool hook)
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 114, hook, composite: true); var leaf = ModuleFixture(snapshot, 115, hook);
            var id = RegisterWithModules(snapshot, hook ? null : root, hook ? root : null);
            string role = hook ? "hook" : "verifier", route = hook ? "callHook" : "callVerifier";
            var child = new object[] { id, leaf.Hash, "configure", new object[] { 9 } };
            Success(snapshot, route + "Child", child);
            Success(snapshot, route + "Child", child, 1000 + SmartAccountState.ModuleChangeDelayMs);
            var registry = (Array)Success(snapshot, "getModuleDependencies", [id, role]);
            Assert.AreEqual(1, ((Array)registry[1]).Count); Assert.AreEqual(0, ((Array)registry[2]).Count);
            Fault(snapshot, hook ? "setHookDependencies" : "setVerifierDependencies", [id, new object[] { leaf.Hash }]);
            var publish = new object[] { id, "roster", new object[] { new object[] { leaf.Hash } } };
            Success(snapshot, route, publish);
            Success(snapshot, route, publish, 1000 + SmartAccountState.ModuleChangeDelayMs);
            registry = (Array)Success(snapshot, "getModuleDependencies", [id, role]);
            Assert.AreEqual(1, ((Array)registry[2]).Count);
            Success(snapshot, hook ? "proposeHook" : "proposeVerifier", [id, UInt160.Zero]);
            Success(snapshot, hook ? "activateHook" : "activateVerifier", [id], 1000 + SmartAccountState.ModuleChangeDelayMs);
            Assert.IsFalse(snapshot.Contains(ConfigKey(leaf, id)));
            registry = (Array)Success(snapshot, "getModuleDependencies", [id, role]);
            Assert.AreEqual(0, ((Array)registry[1]).Count); Assert.AreEqual(0, ((Array)registry[2]).Count);
            Assert.IsInstanceOfType<Null>(registry[0]);
        }

        [TestMethod]
        public void VerifierDomainOverlapAndNestedCompositionCannotPublish()
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 121, composite: true);
            var first = ModuleFixture(snapshot, 122, domain: 7); var duplicate = ModuleFixture(snapshot, 123, domain: 7);
            var nested = ModuleFixture(snapshot, 124, composite: true); var id = RegisterWithModules(snapshot, root);
            Fault(snapshot, "callVerifierChild", [id, nested.Hash, "configure", new object[] { 1 }]);
            Fault(snapshot, "callVerifierChild", [id, root.Hash, "configure", new object[] { 1 }]);
            foreach (var roster in new[] { new object[] { first.Hash, duplicate.Hash }, new object[] { first.Hash, first.Hash }, new object[] { root.Hash }, new object[] { NativeContract.GAS.Hash } })
            {
                object[] args = [id, "roster", new object[] { roster }];
                Success(snapshot, "callVerifier", args);
                Fault(snapshot, "callVerifier", args, 1000 + SmartAccountState.ModuleChangeDelayMs);
                var registry = (Array)Success(snapshot, "getModuleDependencies", [id, "verifier"]);
                Assert.AreEqual(0, ((Array)registry[1]).Count); Assert.AreEqual(0, ((Array)registry[2]).Count);
                Assert.AreEqual(BigInteger.Zero, ((Array)Success(snapshot, "getAccount", [id]))[8].GetInteger());
                Success(snapshot, "cancelModuleCall", [id, "verifier"]);
            }
        }

        [TestMethod]
        public void PartialReplacementCleansRemovedActiveLeavesButRetainsBootstraps()
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 125, hook: true, composite: true);
            var first = ModuleFixture(snapshot, 126, hook: true); var inactive = ModuleFixture(snapshot, 127, hook: true);
            var id = RegisterWithModules(snapshot, hook: root);
            foreach (var leaf in new[] { first, inactive })
            {
                object[] args = [id, leaf.Hash, "configure", new object[] { 9 }];
                Success(snapshot, "callHookChild", args); Success(snapshot, "callHookChild", args, 1000 + SmartAccountState.ModuleChangeDelayMs);
            }
            foreach (var roster in new[] { new object[] { first.Hash }, new object[0] })
            {
                object[] args = [id, "roster", new object[] { roster }];
                Success(snapshot, "callHook", args); Success(snapshot, "callHook", args, 1000 + SmartAccountState.ModuleChangeDelayMs);
            }
            Assert.IsFalse(snapshot.Contains(ConfigKey(first, id))); Assert.IsTrue(snapshot.Contains(ConfigKey(inactive, id)));
            var registry = (Array)Success(snapshot, "getModuleDependencies", [id, "hook"]);
            Assert.AreEqual(1, ((Array)registry[1]).Count); Assert.AreEqual(0, ((Array)registry[2]).Count);
            Success(snapshot, "proposeHook", [id, UInt160.Zero]);
            Success(snapshot, "activateHook", [id], 1000 + SmartAccountState.ModuleChangeDelayMs);
            Assert.IsFalse(snapshot.Contains(ConfigKey(inactive, id)));
        }

        [TestMethod]
        public void LifecycleCancellationAndReplacementPreserveEpochRules()
        {
            var snapshot = Snapshot(); var id = RegisterWithModules(snapshot);
            var verifier = ModuleFixture(snapshot, 128); var hook = ModuleFixture(snapshot, 129, hook: true);
            foreach (var (role, module) in new[] { ("Verifier", verifier), ("Hook", hook) })
            {
                Success(snapshot, "propose" + role, [id, module.Hash]); Success(snapshot, "cancel" + role, [id]);
                Success(snapshot, "propose" + role, [id, module.Hash]);
                Success(snapshot, "activate" + role, [id], 1000 + SmartAccountState.ModuleChangeDelayMs);
            }
            Success(snapshot, "proposeRecoveryAddress", [id, Next]); Success(snapshot, "cancelRecoveryAddress", [id]);
            Success(snapshot, "proposeRecovery", [id, Next], signers: [Recovery]); Success(snapshot, "cancelRecovery", [id]);
            Assert.AreEqual(new BigInteger(2), ((Array)Success(snapshot, "getAccount", [id]))[8].GetInteger());
            Assert.AreEqual(new BigInteger(2), Success(snapshot, "getVersion", []).GetInteger());
            Assert.IsInstanceOfType<ByteString>(Success(snapshot, "getOperationDigest", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])]));
            foreach (string role in new[] { "verifier", "hook", "unknown" })
                Assert.IsFalse(Success(snapshot, "hasModuleContext", [id, role, verifier.Hash, "unknown"]).GetBoolean());
            Assert.IsFalse(Success(snapshot, "isAccountAuthorized", [id]).GetBoolean());
        }

        [TestMethod]
        public void MalformedPendingCallRecordsFailClosedWithoutTypeCoercion()
        {
            var snapshot = Snapshot(); var module = ModuleFixture(snapshot, 120); var id = RegisterWithModules(snapshot, module);
            Success(snapshot, "callVerifier", [id, "configure", new object[] { 7 }]);
            var key = new KeyBuilder(NativeContract.AccountManagement.Id, 0x30).Add(id).Add((byte)0);
            var original = snapshot[key].Value.ToArray();
            Action<Array>[] changes = [
                record => record[0] = true,
                record => record[2] = false,
                record => record[7] = new BigInteger(ulong.MaxValue) + 1,
                record => record[8] = new BigInteger(ulong.MaxValue) + 1,
                record => record[9] = false,
                record => record[9] = 1,
                record => record[5] = new Neo.VM.Types.Buffer("configure"u8),
                record => record[6] = new Struct((Array)record[6]),
                record => ((Array)record[6])[0] = Next.ToArray(),
                record => record[8] = 1001,
                record => ((Array)record[3])[0] = Next.ToArray(),
            ];
            foreach (var change in changes)
            {
                var record = (Array)BinarySerializer.Deserialize(original, ExecutionEngineLimits.Default); change(record);
                snapshot.GetAndChange(key).Value = BinarySerializer.Serialize(record, 8192, 8192);
                Fault(snapshot, "getPendingModuleCall", [id, "verifier"]);
                Fault(snapshot, "callVerifier", [id, "configure", new object[] { 7 }], 1000 + SmartAccountState.ModuleChangeDelayMs);
            }
            Success(snapshot, "cancelModuleCall", [id, "verifier"]);
            Assert.IsFalse(snapshot.Contains(key));
        }

        [TestMethod]
        public void CleanupFaultRetainsBindingAndPendingChangeWithoutPartialWrite()
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 116, abortCleanup: true); var id = RegisterWithModules(snapshot, root);
            var args = new object[] { id, "configure", new object[] { 17 } };
            Success(snapshot, "callVerifier", args); Success(snapshot, "callVerifier", args, 1000 + SmartAccountState.ModuleChangeDelayMs);
            Success(snapshot, "proposeVerifier", [id, UInt160.Zero]);
            Fault(snapshot, "activateVerifier", [id], 1000 + SmartAccountState.ModuleChangeDelayMs);
            Assert.AreEqual(new BigInteger(17), new BigInteger(snapshot[ConfigKey(root, id)].Value.Span));
            var state = (Array)Success(snapshot, "getAccount", [id]); Assert.IsInstanceOfType<Array>(state[5]); Assert.IsInstanceOfType<Array>(state[9]);
            Assert.AreEqual(BigInteger.One, state[8].GetInteger());
        }
    }
}
