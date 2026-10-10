// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.ThresholdRecovery.cs file belongs to the neo project and is free
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
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Neo.UnitTests.SmartContract.Native
{
    public partial class UT_AccountManagement
    {
        private sealed record ThresholdWallet(byte[] Script, KeyPair[] Keys,
            WitnessScope Scope = WitnessScope.CustomContracts)
        {
            internal UInt160 Account => Script.ToScriptHash();
        }

        private static KeyPair ThresholdFixtureKey(byte scalar)
        {
            // Reuse only public test scalars from Witness, TransactionCommitment and Clock.
            Assert.IsTrue(scalar is 2 or 3 or 19 or 41);
            byte[] secret = new byte[32]; secret[^1] = scalar;
            return new KeyPair(secret);
        }

        private static Transaction ThresholdTransaction(DataCache snapshot, string method, object[] args,
            params ThresholdWallet[] wallets)
        {
            Assert.AreEqual(wallets.Length, wallets.Select(wallet => wallet.Account).Distinct().Count());
            using var script = new ScriptBuilder();
            Push(script, args);
            script.EmitPush(CallFlags.All).EmitPush(method).EmitPush(NativeContract.AccountManagement.Hash)
                .EmitSysCall(ApplicationEngine.System_Contract_Call);
            var transaction = new Transaction
            {
                Version = 0,
                Script = script.ToArray(),
                SystemFee = 1_000_000_000,
                ValidUntilBlock = NativeContract.Ledger.CurrentIndex(snapshot) + 20,
                Signers = wallets.Select(wallet => new Signer
                {
                    Account = wallet.Account,
                    Scopes = wallet.Scope,
                    AllowedContracts = wallet.Scope == WitnessScope.CustomContracts
                        ? [NativeContract.AccountManagement.Hash] : []
                }).ToArray(),
                Attributes = [],
                Witnesses = wallets.Select(wallet => new Witness
                {
                    VerificationScript = wallet.Script,
                    InvocationScript = new byte[66 * (Neo.SmartContract.Helper.IsMultiSigContract(wallet.Script,
                        out int m, out int _) ? m : 1)]
                }).ToArray()
            };
            transaction.NetworkFee = transaction.CalculateNetworkFee(snapshot, Settings);
            for (int i = 0; i < wallets.Length; i++)
            {
                using var invocation = new ScriptBuilder();
                foreach (var key in wallets[i].Keys.OrderBy(key => key.PublicKey))
                    invocation.EmitPush(transaction.Sign(key, Settings.Network));
                transaction.Witnesses[i].InvocationScript = invocation.ToArray();
            }
            // Reparse the exported bytes so cached hashes or sizes cannot conceal a mutation.
            return transaction.ToArray().AsSerializable<Transaction>();
        }

        private static ApplicationEngine ThresholdApplication(DataCache snapshot, Transaction transaction, ulong time)
        {
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateIndependent(Settings));
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateDependent(Settings, snapshot,
                new TransactionVerificationContext(), []));
            // Also execute each ordinary wallet's Verification script in the VM, beyond the
            // standard-script fast paths above. Application consumes this same signed transaction.
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, 150_000_000));
            var block = Block(time);
            block.Header.Index = NativeContract.Ledger.CurrentIndex(snapshot) + 1;
            block.Header.PrevHash = NativeContract.Ledger.CurrentHash(snapshot);
            var engine = ApplicationEngine.Create(TriggerType.Application, transaction, snapshot, block,
                Settings, gas: transaction.SystemFee);
            engine.LoadScript(transaction.Script);
            engine.Execute();
            return engine;
        }

        private static StackItem ThresholdSuccess(DataCache snapshot, Transaction transaction, ulong time, string notification)
        {
            using var engine = ThresholdApplication(snapshot, transaction, time);
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            Assert.AreEqual(notification, engine.Notifications.Single().EventName);
            var result = engine.ResultStack.Count == 0 ? StackItem.Null : engine.ResultStack.Pop();
            engine.SnapshotCache.Commit();
            // Public, reproducible interoperability evidence; no private scalars are exported.
            Console.WriteLine("THRESHOLD_VECTOR " + JsonSerializer.Serialize(new
            {
                notification,
                networkMagic = Settings.Network,
                signData = Convert.ToHexString(transaction.GetSignData(Settings.Network)),
                rawTransaction = Convert.ToHexString(transaction.ToArray()),
                applicationScript = Convert.ToHexString(transaction.Script.Span),
                witnesses = transaction.Witnesses.Select((witness, index) => new
                {
                    account = transaction.Signers[index].Account.ToString(),
                    scriptHash = witness.ScriptHash.ToString(),
                    invocationScript = Convert.ToHexString(witness.InvocationScript.Span),
                    verificationScript = Convert.ToHexString(witness.VerificationScript.Span)
                })
            }));
            return result;
        }

        private static (DataCache Snapshot, UInt160 Id, ThresholdWallet Payer, ThresholdWallet OldCustody,
            KeyPair[] Members, ulong Time) ThresholdAccount()
        {
            var snapshot = Snapshot();
            KeyPair[] members = [ThresholdFixtureKey(2), ThresholdFixtureKey(3), ThresholdFixtureKey(19)];
            var custodyKey = ThresholdFixtureKey(41);
            var payer = new ThresholdWallet(Contract.CreateSignatureRedeemScript(custodyKey.PublicKey),
                [custodyKey], WitnessScope.None);
            // With this limited public fixture roster, losing member 2 or 3 also disables
            // old custody. Losing 19 only affects guardian availability. All role hashes differ.
            var oldCustody = new ThresholdWallet(Contract.CreateMultiSigRedeemScript(2,
                members.Take(2).Select(key => key.PublicKey).ToArray()), members.Take(2).ToArray());
            byte[] guardianScript = Contract.CreateMultiSigRedeemScript(2, members.Select(key => key.PublicKey).ToArray());
            var balance = snapshot.GetAndChange(NativeContract.GAS.CreateStorageKey(20, payer.Account),
                () => new StorageItem(new AccountState()));
            balance.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;
            ulong time = NativeContract.Ledger.GetHeader(snapshot, NativeContract.Ledger.CurrentHash(snapshot)).Timestamp + 10_000;
            var registration = ThresholdTransaction(snapshot, "registerAccount",
                [oldCustody.Account, new byte[32], UInt160.Zero, UInt160.Zero, guardianScript.ToScriptHash()], payer, oldCustody);
            var id = new UInt160(ThresholdSuccess(snapshot, registration, time, "AccountCreated").GetSpan());
            return (snapshot, id, payer, oldCustody, members, time);
        }

        [TestMethod]
        [TestCategory("ThresholdRecovery")]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void ThresholdGuardianEverySurvivingQuorumRecoversAndJointlyUnfreezes(int unavailableMember)
        {
            var (snapshot, id, payer, oldCustody, members, time) = ThresholdAccount();
            var guardian = new ThresholdWallet(Contract.CreateMultiSigRedeemScript(2,
                members.Select(key => key.PublicKey).ToArray()),
                members.Where((_, index) => index != unavailableMember).ToArray());
            var newCustody = payer with { Scope = WitnessScope.CustomContracts };
            var original = ReadAccountState(snapshot, id);

            var freeze = ThresholdTransaction(snapshot, "freeze", [id], payer, guardian);
            Assert.AreEqual(2, freeze.Signers.Length, "Guardian remains one signer, regardless of member count.");
            Assert.AreEqual(132, freeze.Witnesses[1].InvocationScript.Length);
            ThresholdSuccess(snapshot, freeze, time, "AccountFrozen");
            Assert.AreEqual(SmartAccountStatus.Frozen, ReadAccountState(snapshot, id).Status);

            ThresholdSuccess(snapshot, ThresholdTransaction(snapshot, "proposeRecovery", [id, newCustody.Account],
                payer, guardian), time, "RecoveryProposed");
            byte[] pending = ReadAccountState(snapshot, id).Serialize();
            var recovery = ThresholdTransaction(snapshot, "executeRecovery", [id], payer);
            using (var early = ThresholdApplication(snapshot, recovery, time + SmartAccountState.CustodyRecoveryDelayMs - 1))
                Rejected(early, "The intent is immature or stale.");
            Assert.AreSequenceEqual(pending, ReadAccountState(snapshot, id).Serialize());
            ulong mature = time + SmartAccountState.CustodyRecoveryDelayMs;
            ThresholdSuccess(snapshot, recovery, mature, "RecoveryExecuted");
            var recovered = ReadAccountState(snapshot, id);
            Assert.AreEqual(newCustody.Account, recovered.CustodyAddress);
            Assert.AreEqual(original.AccountId, recovered.AccountId);
            Assert.AreEqual(original.AccountAddress, recovered.AccountAddress);
            Assert.AreEqual(guardian.Account, recovered.RecoveryAddress);
            Assert.AreEqual(1UL, recovered.AuthorityEpoch);
            Assert.AreEqual(SmartAccountStatus.Frozen, recovered.Status, "Recovery does not unfreeze the account.");

            foreach (var signers in new ThresholdWallet[][]
            {
                [payer, guardian],                         // Payer's None scope is not custody authority.
                [newCustody],                              // Custody cannot replace guardian quorum.
                [payer, oldCustody, guardian],             // Even restored old keys no longer authorize custody.
                [newCustody, guardian with { Scope = WitnessScope.None }]
            })
            {
                var rejected = ThresholdTransaction(snapshot, "unfreeze", [id], signers);
                using var engine = ThresholdApplication(snapshot, rejected, mature);
                Rejected(engine, "Unfreeze requires custody and any configured recovery authority.");
                Assert.AreSequenceEqual(recovered.Serialize(), ReadAccountState(snapshot, id).Serialize());
            }
            ThresholdSuccess(snapshot, ThresholdTransaction(snapshot, "unfreeze", [id], newCustody, guardian),
                mature, "AccountUnfrozen");
            Assert.AreEqual(SmartAccountStatus.Active, ReadAccountState(snapshot, id).Status);
            Assert.AreEqual(1UL, ReadAccountState(snapshot, id).AuthorityEpoch);
        }

        [TestMethod]
        [TestCategory("ThresholdRecovery")]
        [DataRow("insufficient")]
        [DataRow("duplicate")]
        [DataRow("wrong-key")]
        [DataRow("wrong-order")]
        [DataRow("different-transaction")]
        public void ThresholdGuardianRejectsInvalidCryptographicWitnesses(string mutation)
        {
            var (snapshot, id, payer, _, members, _) = ThresholdAccount();
            var guardian = new ThresholdWallet(Contract.CreateMultiSigRedeemScript(2,
                members.Select(key => key.PublicKey).ToArray()), members.Take(2).ToArray());
            var transaction = ThresholdTransaction(snapshot, "freeze", [id], payer, guardian);
            byte[] state = ReadAccountState(snapshot, id).Serialize();
            var ordered = guardian.Keys.OrderBy(key => key.PublicKey).ToArray();
            byte[][] signatures = ordered.Select(key => transaction.Sign(key, Settings.Network)).ToArray();
            if (mutation == "insufficient") signatures = [signatures[0]];
            else if (mutation == "duplicate") signatures = [signatures[0], signatures[0]];
            else if (mutation == "wrong-key") signatures[1] = transaction.Sign(payer.Keys[0], Settings.Network);
            else if (mutation == "wrong-order") System.Array.Reverse(signatures);
            else
            {
                var other = transaction.ToArray().AsSerializable<Transaction>();
                other.Nonce++;
                signatures = ordered.Select(key => other.Sign(key, Settings.Network)).ToArray();
            }
            using (var invocation = new ScriptBuilder())
            {
                foreach (var signature in signatures) invocation.EmitPush(signature);
                transaction.Witnesses[1].InvocationScript = invocation.ToArray();
            }
            transaction = transaction.ToArray().AsSerializable<Transaction>();
            if (mutation == "insufficient")
            {
                // Nonstandard invocation shape falls through to actual VM verification.
                Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateIndependent(Settings));
                Assert.AreEqual(VerifyResult.Invalid, transaction.VerifyStateDependent(Settings, snapshot,
                    new TransactionVerificationContext(), []));
            }
            else Assert.AreEqual(VerifyResult.InvalidSignature, transaction.VerifyStateIndependent(Settings));
            Assert.IsFalse(transaction.VerifyWitnesses(Settings, snapshot, 150_000_000));
            Assert.AreSequenceEqual(state, ReadAccountState(snapshot, id).Serialize());
            Assert.AreEqual(SmartAccountStatus.Active, ReadAccountState(snapshot, id).Status);
        }
    }
}
