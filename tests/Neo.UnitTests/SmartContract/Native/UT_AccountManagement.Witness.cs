// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.Witness.cs file belongs to the neo project and is free
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
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        private static ContractState WitnessTarget(DataCache snapshot)
        {
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness).Emit(OpCode.RET);
            var target = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("check", ContractParameterType.Boolean, ContractParameterType.Hash160));
            snapshot.AddContract(target.Hash, target); return target;
        }

        [TestMethod]
        public void RegisteredProxyRequiresExactTargetFrameAndOriginalScope()
        {
            var snapshot = Snapshot(); var id = Register(snapshot); var proxy = SmartAccountProtocol.GetAccountAddress(id);
            var target = WitnessTarget(snapshot);
            var custody = new Signer { Account = Custody, Scopes = WitnessScope.Global };
            long nonce = 0;
            foreach (var (scope, allowed, expected) in new[] {
                (WitnessScope.Global, target.Hash, true), (WitnessScope.None, target.Hash, false),
                (WitnessScope.CalledByEntry, target.Hash, false),
                (WitnessScope.CustomContracts, target.Hash, true), (WitnessScope.CustomContracts, Next, false) })
            {
                var signer = new Signer { Account = proxy, Scopes = scope, AllowedContracts = [allowed] };
                using var direct = Invoke(snapshot, "check", [proxy], target: target.Hash, signerDetails: [custody, signer]);
                Assert.AreEqual(VMState.HALT, direct.State, direct.FaultException?.ToString());
                Assert.IsFalse(direct.ResultStack.Pop().GetBoolean(), "A signer alone must not authorize outside the entrypoint.");
                using var executed = Invoke(snapshot, "executeUserOp", [id, Operation(target.Hash, "check", [proxy], nonce++)], signerDetails: [custody, signer]);
                Assert.AreEqual(VMState.HALT, executed.State, executed.FaultException?.ToString());
                Assert.AreEqual(expected, executed.ResultStack.Pop().GetBoolean(), scope.ToString());
            }
            using var noSigner = Invoke(snapshot, "executeUserOp", [id, Operation(target.Hash, "check", [proxy], nonce)]);
            Assert.AreEqual(VMState.HALT, noSigner.State, noSigner.FaultException?.ToString());
            Assert.IsFalse(noSigner.ResultStack.Pop().GetBoolean(), "The invocation grant cannot synthesize a signer.");
            // Ordinary unregistered identities retain their original scope semantics.
            using var ordinary = Invoke(snapshot, "check", [Custody], target: target.Hash);
            Assert.AreEqual(VMState.HALT, ordinary.State, ordinary.FaultException?.ToString());
            Assert.IsTrue(ordinary.ResultStack.Pop().GetBoolean());
        }

        [TestMethod]
        public void ProxyWitnessCannotEscapeIntoDescendantOrLoadedScript()
        {
            var snapshot = Snapshot(); var id = Register(snapshot); var proxy = SmartAccountProtocol.GetAccountAddress(id);
            var leaf = WitnessTarget(snapshot);
            foreach (bool dynamic in new[] { false, true })
            {
                using var script = new ScriptBuilder();
                if (dynamic)
                {
                    Push(script, new object[] { proxy }); script.EmitPush(CallFlags.All);
                    // The loaded script checks the supplied witness, not its caller's hash.
                    using var loaded = new ScriptBuilder(); loaded.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness).Emit(OpCode.RET);
                    script.EmitPush(loaded.ToArray()).EmitSysCall(ApplicationEngine.System_Runtime_LoadScript);
                }
                else script.EmitDynamicCall(leaf.Hash, "check", proxy);
                script.Emit(OpCode.RET);
                var root = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("call", ContractParameterType.Boolean)); root.Id = dynamic ? 103 : 102;
                snapshot.AddContract(root.Hash, root);
                using var executed = Invoke(snapshot, "executeUserOp", [id, Operation(root.Hash, "call", [], dynamic ? 1 : 0)], signers: [Custody, proxy]);
                Assert.AreEqual(VMState.HALT, executed.State, executed.FaultException?.ToString());
                Assert.IsFalse(executed.ResultStack.Pop().GetBoolean());
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ExactProxyVerificationValidatesEveryOperationWithoutWriting(bool plugin)
        {
            var snapshot = Snapshot();
            var verifier = plugin ? ModuleFixture(snapshot, 119) : null;
            var id = RegisterWithModules(snapshot, verifier, plugin ? ModuleFixture(snapshot, 120, hook: true) : null);
            Array Op(int nonce) => new([NativeContract.StdLib.Hash.ToArray(), "serialize", new Array([7]),
                new BigInteger(nonce), new BigInteger(long.MaxValue), ByteString.Empty]);
            foreach (bool duplicate in new[] { false, true })
            {
                var payload = new Array([Op(0), Op(duplicate ? 0 : 1)]);
                var tx = new Transaction
                {
                    Version = 0,
                    Script = SmartAccountEnvelope.CreateApplicationScript(id, payload, true),
                    Signers = [new Signer { Account = Custody, Scopes = WitnessScope.Global }],
                    Attributes = [],
                    Witnesses = []
                };
                using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, Block(1000), Settings, gas: 150_000_000);
                engine.LoadScript(SmartAccountProtocol.CreateVerificationScript(id), configureState: state => state.CallFlags = CallFlags.ReadOnly);
                Assert.AreEqual(duplicate ? VMState.FAULT : VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
                if (!duplicate)
                {
                    Assert.AreEqual(1, engine.ResultStack.Count);
                    Assert.IsInstanceOfType<Neo.VM.Types.Boolean>(engine.ResultStack.Peek());
                    Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                }
                Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
                Assert.AreEqual(0, engine.Notifications.Count);
            }
        }

        [TestMethod]
        public void NativeReentryFaultsAndCannotConsumeNonceOrRetainAuthority()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            var inner = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
            using var failed = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.AccountManagement.Hash, "executeUserOp", [id, inner])]);
            Assert.AreEqual(VMState.FAULT, failed.State);
            Assert.Contains("already executing", failed.FaultException.Message);
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
            using var again = failed.GetState<SmartAccountInvocationContext>().EnterAccount(id);
        }
    }
}
