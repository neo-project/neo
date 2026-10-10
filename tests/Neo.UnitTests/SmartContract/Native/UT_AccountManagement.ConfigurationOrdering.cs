// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.ConfigurationOrdering.cs file belongs to the neo project and is free
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
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        private static void AddConfigurationReadiness(DataCache snapshot, ContractState leaf)
        {
            using var script = new ScriptBuilder();
            script.Emit(OpCode.INITSLOT, new byte[] { 0, 1 }).Emit(OpCode.LDARG0)
                .EmitSysCall(ApplicationEngine.System_Storage_GetReadOnlyContext).EmitSysCall(ApplicationEngine.System_Storage_Get)
                .EmitPush(new byte[] { 17 }).Emit(OpCode.EQUAL).Emit(OpCode.RET);
            leaf.Manifest.Abi.Methods = [.. leaf.Manifest.Abi.Methods, new ContractMethodDescriptor
            {
                Name = "configurationReady", Safe = true, ReturnType = ContractParameterType.Boolean,
                Offset = leaf.Nef.Script.Length,
                Parameters = [new ContractParameterDefinition { Name = "accountId", Type = ContractParameterType.Hash160 }]
            }];
            leaf.Nef.Script = leaf.Nef.Script.ToArray().Concat(script.ToArray()).ToArray();
            ReplaceFixture(snapshot, leaf);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NativeCompositeCanInitializeLeafBeforePublishingRoot(bool hook)
        {
            var snapshot = Snapshot(); var leaf = ModuleFixture(snapshot, 231, hook);
            var root = ModuleFixture(snapshot, 232, hook, composite: true, delegatedChild: leaf.Hash);
            var id = RegisterWithModules(snapshot, hook ? null : root, hook ? root : null);
            string role = hook ? "hook" : "verifier", route = hook ? "callHook" : "callVerifier";
            ulong first = 1000 + SmartAccountState.ModuleChangeDelayMs, second = first + SmartAccountState.ModuleChangeDelayMs;
            object[] leafArgs = [id, leaf.Hash, "configure", new object[] { 17 }];
            Success(snapshot, route + "Child", leafArgs); Success(snapshot, route + "Child", leafArgs, first);
            Assert.AreEqual(new BigInteger(17), new BigInteger(snapshot[ConfigKey(leaf, id)].Value.Span));
            var before = (Array)Success(snapshot, "getModuleDependencies", [id, role]);
            Assert.AreEqual(1, ((Array)before[1]).Count); Assert.AreEqual(0, ((Array)before[2]).Count);
            object[] rootArgs = [id, "roster", new object[] { new object[] { leaf.Hash } }];
            Success(snapshot, route, rootArgs, first); Success(snapshot, route, rootArgs, second);
            Assert.AreEqual(2UL, ReadAccountState(snapshot, id).ConfigurationNonce);
            var after = (Array)Success(snapshot, "getModuleDependencies", [id, role]);
            Assert.AreEqual(1, ((Array)after[2]).Count);
            Assert.IsInstanceOfType<ByteString>(Success(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])], second));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NativeRootReplacementInvalidatesPendingChildConfiguration(bool hook)
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 233, hook, composite: true);
            var next = ModuleFixture(snapshot, 234, hook, composite: true); var leaf = ModuleFixture(snapshot, 235, hook);
            var id = RegisterWithModules(snapshot, hook ? null : root, hook ? root : null);
            string role = hook ? "hook" : "verifier", route = hook ? "callHookChild" : "callVerifierChild";
            object[] args = [id, leaf.Hash, "configure", new object[] { 17 }];
            Success(snapshot, route, args);
            Success(snapshot, hook ? "proposeHook" : "proposeVerifier", [id, next.Hash]);
            ulong first = 1000 + SmartAccountState.ModuleChangeDelayMs;
            Success(snapshot, hook ? "activateHook" : "activateVerifier", [id], first);
            Assert.IsInstanceOfType<Null>(Success(snapshot, "getPendingModuleCall", [id, role]));
            Assert.IsFalse(snapshot.Contains(ConfigKey(leaf, id)));
            Assert.IsFalse(Success(snapshot, route, args, first).GetBoolean());
            Assert.IsFalse(snapshot.Contains(ConfigKey(leaf, id)), "A mature intent under the previous root must only stage under the new root.");
            using (var early = Invoke(snapshot, route, args, first + 1)) Rejected(early, "immature");
            Success(snapshot, route, args, first + SmartAccountState.ModuleChangeDelayMs);
            Assert.AreEqual(new BigInteger(17), new BigInteger(snapshot[ConfigKey(leaf, id)].Value.Span));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailedRootIntentAndSuccessfulLeafMutationRequireFreshRootDelay(bool hook)
        {
            var snapshot = Snapshot(); var leaf = ModuleFixture(snapshot, 236, hook); AddConfigurationReadiness(snapshot, leaf);
            var root = ModuleFixture(snapshot, 237, hook, composite: true);
            ReplaceBody(snapshot, root, "configure", script =>
            {
                AssertPhase(script, hook ? "hook" : "verifier", "configuration");
                script.Emit(OpCode.LDARG0).EmitPush(1).Emit(OpCode.PACK).EmitPush(CallFlags.ReadOnly)
                    .EmitPush("configurationReady").EmitPush(leaf.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call)
                    .Emit(OpCode.ASSERT).EmitPush(false);
            });
            var id = RegisterWithModules(snapshot, hook ? null : root, hook ? root : null);
            string role = hook ? "hook" : "verifier", route = hook ? "callHook" : "callVerifier";
            ulong first = 1000 + SmartAccountState.ModuleChangeDelayMs, second = first + SmartAccountState.ModuleChangeDelayMs;
            object[] rootArgs = [id, "configure", new object[] { 7 }];
            Success(snapshot, route, rootArgs);
            byte[] pending = BinarySerializer.Serialize(Success(snapshot, "getPendingModuleCall", [id, role]), 8192, 8192);
            using (var failed = Invoke(snapshot, route, rootArgs, first)) Rejected(failed, "ASSERT");
            Assert.AreSequenceEqual(pending, BinarySerializer.Serialize(Success(snapshot, "getPendingModuleCall", [id, role]), 8192, 8192));
            Assert.AreEqual(0UL, ReadAccountState(snapshot, id).ConfigurationNonce);
            // One intent per role: owner explicitly cancels the failed root intent before selecting a leaf.
            Success(snapshot, "cancelModuleCall", [id, role], first);
            object[] leafArgs = [id, leaf.Hash, "configure", new object[] { 17 }];
            Success(snapshot, route + "Child", leafArgs, first); Success(snapshot, route + "Child", leafArgs, second);
            Assert.AreEqual(1UL, ReadAccountState(snapshot, id).ConfigurationNonce);
            Assert.IsFalse(Success(snapshot, route, rootArgs, second).GetBoolean());
            using (var early = Invoke(snapshot, route, rootArgs, second + 1)) Rejected(early, "immature");
            var fresh = (Array)Success(snapshot, "getPendingModuleCall", [id, role]);
            Assert.AreEqual(new BigInteger(second), fresh[7].GetInteger());
            Assert.AreEqual(BigInteger.One, fresh[9].GetInteger());
            Success(snapshot, route, rootArgs, second + SmartAccountState.ModuleChangeDelayMs);
            Assert.AreEqual(2UL, ReadAccountState(snapshot, id).ConfigurationNonce);
            Assert.IsInstanceOfType<Null>(Success(snapshot, "getPendingModuleCall", [id, role]));
        }
    }
}
