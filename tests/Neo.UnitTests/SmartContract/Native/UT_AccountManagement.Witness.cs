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
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
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
                    Script = SmartAccountEnvelope.CreateApplicationScript(id, payload, true, 0, 0),
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
        [DataRow(false)]
        [DataRow(true)]
        public void ProxyCannotAuthorizeItsOwnTransactionFees(bool batch)
        {
            var snapshot = Snapshot();
            var id = RegisterWithModules(snapshot, ModuleFixture(snapshot, 121));
            var proxy = SmartAccountProtocol.GetAccountAddress(id);
            Array operation = new([NativeContract.StdLib.Hash.ToArray(), "serialize", new Array([7]),
                BigInteger.Zero, new BigInteger(long.MaxValue), ByteString.Empty]);
            foreach (bool proxyPays in new[] { true, false })
            {
                var tx = new Transaction
                {
                    Script = SmartAccountEnvelope.CreateApplicationScript(id, batch ? new Array([operation]) : operation, batch, 0, 0),
                    Signers = proxyPays
                        ? [new Signer { Account = proxy, Scopes = WitnessScope.Global }]
                        : [new Signer { Account = Next, Scopes = WitnessScope.None },
                           new Signer { Account = proxy, Scopes = WitnessScope.CustomContracts, AllowedContracts = [NativeContract.StdLib.Hash] }],
                    Attributes = [],
                    Witnesses = []
                };
                using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, Block(1000), Settings, gas: 150_000_000);
                engine.LoadScript(SmartAccountProtocol.CreateVerificationScript(id), configureState: state => state.CallFlags = CallFlags.ReadOnly);
                Assert.AreEqual(proxyPays ? VMState.FAULT : VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
                if (proxyPays) Assert.Contains("fee payer", engine.FaultException?.Message ?? string.Empty);
                else Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
            }
        }

        [TestMethod]
        public void ExternalSponsorAndStateBoundOperationSignaturesVerifyExactTransaction()
        {
            var snapshot = Snapshot();
            byte[] userSecret = new byte[32]; userSecret[^1] = 2;
            byte[] sponsorSecret = new byte[32]; sponsorSecret[^1] = 3;
            var userKey = new KeyPair(userSecret); var sponsorKey = new KeyPair(sponsorSecret);
            var verifier = ModuleFixture(snapshot, 122);
            ReplaceBody(snapshot, verifier, "validateSignature", script =>
            {
                AssertPhase(script, "verifier", "validation");
                script.EmitPush((byte)NamedCurveHash.secp256r1SHA256);
                script.Emit(OpCode.LDARG1).EmitPush(5).Emit(OpCode.PICKITEM);
                script.EmitPush(userKey.PublicKey.EncodePoint(true));
                script.Emit(OpCode.LDARG1).Emit(OpCode.LDARG0).EmitPush(2).Emit(OpCode.PACK)
                    .EmitPush(CallFlags.ReadOnly).EmitPush("getOperationDigest").EmitPush(NativeContract.AccountManagement.Hash)
                    .EmitSysCall(ApplicationEngine.System_Contract_Call);
                script.EmitPush(4).Emit(OpCode.PACK).EmitPush(CallFlags.ReadOnly).EmitPush("verifyWithECDsa")
                    .EmitPush(NativeContract.CryptoLib.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call);
            });
            var id = RegisterWithModules(snapshot, verifier);
            var proxy = SmartAccountProtocol.GetAccountAddress(id);
            var unsigned = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
            byte[] digest = Success(snapshot, "getOperationDigest", [id, unsigned]).GetSpan().ToArray();
            Array operation = new([NativeContract.StdLib.Hash.ToArray(), "serialize", new Array([7]),
                BigInteger.Zero, new BigInteger(long.MaxValue), Crypto.Sign(digest, userKey)]);
            byte[] payerScript = Contract.CreateSignatureRedeemScript(sponsorKey.PublicKey);
            var tx = new Transaction
            {
                Script = SmartAccountEnvelope.CreateApplicationScript(id, operation, false, 0, 0),
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + 20,
                Signers = [new Signer { Account = payerScript.ToScriptHash(), Scopes = WitnessScope.None },
                    new Signer { Account = proxy, Scopes = WitnessScope.CustomContracts, AllowedContracts = [NativeContract.StdLib.Hash] }],
                Attributes = [],
                Witnesses = [new Witness { VerificationScript = payerScript, InvocationScript = new byte[66] },
                    new Witness { VerificationScript = SmartAccountProtocol.CreateVerificationScript(id), InvocationScript = System.ReadOnlyMemory<byte>.Empty }]
            };
            tx.NetworkFee = tx.CalculateNetworkFee(snapshot, Settings);
            using (var invocation = new ScriptBuilder()) tx.Witnesses[0].InvocationScript = invocation.EmitPush(tx.Sign(sponsorKey, Settings.Network)).ToArray();
            Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(Settings));
            Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateDependent(Settings, snapshot, null, []));
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            tx.SystemFee++;
            Assert.AreEqual(VerifyResult.InvalidSignature, tx.VerifyStateIndependent(Settings));
            tx.SystemFee--;
            tx.Signers = [tx.Signers[1]]; tx.Witnesses = [tx.Witnesses[1]];
            Assert.IsFalse(tx.VerifyWitnesses(Settings, snapshot, 150_000_000), "A valid UserOp signature must never authorize proxy fee payment.");
        }

        [TestMethod]
        public void NativeReentryFaultsAndCannotConsumeNonceOrRetainAuthority()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            var inner = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
            using var failed = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.AccountManagement.Hash, "executeUserOp", [id, inner, 0, 0])]);
            Assert.AreEqual(VMState.FAULT, failed.State);
            Assert.Contains("already executing", failed.FaultException.Message);
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
            using var again = failed.GetState<SmartAccountInvocationContext>().EnterAccount(id);
        }
    }
}
