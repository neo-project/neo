// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.TransactionCommitment.cs file belongs to the neo project and is free
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
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
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
        private static Transaction SignCommittedExecution(DataCache snapshot, UInt160 id, KeyPair custodyKey, bool batch)
        {
            var op = new Array([NativeContract.StdLib.Hash.ToArray(), "serialize", new Array([7]),
                BigInteger.Zero, new BigInteger(long.MaxValue), ByteString.Empty]);
            byte[] custodyScript = Contract.CreateSignatureRedeemScript(custodyKey.PublicKey);
            var transaction = new Transaction
            {
                Script = SmartAccountEnvelope.CreateApplicationScript(id, batch ? new Array([op]) : op, batch, ReadAccountState(snapshot, id).AuthorityEpoch, ReadAccountState(snapshot, id).ConfigurationNonce),
                SystemFee = 1_000_000_000,
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + 20,
                Signers = [new Signer { Account = custodyScript.ToScriptHash(), Scopes = WitnessScope.Global },
                    new Signer { Account = SmartAccountProtocol.GetAccountAddress(id), Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = [new Witness { VerificationScript = custodyScript, InvocationScript = new byte[66] },
                    new Witness { VerificationScript = SmartAccountProtocol.CreateVerificationScript(id), InvocationScript = ReadOnlyMemory<byte>.Empty }]
            };
            transaction.NetworkFee = transaction.CalculateNetworkFee(snapshot, Settings);
            using var invocation = new ScriptBuilder();
            transaction.Witnesses[0].InvocationScript = invocation.EmitPush(transaction.Sign(custodyKey, Settings.Network)).ToArray();
            return transaction;
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, false, true)]
        [DataRow(true, false, false)]
        [DataRow(true, false, true)]
        [DataRow(false, true, false)]
        [DataRow(false, true, true)]
        [DataRow(true, true, false)]
        [DataRow(true, true, true)]
        public void SignedRawWitnessAuthorizationCannotOutliveItsAccountConfiguration(bool nativeVerifier, bool recoveryCycle, bool batch)
        {
            var snapshot = Snapshot(); byte[] secret = new byte[32]; secret[^1] = 19;
            var key = new KeyPair(secret); var custody = Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash();
            ContractState verifier = null;
            if (nativeVerifier)
            {
                verifier = ModuleFixture(snapshot, 221);
                ReplaceBody(snapshot, verifier, "validateSignature", script =>
                {
                    AssertPhase(script, "verifier", "validation");
                    script.EmitPush(custody).EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness);
                });
            }
            var id = new UInt160(Success(snapshot, "registerAccount",
                [custody, new byte[32], verifier?.Hash ?? UInt160.Zero, UInt160.Zero, Recovery], signers: [custody]).GetSpan());
            var transaction = SignCommittedExecution(snapshot, id, key, batch);
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateIndependent(Settings));
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateDependent(Settings, snapshot, null, []));
            byte[] raw = transaction.ToArray();
            if (recoveryCycle)
            {
                Success(snapshot, "proposeRecovery", [id, Next], signers: [Recovery]);
                Success(snapshot, "executeRecovery", [id], 1000 + SmartAccountState.CustodyRecoveryDelayMs, []);
                Success(snapshot, "proposeRecovery", [id, custody], 1000 + SmartAccountState.CustodyRecoveryDelayMs, [Recovery]);
                Success(snapshot, "executeRecovery", [id], 1000 + 2 * SmartAccountState.CustodyRecoveryDelayMs, []);
                if (nativeVerifier)
                {
                    Success(snapshot, "proposeVerifier", [id, verifier.Hash], signers: [custody]);
                    Success(snapshot, "activateVerifier", [id], 1000 + SmartAccountState.ModuleChangeDelayMs);
                }
            }
            else
            {
                Success(snapshot, "proposeRecoveryAddress", [id, Next], signers: [custody]);
                Success(snapshot, "activateRecoveryAddress", [id], 1000 + SmartAccountState.ModuleChangeDelayMs);
            }
            // Exactly the exported bytes and signatures are replayed; no refreshed domain/signature.
            var stale = raw.AsSerializable<Transaction>();
            Assert.AreSequenceEqual(raw, stale.ToArray());
            Assert.AreEqual(VerifyResult.Succeed, stale.VerifyStateIndependent(Settings));
            Assert.AreEqual(VerifyResult.Invalid, stale.VerifyStateDependent(Settings, snapshot, null, []));
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            using (var application = ApplicationEngine.Create(TriggerType.Application, stale, snapshot, Block(1000), Settings, gas: stale.SystemFee))
            {
                application.LoadScript(stale.Script); application.Execute();
                Rejected(application, "execution authority epoch or configuration nonce is stale");
            }
            var fresh = SignCommittedExecution(snapshot, id, key, batch);
            Assert.AreEqual(VerifyResult.Succeed, fresh.VerifyStateIndependent(Settings));
            Assert.AreEqual(VerifyResult.Succeed, fresh.VerifyStateDependent(Settings, snapshot, null, []));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ExecutionCounterCommitmentIsExactAndMandatoryInBothTriggers(bool batch)
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            var op = new Array([NativeContract.StdLib.Hash.ToArray(), "serialize", new Array([7]),
                BigInteger.Zero, new BigInteger(long.MaxValue), ByteString.Empty]);
            Array payload = batch ? new Array([op]) : op;
            string method = batch ? "executeUserOps" : "executeUserOp";
            var abi = NativeContract.AccountManagement.GetContractState(Settings, 1).Manifest.Abi;
            Assert.IsNull(abi.GetMethod(method, 2));
            var descriptor = abi.GetMethod(method, 4);
            Assert.IsNotNull(descriptor);
            Assert.AreEqual(ContractParameterType.Hash160, descriptor.Parameters[0].Type);
            Assert.AreEqual(ContractParameterType.Array, descriptor.Parameters[1].Type);
            foreach (int index in new[] { 2, 3 }) Assert.AreEqual(ContractParameterType.Integer, descriptor.Parameters[index].Type);
            // Direct entry does not accept the historical ABI even without a proxy witness.
            using (var old = Invoke(snapshot, method, [id, batch ? new object[] { Operation(NativeContract.StdLib.Hash, "serialize", [7]) } : Operation(NativeContract.StdLib.Hash, "serialize", [7])], target: NativeContract.AccountManagement.Hash))
                Assert.AreEqual(VMState.FAULT, old.State);
            StackItem[] invalid = [StackItem.Null, true, false, ByteString.Empty, new byte[] { 0 }, new Array(), -1, new Integer(BigInteger.One << 64)];
            foreach (int index in new[] { 2, 3 })
                foreach (var value in invalid)
                {
                    var arguments = new Array([id.ToArray(), payload, 0, 0])
                    {
                        [index] = value
                    }; using var engine = InvokeItems(snapshot, method, arguments);
                    Assert.AreEqual(VMState.FAULT, engine.State, $"{method} counter {index} accepted {value.Type}");
                    Assert.AreEqual(0, engine.Notifications.Count);
                }
            foreach (var (epoch, configuration) in new[] { (1UL, 0UL), (0UL, 1UL), (ulong.MaxValue, ulong.MaxValue) })
            {
                byte[] script = SmartAccountEnvelope.CreateApplicationScript(id, payload, batch, epoch, configuration);
                var tx = new Transaction { Script = script, Signers = [new Signer { Account = Custody, Scopes = WitnessScope.Global }], Attributes = [], Witnesses = [] };
                foreach (var trigger in new[] { TriggerType.Application, TriggerType.Verification })
                {
                    using var engine = ApplicationEngine.Create(trigger, tx, snapshot, Block(1000), Settings, gas: 1_000_000_000);
                    engine.LoadScript(trigger == TriggerType.Application ? script : SmartAccountProtocol.CreateVerificationScript(id));
                    engine.Execute(); Rejected(engine, "execution authority epoch or configuration nonce is stale");
                }
            }
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, 0]).GetInteger());
            Assert.AreEqual(0UL, ReadAccountState(snapshot, id).ConfigurationNonce);
        }
    }
}
