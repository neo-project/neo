// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.CompositeReceipt.cs file belongs to the neo project and is free
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
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        private static void EmitReceipt(ScriptBuilder script, StackItem receipt) =>
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "deserialize", BinarySerializer.Serialize(receipt, 1024, 32));
        private static Array Receipt(params UInt160[] children) =>
            new([Boolean.True, new Array(children.Select(c => (StackItem)c.ToArray())), new ByteString(new byte[32])]);
        private static (DataCache Snapshot, UInt160 Id, ContractState Root, ContractState[] Leaves) ReceiptFixture(
            Func<ContractState[], StackItem> receipt = null, Action<ScriptBuilder> post = null, bool scalarAbort = false)
        {
            var snapshot = Snapshot();
            var leaves = new[] { ModuleFixture(snapshot, 701), ModuleFixture(snapshot, 702), ModuleFixture(snapshot, 703) };
            var root = ModuleFixture(snapshot, 700, composite: true);
            if (receipt is not null) ReplaceBody(snapshot, root, "validateCompositeSignature", b => EmitReceipt(b, receipt(leaves)));
            if (post is not null) ReplaceBody(snapshot, root, "postExecuteComposite", post);
            if (scalarAbort) ReplaceBody(snapshot, root, "validateSignature", b => b.Emit(OpCode.ABORT));
            var id = RegisterWithModules(snapshot, root);
            object[] roster = [id, "roster", new object[] { leaves.Select(l => (object)l.Hash).ToArray() }];
            Success(snapshot, "callVerifier", roster);
            Success(snapshot, "callVerifier", roster, 1000 + SmartAccountState.ModuleChangeDelayMs);
            return (snapshot, id, root, leaves);
        }
        private static void PostTo(ScriptBuilder script, UInt160 child)
        {
            script.Emit(OpCode.LDARG2).Emit(OpCode.LDARG1).Emit(OpCode.LDARG0).EmitPush(3).Emit(OpCode.PACK)
                .EmitPush(CallFlags.All).EmitPush("postExecute").EmitPush(child)
                .EmitSysCall(ApplicationEngine.System_Contract_Call).Emit(OpCode.DROP);
        }
        [TestMethod]
        public void CompositeReceiptUsesNewCallbacksAndNeverScalarRevalidation()
        {
            var fx = ReceiptFixture(leaves => Receipt(leaves[0].Hash, leaves[2].Hash), scalarAbort: true);
            Success(fx.Snapshot, "executeUserOp", [fx.Id, Operation(NativeContract.StdLib.Hash, "serialize", [7])], signers: []);
            Assert.AreEqual(BigInteger.One, Success(fx.Snapshot, "getNonce", [fx.Id, 0]).GetInteger());
        }
        [TestMethod]
        [DataRow("struct")]
        [DataRow("integer")]
        [DataRow("false")]
        [DataRow("empty")]
        [DataRow("duplicate")]
        [DataRow("reversed")]
        [DataRow("foreign")]
        [DataRow("three")]
        [DataRow("child-buffer")]
        [DataRow("commitment-buffer")]
        [DataRow("commitment-short")]
        [DataRow("child-struct")]
        [DataRow("shape")]
        public void CompositeReceiptRejectsMalformedApprovals(string variant)
        {
            var fx = ReceiptFixture(leaves =>
            {
                var receipt = Receipt(leaves[0].Hash, leaves[1].Hash);
                switch (variant)
                {
                    case "struct": return new Struct(receipt);
                    case "integer": receipt[0] = new Integer(1); break;
                    case "false": receipt[0] = Boolean.False; break;
                    case "empty": receipt[1] = new Array(); break;
                    case "duplicate": receipt[1] = new Array([leaves[0].Hash.ToArray(), leaves[0].Hash.ToArray()]); break;
                    case "reversed": receipt[1] = new Array([leaves[1].Hash.ToArray(), leaves[0].Hash.ToArray()]); break;
                    case "foreign": receipt[1] = new Array([Custody.ToArray()]); break;
                    case "three": return Receipt(leaves.Select(c => c.Hash).ToArray());
                    case "child-buffer": receipt[1] = new Array([new Neo.VM.Types.Buffer(leaves[0].Hash.ToArray())]); break;
                    case "commitment-buffer": receipt[2] = new Neo.VM.Types.Buffer(new byte[32]); break;
                    case "commitment-short": receipt[2] = new ByteString(new byte[31]); break;
                    case "child-struct": receipt[1] = new Struct([leaves[0].Hash.ToArray()]); break;
                    case "shape": receipt.Add(StackItem.Null); break;
                }
                return receipt;
            });
            using var engine = Invoke(fx.Snapshot, "executeUserOp", [fx.Id, Operation(NativeContract.StdLib.Hash, "serialize", [7])], signers: []);
            Rejected(engine, "composite receipt");
            Assert.AreEqual(BigInteger.Zero, Success(fx.Snapshot, "getNonce", [fx.Id, 0]).GetInteger());
        }
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompositeReceiptPostGrantsOnlyApprovedChild(bool selected)
        {
            // Hashes are deterministic for each fixture; create the bodies before pinning.
            var snapshot = Snapshot(); var child = ModuleFixture(snapshot, 710); var other = ModuleFixture(snapshot, 711);
            var root = ModuleFixture(snapshot, 712, composite: true);
            ReplaceBody(snapshot, root, "validateCompositeSignature", b => EmitReceipt(b, Receipt(child.Hash)));
            ReplaceBody(snapshot, root, "postExecuteComposite", b => PostTo(b, selected ? child.Hash : other.Hash));
            var id = RegisterWithModules(snapshot, root);
            object[] roster = [id, "roster", new object[] { new object[] { child.Hash, other.Hash } }];
            Success(snapshot, "callVerifier", roster); Success(snapshot, "callVerifier", roster, 1000 + SmartAccountState.ModuleChangeDelayMs);
            using var engine = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])]);
            if (selected) Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            else Rejected(engine, "ASSERT");
        }

        private static Array CompositeOperation(int nonce = 0) => new([NativeContract.StdLib.Hash.ToArray(), "serialize",
            new Array([7]), new Integer(nonce), new Integer(long.MaxValue), ByteString.Empty]);
        private static void PublishReceiptRoster(DataCache snapshot, UInt160 id, params ContractState[] children)
        {
            object[] args = [id, "roster", new object[] { children.Select(c => (object)c.Hash).ToArray() }];
            Success(snapshot, "callVerifier", args); Success(snapshot, "callVerifier", args, 1000 + SmartAccountState.ModuleChangeDelayMs);
        }
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompositeReceiptVerificationRejectsMalformedAndApplicationValidatesAfresh(bool malformed)
        {
            var snapshot = Snapshot(); var child = ModuleFixture(snapshot, 720); var root = ModuleFixture(snapshot, 721, composite: true);
            ReplaceBody(snapshot, root, "validateCompositeSignature", script =>
            {
                // Verification returns a valid receipt; Application returns false. A receipt
                // cached between trigger executions would incorrectly authorize the latter.
                script.EmitPush(new byte[32]);
                script.EmitPush(child.Hash).EmitPush(1).Emit(OpCode.PACK);
                if (malformed) script.EmitPush(1);
                else script.EmitSysCall(ApplicationEngine.System_Runtime_GetTrigger).EmitPush((int)TriggerType.Verification).Emit(OpCode.NUMEQUAL);
                script.EmitPush(3).Emit(OpCode.PACK);
            });
            var id = RegisterWithModules(snapshot, root); PublishReceiptRoster(snapshot, id, child);
            var state = ReadAccountState(snapshot, id);
            var tx = new Transaction
            {
                Script = SmartAccountEnvelope.CreateApplicationScript(id, CompositeOperation(), false, state.AuthorityEpoch, state.ConfigurationNonce),
                Signers = [new Signer { Account = Custody, Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = []
            };
            using var verification = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, Block(1000), Settings, gas: 150_000_000);
            verification.LoadScript(SmartAccountProtocol.CreateVerificationScript(id), configureState: value => value.CallFlags = CallFlags.ReadOnly);
            Assert.AreEqual(malformed ? VMState.FAULT : VMState.HALT, verification.Execute(), verification.FaultException?.ToString());
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            using var application = InvokeItems(snapshot, "executeUserOp", new Array([id.ToArray(), CompositeOperation()]));
            Rejected(application, "composite receipt");
            Assert.IsFalse(Success(snapshot, "hasModuleContext", [id, "verifier", child.Hash, "postExecute"]).GetBoolean());
        }
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CompositeReceiptBatchOwnsIndependentSubsetsAndPostMutationCannotExpandGrant(bool reuseFirst)
        {
            var snapshot = Snapshot(); var first = ModuleFixture(snapshot, 730); var second = ModuleFixture(snapshot, 731);
            foreach (var child in new[] { first, second }) ReplaceBody(snapshot, child, "postExecute", script =>
            {
                AssertPhase(script, "verifier", "postExecute");
                script.EmitPush(99).Emit(OpCode.LDARG0).EmitSysCall(ApplicationEngine.System_Storage_GetContext)
                    .EmitSysCall(ApplicationEngine.System_Storage_Put);
            });
            var root = ModuleFixture(snapshot, 732, composite: true);
            ReplaceBody(snapshot, root, "validateCompositeSignature", script =>
            {
                script.EmitPush(new byte[32]);
                script.EmitPush(second.Hash).EmitPush(first.Hash).EmitPush(2).Emit(OpCode.PACK)
                    .Emit(OpCode.LDARG1).EmitPush(3).Emit(OpCode.PICKITEM).Emit(OpCode.PICKITEM).EmitPush(1).Emit(OpCode.PACK);
                script.EmitPush(true).EmitPush(3).Emit(OpCode.PACK);
            });
            ReplaceBody(snapshot, root, "postExecuteComposite", script =>
            {
                if (reuseFirst)
                {
                    // Rewrite the VM copy of the receipt and then request the first child's
                    // authority on every operation. The second item's grant must not expand.
                    script.Emit(OpCode.LDARG3).EmitPush(1).Emit(OpCode.PICKITEM).EmitPush(0).EmitPush(first.Hash).Emit(OpCode.SETITEM);
                }
                script.Emit(OpCode.LDARG2).Emit(OpCode.LDARG1).Emit(OpCode.LDARG0).EmitPush(3).Emit(OpCode.PACK)
                    .EmitPush(CallFlags.All).EmitPush("postExecute")
                    .Emit(OpCode.LDARG3).EmitPush(1).Emit(OpCode.PICKITEM).EmitPush(0).Emit(OpCode.PICKITEM)
                    .EmitSysCall(ApplicationEngine.System_Contract_Call).Emit(OpCode.DROP);
            });
            var id = RegisterWithModules(snapshot, root); PublishReceiptRoster(snapshot, id, first, second);
            using var engine = InvokeItems(snapshot, "executeUserOps", new Array([id.ToArray(), new Array([CompositeOperation(), CompositeOperation(1)])]));
            if (reuseFirst)
            {
                Rejected(engine, "ASSERT");
                Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
                Assert.IsFalse(snapshot.Contains(ConfigKey(first, id)), "Earlier child settlement writes cannot persist after a later batch fault.");
                Assert.IsTrue(engine.SnapshotCache.Contains(ConfigKey(first, id)), "The first child really settled before the second operation failed; this cache is discarded on FAULT.");
            }
            else
            {
                Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString()); engine.SnapshotCache.Commit();
                Assert.AreEqual(new BigInteger(2), Success(snapshot, "getNonce", [id, 0]).GetInteger());
            }
        }
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void CompositeReceiptRechecksAllActiveCodePinsAfterTarget(bool rootChanged, bool update)
        {
            var snapshot = Snapshot(); var selected = ModuleFixture(snapshot, 740); var other = ModuleFixture(snapshot, 741, allowDestroy: true);
            var root = ModuleFixture(snapshot, 742, composite: true, allowDestroy: true);
            ReplaceBody(snapshot, root, "validateCompositeSignature", b => EmitReceipt(b, Receipt(selected.Hash)));
            var id = RegisterWithModules(snapshot, root); PublishReceiptRoster(snapshot, id, selected, other);
            var changed = rootChanged ? root : other;
            var manifest = Neo.Json.JObject.Parse(changed.Manifest.ToJson().ToString()); manifest["extra"]["mutation"] = "during-target";
            using var engine = Invoke(snapshot, "executeUserOp", [id, Operation(changed.Hash, update ? "replaceManifest" : "destroySelf",
                update ? new object[] { System.Text.Encoding.UTF8.GetBytes(manifest.ToString()) } : [])]);
            Rejected(engine, update ? "code identity has changed" : "module is zero, native or blocked");
            Assert.IsNotNull(NativeContract.ContractManagement.GetContract(snapshot, changed.Hash));
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
        }

        [TestMethod]
        public void CompositeReceiptRechecksPinsAfterPostAndClearsAuthorityOnFault()
        {
            var snapshot = Snapshot(); var child = ModuleFixture(snapshot, 750, allowDestroy: true);
            var root = ModuleFixture(snapshot, 751, composite: true);
            ReplaceBody(snapshot, root, "validateCompositeSignature", b => EmitReceipt(b, Receipt(child.Hash)));
            ReplaceBody(snapshot, root, "postExecuteComposite", b => b.EmitDynamicCall(child.Hash, "destroySelf").Emit(OpCode.DROP));
            var id = RegisterWithModules(snapshot, root); PublishReceiptRoster(snapshot, id, child);
            using var engine = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7])]);
            Rejected(engine, "module is zero, native or blocked");
            Assert.IsNotNull(NativeContract.ContractManagement.GetContract(snapshot, child.Hash));
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            Assert.IsFalse(Success(snapshot, "hasModuleContext", [id, "verifier", child.Hash, "postExecute"]).GetBoolean());
        }

        [TestMethod]
        public void CompositeConfigurationRejectsActiveChildDomainExpansionAtomically()
        {
            var snapshot = Snapshot(); var child = ModuleFixture(snapshot, 760); var root = ModuleFixture(snapshot, 761, composite: true);
            ReplaceBody(snapshot, child, "getSignerDomains", script =>
            {
                Array Domains(int count) => new(Enumerable.Range(1, count).Select(n => (StackItem)new ByteString(Enumerable.Repeat((byte)n, 32).ToArray())));
                EmitReceipt(script, new Array([Domains(1), Domains(4)]));
                script.Emit(OpCode.LDARG0).EmitSysCall(ApplicationEngine.System_Storage_GetReadOnlyContext)
                    .EmitSysCall(ApplicationEngine.System_Storage_Get).Emit(OpCode.CONVERT, [(byte)StackItemType.Integer])
                    .EmitPush(7).Emit(OpCode.GT).Emit(OpCode.CONVERT, [(byte)StackItemType.Integer]).Emit(OpCode.PICKITEM);
            });
            var id = RegisterWithModules(snapshot, root);
            object[] bootstrap = [id, child.Hash, "configure", new object[] { 7 }];
            Success(snapshot, "callVerifierChild", bootstrap); Success(snapshot, "callVerifierChild", bootstrap, 1000 + SmartAccountState.ModuleChangeDelayMs);
            PublishReceiptRoster(snapshot, id, child);
            var before = ReadAccountState(snapshot, id);
            object[] changed = [id, child.Hash, "configure", new object[] { 9 }];
            Success(snapshot, "callVerifierChild", changed);
            using var engine = Invoke(snapshot, "callVerifierChild", changed, 1000 + SmartAccountState.ModuleChangeDelayMs);
            Rejected(engine, "three aggregate signer domains");
            Assert.AreEqual(new BigInteger(7), new BigInteger(snapshot[ConfigKey(child, id)].Value.Span));
            Assert.AreEqual(before.ConfigurationNonce, ReadAccountState(snapshot, id).ConfigurationNonce);
            Assert.IsInstanceOfType<Array>(Success(snapshot, "getPendingModuleCall", [id, "verifier"]));
        }

        [TestMethod]
        public void ScalarVerifierCannotPublishRosterToBypassReceiptAuthority()
        {
            var snapshot = Snapshot(); var child = ModuleFixture(snapshot, 770); var root = ModuleFixture(snapshot, 771, composite: true);
            // A matching display name and a configuration capability do not opt a
            // scalar verifier into the composite callback/receipt protocol.
            root.Manifest.Extra["smartAccount"]["compositeVerifier"] = false;
            ReplaceReturn(snapshot, root, "supportsComposition", OpCode.PUSHF);
            var id = RegisterWithModules(snapshot, root);
            object[] roster = [id, "roster", new object[] { new object[] { child.Hash } }];
            Success(snapshot, "callVerifier", roster);
            using var engine = Invoke(snapshot, "callVerifier", roster, 1000 + SmartAccountState.ModuleChangeDelayMs);
            Rejected(engine, "composite profile");
            var dependencies = (Array)Success(snapshot, "getModuleDependencies", [id, "verifier"]);
            Assert.AreEqual(0, ((Array)dependencies[2]).Count);
        }
    }
}
