// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.Negative.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
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
        private static ApplicationEngine InvokeItems(DataCache snapshot, string method, Array args,
            TriggerType trigger = TriggerType.Application, bool ledgerTime = false, UInt160[] signers = null)
        {
            if ((method is "executeUserOp" or "executeUserOps") && args.Count == 2)
            {
                var state = ReadAccountState(snapshot, new UInt160(args[0].GetSpan()));
                args = new Array([.. args, new Integer(state.AuthorityEpoch), new Integer(state.ConfigurationNonce)]);
            }
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "deserialize", BinarySerializer.Serialize(args, 16384, 8192))
                .EmitPush(CallFlags.All).EmitPush(method).EmitPush(NativeContract.AccountManagement.Hash)
                .EmitSysCall(ApplicationEngine.System_Contract_Call);
            var tx = new Transaction { Script = script.ToArray(), Signers = (signers ?? [Custody]).Select(h => new Signer { Account = h, Scopes = WitnessScope.Global }).ToArray(), Attributes = [], Witnesses = [] };
            var engine = ApplicationEngine.Create(trigger, tx, snapshot, ledgerTime ? null : Block(1000), Settings, gas: 10_000_000_000);
            engine.LoadScript(tx.Script); engine.Execute(); return engine;
        }
        private static void Rejected(ApplicationEngine engine, string reason)
        {
            Assert.AreEqual(VMState.FAULT, engine.State, engine.FaultException?.ToString());
            Assert.Contains(reason, engine.FaultException.ToString());
            Assert.AreEqual(0, engine.Notifications.Count);
        }
        private static void ReplaceFixture(DataCache snapshot, ContractState fixture)
        {
            fixture.Nef.CheckSum = NefFile.ComputeChecksum(fixture.Nef);
            snapshot.DeleteContract(fixture.Hash); snapshot.AddContract(fixture.Hash, fixture);
        }
        private static void ReplaceReturn(DataCache snapshot, ContractState fixture, string method, OpCode opcode)
        {
            var descriptor = fixture.Manifest.Abi.Methods.Single(m => m.Name == method);
            int end = fixture.Manifest.Abi.Methods.Where(m => m.Offset > descriptor.Offset).Select(m => m.Offset).DefaultIfEmpty(fixture.Nef.Script.Length).Min();
            var script = fixture.Nef.Script.ToArray();
            Assert.AreEqual((byte)OpCode.RET, script[end - 1]);
            script[end - 2] = (byte)opcode; fixture.Nef.Script = script; ReplaceFixture(snapshot, fixture);
        }
        private static void ReplaceBody(DataCache snapshot, ContractState fixture, string method, Action<ScriptBuilder> body)
        {
            var descriptor = fixture.Manifest.Abi.Methods.Single(m => m.Name == method);
            int end = fixture.Manifest.Abi.Methods.Where(m => m.Offset > descriptor.Offset).Select(m => m.Offset).DefaultIfEmpty(fixture.Nef.Script.Length).Min();
            using var script = new ScriptBuilder();
            if (descriptor.Parameters.Length != 0) script.Emit(OpCode.INITSLOT, new byte[] { 0, (byte)descriptor.Parameters.Length });
            body(script); script.Emit(OpCode.RET);
            var bytes = fixture.Nef.Script.ToArray(); var replacement = script.ToArray();
            fixture.Nef.Script = bytes[..descriptor.Offset].Concat(replacement).Concat(bytes[end..]).ToArray();
            foreach (var next in fixture.Manifest.Abi.Methods.Where(m => m.Offset >= end)) next.Offset += replacement.Length - (end - descriptor.Offset);
            ReplaceFixture(snapshot, fixture);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ActiveLeafDispatchRequiresPublishedRosterAndCorrectCallbackPhase(bool hook)
        {
            var snapshot = Snapshot(); var leaf = ModuleFixture(snapshot, 145, hook);
            var root = ModuleFixture(snapshot, 146, hook, composite: true, delegatedChild: leaf.Hash);
            var id = RegisterWithModules(snapshot, hook ? null : root, hook ? root : null);
            string route = hook ? "callHook" : "callVerifier";
            using (var unpublished = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])]))
                Rejected(unpublished, "ASSERT");
            object[] publish = [id, "roster", new object[] { new object[] { leaf.Hash } }];
            Success(snapshot, route, publish); Success(snapshot, route, publish, 1000 + SmartAccountState.ModuleChangeDelayMs);
            Assert.IsInstanceOfType<ByteString>(Success(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])]));
            Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            // The same child must lose its callback grant once the root clears it.
            object[] clear = [id, "clearRoster", new object[0]];
            Success(snapshot, route, clear); Success(snapshot, route, clear, 1000 + SmartAccountState.ModuleChangeDelayMs);
            var registry = (Array)Success(snapshot, "getModuleDependencies", [id, hook ? "hook" : "verifier"]);
            Assert.AreEqual(0, ((Array)registry[1]).Count); Assert.AreEqual(0, ((Array)registry[2]).Count);
            using var removed = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7], 1)]);
            Rejected(removed, "ASSERT");
        }

        [TestMethod]
        public void CompositePublicationChecksRuntimeMarkersAndSignerDomainEvidence()
        {
            var variants = new (Action<DataCache, ContractState> Change, string Reason)[] {
                ((s, c) => ReplaceReturn(s, c, "supportsComposition", OpCode.PUSH1), "composition marker"),
                ((s, c) => ReplaceReturn(s, c, "supportsComposition", OpCode.PUSHT), "composition marker"),
                ((s, c) => c.Manifest.Abi.Methods = c.Manifest.Abi.Methods.Where(m => m.Name != "getSignerDomains").ToArray(), "signer-domain ABI"),
                ((s, c) => c.Manifest.Abi.GetMethod("getSignerDomains", 1).Safe = false, "signer-domain ABI"),
                ((s, c) => c.Manifest.Abi.GetMethod("getSignerDomains", 1).ReturnType = ContractParameterType.Any, "signer-domain ABI"),
                ((s, c) => c.Manifest.Abi.GetMethod("getSignerDomains", 1).Parameters[0].Type = ContractParameterType.ByteArray, "signer-domain ABI"),
                ((s, c) => ReplaceBody(s, c, "getSignerDomains", b => b.Emit(OpCode.NEWARRAY0)), "non-empty exact signer-domain Array"),
                ((s, c) => ReplaceBody(s, c, "getSignerDomains", b => b.EmitPush(true)), "non-empty exact signer-domain Array"),
                ((s, c) => ReplaceBody(s, c, "getSignerDomains", b => b.EmitPush(0).Emit(OpCode.NEWSTRUCT)), "non-empty exact signer-domain Array"),
                ((s, c) => ReplaceBody(s, c, "getSignerDomains", b => b.EmitPush(new byte[31]).EmitPush(1).Emit(OpCode.PACK)), "record ByteString"),
                ((s, c) => ReplaceBody(s, c, "getSignerDomains", b => b.EmitPush(new byte[32]).EmitPush(new byte[32]).EmitPush(2).Emit(OpCode.PACK)), "Duplicate signer domain"),
            };
            foreach (var (change, reason) in variants)
            {
                var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 147, composite: true); var child = ModuleFixture(snapshot, 148);
                change(snapshot, child); ReplaceFixture(snapshot, child); var id = RegisterWithModules(snapshot, root);
                object[] args = [id, "roster", new object[] { new object[] { child.Hash } }];
                Success(snapshot, "callVerifier", args);
                using var engine = Invoke(snapshot, "callVerifier", args, 1000 + SmartAccountState.ModuleChangeDelayMs); Rejected(engine, reason);
                Assert.AreEqual(BigInteger.Zero, ((Array)Success(snapshot, "getAccount", [id]))[8].GetInteger());
                Assert.AreEqual(0, ((Array)((Array)Success(snapshot, "getModuleDependencies", [id, "verifier"]))[1]).Count);
            }
            var invalid = Snapshot(); var named = ModuleFixture(invalid, 149, composite: true); named.Manifest.Name = "UnknownComposite"; ReplaceFixture(invalid, named);
            using var registration = Invoke(invalid, "registerAccount", [Custody, new byte[32], named.Hash, UInt160.Zero, UInt160.Zero]);
            Rejected(registration, "composite profile is not supported");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompositeCapacityCountsBootstrapLeavesWithoutPartialEnrollment(bool hook)
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 150, hook, composite: true);
            var id = RegisterWithModules(snapshot, hook ? null : root, hook ? root : null);
            string route = hook ? "callHook" : "callVerifier", role = hook ? "hook" : "verifier"; int capacity = hook ? 8 : 10;
            var leaves = Enumerable.Range(0, capacity + 1).Select(i => ModuleFixture(snapshot, 151 + i, hook)).ToArray();
            ulong now = 1000;
            foreach (var child in leaves.Take(capacity))
            {
                object[] args = [id, child.Hash, "configure", new object[] { 7 }];
                Success(snapshot, route + "Child", args, now); now += SmartAccountState.ModuleChangeDelayMs;
                Success(snapshot, route + "Child", args, now++);
            }
            object[] extra = [id, leaves.Last().Hash, "configure", new object[] { 7 }];
            Success(snapshot, route + "Child", extra, now); now += SmartAccountState.ModuleChangeDelayMs;
            using (var overflow = Invoke(snapshot, route + "Child", extra, now)) Rejected(overflow, "enrolled leaf roster is full");
            Success(snapshot, "cancelModuleCall", [id, role], now++);
            foreach (object[] roster in new[] { new object[] { leaves.Last().Hash }, leaves.Select(l => (object)l.Hash).ToArray() })
            {
                object[] args = [id, "roster", new object[] { roster }];
                Success(snapshot, route, args, now); now += SmartAccountState.ModuleChangeDelayMs;
                using (var overflow = Invoke(snapshot, route, args, now)) Rejected(overflow, roster.Length == 1 ? "enrolled cleanup roster exceeds" : "child roster exceeds");
                Success(snapshot, "cancelModuleCall", [id, role], now++);
            }
            var registry = (Array)Success(snapshot, "getModuleDependencies", [id, role]);
            Assert.AreEqual(capacity, ((Array)registry[1]).Count); Assert.AreEqual(0, ((Array)registry[2]).Count);
            Assert.IsFalse(snapshot.Contains(ConfigKey(leaves.Last(), id)));
        }

        [TestMethod]
        public void InitializationStoresProfileOnlyAtItsActivation()
        {
            foreach (Hardfork? fork in new Hardfork?[] { null, Hardfork.HF_Huyao, Hardfork.HF_SmartAccountV1 })
            {
                var snapshot = Snapshot();
                using var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, Block(1000), Settings);
                engine.LoadScript(new byte[] { (byte)OpCode.RET });
                var task = NativeContract.AccountManagement.InitializeAsync(engine, fork);
                Assert.IsTrue(task.GetAwaiter().IsCompleted);
                var key = new KeyBuilder(NativeContract.AccountManagement.Id, 0);
                if (fork != Hardfork.HF_SmartAccountV1) Assert.IsFalse(engine.SnapshotCache.Contains(key));
                else
                {
                    var bytes = engine.SnapshotCache[key].Value;
                    var record = (Array)BinarySerializer.Deserialize(bytes, ExecutionEngineLimits.Default);
                    Assert.AreEqual(2, record.Count); Assert.AreEqual(new BigInteger(2), record[0].GetInteger());
                    var expected = NativeContract.AccountManagement.GetContractState(Settings, 1).Manifest.Extra["smartAccount"]["profileParameterDigest"].GetString();
                    Assert.AreSequenceEqual(Convert.FromHexString(expected), record[1].GetSpan().ToArray());
                    Assert.AreSequenceEqual(BinarySerializer.Serialize(record, 128, 8), bytes.ToArray());
                }
            }
        }

        [TestMethod]
        public void LedgerTimestampIsRequiredWhenNoPersistingBlockIsSupplied()
        {
            foreach (bool removeHeader in new[] { false, true })
            {
                var snapshot = Snapshot(); var id = Register(snapshot, Recovery);
                var hash = NativeContract.Ledger.CurrentHash(snapshot);
                ulong expected = NativeContract.Ledger.GetHeader(snapshot, hash).Timestamp;
                if (removeHeader) snapshot.Delete(new KeyBuilder(NativeContract.Ledger.Id, 5).Add(hash));
                using var engine = InvokeItems(snapshot, "proposeRecoveryAddress", new Array([id.ToArray(), Next.ToArray()]), ledgerTime: true);
                if (removeHeader) Rejected(engine, "known ledger timestamp");
                else
                {
                    Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString()); engine.SnapshotCache.Commit();
                    var state = (Array)Success(snapshot, "getAccount", [id]);
                    Assert.AreEqual(new BigInteger(expected), ((Array)state[11])[1].GetInteger());
                }
            }
        }

        [TestMethod]
        public void FallbackWitnessSignatureFrozenStateAndTriggerFailClosed()
        {
            var snapshot = Snapshot(); var id = Register(snapshot, Recovery);
            var op = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
            using (var absent = Invoke(snapshot, "executeUserOp", [id, op], signers: [])) Rejected(absent, "custody witness");
            op[5] = new byte[] { 1 };
            using (var signature = Invoke(snapshot, "executeUserOp", [id, op])) Rejected(signature, "empty signature");
            using (var wrongTrigger = Invoke(snapshot, "freeze", [id], trigger: TriggerType.Verification)) Rejected(wrongTrigger, "requires Application trigger");
            Success(snapshot, "freeze", [id], signers: [Recovery]); op[5] = System.Array.Empty<byte>();
            using (var frozen = Invoke(snapshot, "executeUserOp", [id, op])) Rejected(frozen, "Frozen");
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
        }

        [TestMethod]
        public void VerificationRequiresTheExactProxyAndTargetContextCannotBeForged()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            using (var direct = InvokeItems(snapshot, "verify", new Array([id.ToArray()]), TriggerType.Verification))
                Rejected(direct, "exact proxy script");
            using var script = new ScriptBuilder();
            script.EmitPush(1).Emit(OpCode.PACK).EmitPush(CallFlags.ReadOnly).EmitPush("isAccountAuthorized")
                .EmitPush(NativeContract.AccountManagement.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call).Emit(OpCode.RET);
            var target = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("query", ContractParameterType.Boolean, ContractParameterType.Hash160));
            snapshot.AddContract(target.Hash, target);
            using (var ordinary = Invoke(snapshot, "query", [id], target: target.Hash))
            {
                Assert.AreEqual(VMState.HALT, ordinary.State, ordinary.FaultException?.ToString());
                Assert.IsFalse(ordinary.ResultStack.Pop().GetBoolean());
            }
            Assert.IsTrue(Success(snapshot, "executeUserOp", [id, Operation(target.Hash, "query", [id])]).GetBoolean());
            Assert.IsFalse(Success(snapshot, "executeUserOp", [id, Operation(target.Hash, "query", [Next], 1)]).GetBoolean());
        }

        [TestMethod]
        public void NonBooleanAndFalseVerifiersCannotConsumeNonce()
        {
            foreach (var opcode in new[] { OpCode.PUSHF, OpCode.PUSH1, OpCode.PUSHNULL })
            {
                var snapshot = Snapshot(); var verifier = ModuleFixture(snapshot, 140);
                ReplaceReturn(snapshot, verifier, "validateSignature", opcode);
                var id = RegisterWithModules(snapshot, verifier);
                using var engine = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])], signers: []);
                Rejected(engine, "exactly Boolean true");
                Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            }
        }

        [TestMethod]
        public void MalformedBatchesAreRejectedBeforeAnyTargetExecutes()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            var op = new Array([NativeContract.StdLib.Hash.ToArray(), "serialize", new Array([7]), BigInteger.Zero, new BigInteger(long.MaxValue), ByteString.Empty]);
            foreach (Array batch in new Array[] { new(), new Struct([op]), new(Enumerable.Range(0, 33).Select(_ => BinarySerializer.Deserialize(BinarySerializer.Serialize(op, 8192, 8192), ExecutionEngineLimits.Default))), new([StackItem.Null]) })
            {
                using var engine = InvokeItems(snapshot, "executeUserOps", new Array([id.ToArray(), batch]));
                Rejected(engine, batch.Count == 1 && batch[0].IsNull ? "batch entry must be an operation Array" : "batch must be an exact non-empty Array");
                Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            }
        }

        [TestMethod]
        public void NativeQueriesRejectInvalidRolesAndStoredNonceValues()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            foreach (string method in new[] { "getModuleDependencies", "getPendingModuleCall", "cancelModuleCall" })
                using (var engine = Invoke(snapshot, method, [id, "unknown"])) Rejected(engine, "Unknown SmartAccount module type");
            var key = new StorageKey { Id = NativeContract.AccountManagement.Id, Key = SmartAccountProtocol.GetNonceKey(id, 0) };
            foreach (BigInteger bad in new[] { BigInteger.MinusOne, SmartAccountProtocol.ExhaustedSequence + 1 })
            {
                snapshot.Delete(key); snapshot.Add(key, new StorageItem(bad));
                using var engine = Invoke(snapshot, "getNonce", [id, 0]); Rejected(engine, "stored channel cursor is invalid");
            }
            snapshot.Delete(key);
            using (var guardian = Invoke(snapshot, "freeze", [id], signers: [])) Rejected(guardian, "recovery");
        }

        [TestMethod]
        public void MissingInstalledModulesCannotConfigureOrPublishDependencies()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            foreach (string role in new[] { "Verifier", "Hook" })
            {
                using (var config = Invoke(snapshot, "call" + role, [id, "configure", new object[] { 7 }])) Rejected(config, "No root module is installed");
                using (var publish = Invoke(snapshot, "set" + role + "Dependencies", [id, new object[0]])) Rejected(publish, "No root module is installed");
                using (var clear = Invoke(snapshot, "clear" + role + "Dependencies", [id])) Rejected(clear, "No root module is installed");
            }
        }

        [TestMethod]
        public void ConfigurationCapabilitiesArgumentsAndTimestampAreValidated()
        {
            var invalidMetadata = new Action<ContractState>[] {
                // Missing ABI metadata is rejected earlier, during module admission.
                // These cases retain ABI 2 and exercise configuration capability checks.
                m => m.Manifest.Extra["smartAccount"]["configurationMethods"] = null,
                m => m.Manifest.Extra["smartAccount"]["configurationMethods"] = new JArray(1),
                m => m.Manifest.Extra["smartAccount"]["configurationMethods"] = new JArray("configure", "configure"),
                m => m.Manifest.Extra["smartAccount"]["configurationMethods"] = new JArray("configure", "_private"),
                m => m.Manifest.Extra["smartAccount"]["configurationMethods"] = new JArray("configure", "clearAccount"),
                m => m.Manifest.Abi.GetMethod("configure", 2).Safe = true,
                m => m.Manifest.Abi.GetMethod("configure", 2).Parameters[0].Type = ContractParameterType.Integer,
            };
            foreach (var mutate in invalidMetadata)
            {
                var snapshot = Snapshot(); var module = ModuleFixture(snapshot, 141); mutate(module); ReplaceFixture(snapshot, module);
                var id = RegisterWithModules(snapshot, module);
                using var engine = Invoke(snapshot, "callVerifier", [id, "configure", new object[] { 7 }]);
                Rejected(engine, "configuration");
            }
            var valid = Snapshot(); var root = ModuleFixture(valid, 142); var account = RegisterWithModules(valid, root);
            foreach (var (method, args) in new[] { ("", new object[] { 7 }), ("_private", new object[] { 7 }), ("configure", new object[64]) })
                using (var engine = Invoke(valid, "callVerifier", [account, method, args])) Rejected(engine, "account-scoped module configuration arguments");
            using (var exact = InvokeItems(valid, "callVerifier", new Array([account.ToArray(), "configure", new Struct([7])]))) Rejected(exact, "account-scoped module configuration arguments");
            using (var overflow = Invoke(valid, "callVerifier", [account, "configure", new object[] { 7 }], ulong.MaxValue)) Rejected(overflow, "maturity overflows");
            Success(valid, "callVerifier", [account, "configure", new object[] { 7 }]);
            using var cancellation = Invoke(valid, "cancelModuleCall", [account, "verifier"], signers: []); Rejected(cancellation, "Cancellation requires custody");
        }

        [TestMethod]
        public void CorruptedDependencyRecordsCannotAuthorizeOrExecute()
        {
            var snapshot = Snapshot(); var root = ModuleFixture(snapshot, 143, composite: true);
            var leaf = ModuleFixture(snapshot, 144); var id = RegisterWithModules(snapshot, root);
            var rootBinding = ((Array)Success(snapshot, "getAccount", [id]))[5];
            var leafBinding = new Array([leaf.Hash.ToArray(), SmartAccountModulePolicy.GetCodeHash(leaf).ToArray()]);
            var key = new KeyBuilder(NativeContract.AccountManagement.Id, 0x40).Add(id).Add((byte)0);
            var valid = new Array([rootBinding, new Array([leafBinding]), new Array([leaf.Hash.ToArray()])]);
            Action<Array>[] changes = [
                r => r[0] = new Struct((Array)r[0]),
                r => ((Array)r[0])[0] = Next.ToArray(),
                r => ((Array)r[0])[1] = new byte[32],
                r => r[1] = new Array(Enumerable.Range(0, 11).Select(_ => BinarySerializer.Deserialize(BinarySerializer.Serialize(leafBinding, 8192, 8192), ExecutionEngineLimits.Default))),
                r => r[2] = new Array(Enumerable.Repeat<StackItem>(leaf.Hash.ToArray(), 11)),
                r => r[1] = new Array([leafBinding, BinarySerializer.Deserialize(BinarySerializer.Serialize(leafBinding, 8192, 8192), ExecutionEngineLimits.Default)]),
                r => r[2] = new Array([leaf.Hash.ToArray(), leaf.Hash.ToArray()]),
                r => ((Array)((Array)r[1])[0])[0] = root.Hash.ToArray(),
                r => r[2] = new Array([Next.ToArray()]),
                r => ((Array)((Array)r[1])[0])[0] = true,
                r => ((Array)((Array)r[1])[0])[1] = new byte[31],
                r => r[2] = new Array([new byte[19]]),
            ];
            var canonical = BinarySerializer.Serialize(valid, 8192, 8192);
            string[] reasons = ["record Array", "stale root", "stale root", "invalid leaf roster", "invalid leaf roster", "invalid leaf roster", "invalid leaf roster", "invalid leaf roster", "invalid leaf roster", "record ByteString", "record ByteString", "record ByteString"];
            for (int i = 0; i < changes.Length; i++)
            {
                var mutate = changes[i]; var record = (Array)BinarySerializer.Deserialize(canonical, ExecutionEngineLimits.Default); mutate(record);
                snapshot.Delete(key); snapshot.Add(key, new StorageItem(BinarySerializer.Serialize(record, 8192, 8192)));
                using var query = Invoke(snapshot, "getModuleDependencies", [id, "verifier"]); Rejected(query, reasons[i]);
                using var execute = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])]); Rejected(execute, reasons[i]);
                Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            }
            snapshot.Delete(key); snapshot.Add(key, new StorageItem(canonical.Concat(new byte[] { 0 }).ToArray()));
            using (var trailing = Invoke(snapshot, "getModuleDependencies", [id, "verifier"])) Rejected(trailing, "record must be canonical");
            var empty = Snapshot(); var emptyId = Register(empty);
            empty.Add(new KeyBuilder(NativeContract.AccountManagement.Id, 0x40).Add(emptyId).Add((byte)0), new StorageItem(canonical));
            using var stale = Invoke(empty, "getModuleDependencies", [emptyId, "verifier"]); Rejected(stale, "stale root");
        }
    }
}
