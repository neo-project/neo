// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Helper.SmartAccount.cs belongs to the neo project and is licensed under MIT.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_Helper_SmartAccount
    {
        private static ProtocolSettings Settings => TestProtocolSettings.Default with
        {
            Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_SmartAccountV1, 0)
        };

        private static Transaction Transaction(params byte[][] verificationScripts) => new()
        {
            Version = 0,
            Nonce = 1,
            Script = new byte[] { (byte)OpCode.RET },
            Signers = System.Array.ConvertAll(verificationScripts, script => new Signer
            {
                Account = script.ToScriptHash(), Scopes = WitnessScope.None
            }),
            Attributes = [],
            ValidUntilBlock = 100,
            Witnesses = System.Array.ConvertAll(verificationScripts, script => new Witness
            {
                InvocationScript = ReadOnlyMemory<byte>.Empty, VerificationScript = script
            })
        };

        private static byte[] BoundedWitness(DataCache snapshot, long budget = 100_000_000, bool suffix = false)
        {
            var contract = TestUtils.GetContract(new byte[] { (byte)OpCode.PUSHT, (byte)OpCode.RET },
                TestUtils.CreateManifest("value", ContractParameterType.Boolean));
            if (NativeContract.ContractManagement.GetContract(snapshot, contract.Hash) is null) snapshot.AddContract(contract.Hash, contract);
            using var script = new ScriptBuilder();
            script.Emit(OpCode.NEWARRAY0).EmitPush(budget).EmitPush(CallFlags.ReadOnly).EmitPush("value")
                .EmitPush(contract.Hash).EmitSysCall(ApplicationEngine.System_Contract_CallWithGasLimit);
            if (suffix) script.Emit(OpCode.NOP);
            script.Emit(OpCode.RET);
            return script.ToArray();
        }

        private static long ByteFee(DataCache snapshot, Transaction transaction) =>
            transaction.Size * NativeContract.Policy.GetFeePerByte(snapshot);

        [TestMethod]
        public void CustomWitnessIncludesExactBytesAndVerificationExecution()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var transaction = Transaction(new byte[] { (byte)OpCode.PUSHT, (byte)OpCode.RET });
            Assert.IsTrue(transaction.VerifyWitness(Settings, snapshot, transaction.Sender, transaction.Witnesses[0],
                Neo.SmartContract.Helper.MaxVerificationGas, out long consumed));
            long quoted = transaction.CalculateNetworkFee(snapshot, Settings);
            Assert.AreEqual(ByteFee(snapshot, transaction) + consumed, quoted);
        }

        [TestMethod]
        public void BoundedWitnessQuoteCoversAdmissionPeakInsteadOfConsumedOnly()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var transaction = Transaction(BoundedWitness(snapshot));
            Assert.IsTrue(transaction.VerifyWitness(Settings, snapshot, transaction.Sender, transaction.Witnesses[0],
                Neo.SmartContract.Helper.MaxVerificationGas, out long consumed));
            Assert.IsLessThan(100_000_000L, consumed);
            Assert.IsFalse(transaction.VerifyWitnesses(Settings, snapshot, consumed), "Consumed GAS cannot admit the fixed child budget.");
            long quoted = transaction.CalculateNetworkFee(snapshot, Settings);
            long verificationFee = quoted - ByteFee(snapshot, transaction);
            Assert.IsGreaterThanOrEqualTo(100_000_000L, verificationFee);
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, verificationFee), "The fee quote must admit the exact witness.");
            Assert.IsFalse(transaction.VerifyWitnesses(Settings, snapshot, verificationFee - 1), "The fixed-budget admission boundary must round up exactly.");
        }

        [TestMethod]
        public void SequentialBoundedWitnessesUseConsumedPrefixAndNotSumOfLimits()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var transaction = Transaction(BoundedWitness(snapshot), BoundedWitness(snapshot, suffix: true));
            long quoted = transaction.CalculateNetworkFee(snapshot, Settings);
            long verificationFee = quoted - ByteFee(snapshot, transaction);
            Assert.IsGreaterThanOrEqualTo(100_000_000L, verificationFee);
            Assert.IsLessThan(Neo.SmartContract.Helper.MaxVerificationGas, verificationFee);
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, verificationFee));
            Assert.IsFalse(transaction.VerifyWitnesses(Settings, snapshot, verificationFee - 1));
        }

        [TestMethod]
        public void BoundedWitnessAboveGlobalVerificationCapCannotBeQuoted()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var transaction = Transaction(BoundedWitness(snapshot, Neo.SmartContract.Helper.MaxVerificationGas));
            Assert.Throws<ArgumentException>(() => transaction.CalculateNetworkFee(snapshot, Settings));
        }

        [TestMethod]
        public void InvalidCustomWitnessDoesNotReturnSuccessfulQuote()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            foreach (var script in new byte[][] {
                [(byte)OpCode.PUSHF, (byte)OpCode.RET], [(byte)OpCode.ABORT],
                [(byte)OpCode.PUSHT, (byte)OpCode.PUSHT, (byte)OpCode.RET], [(byte)OpCode.SYSCALL] })
            {
                var transaction = Transaction(script);
                Assert.Throws<ArgumentException>(() => transaction.CalculateNetworkFee(snapshot, Settings));
            }
            var mismatched = Transaction(new byte[] { (byte)OpCode.PUSHT, (byte)OpCode.RET });
            mismatched.Signers[0].Account = UInt160.Zero;
            Assert.Throws<ArgumentException>(() => mismatched.CalculateNetworkFee(snapshot, Settings));
            var extraWitness = Transaction(new byte[] { (byte)OpCode.PUSHT, (byte)OpCode.RET });
            extraWitness.Witnesses = [extraWitness.Witnesses[0], extraWitness.Witnesses[0]];
            Assert.Throws<ArgumentException>(() => extraWitness.CalculateNetworkFee(snapshot, Settings));
        }

        [TestMethod]
        [DataRow(OpCode.PUSH1)]
        [DataRow(OpCode.NEWARRAY0)]
        public void CustomWitnessUsesTheSameBooleanConversionAsConsensus(OpCode value)
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var transaction = Transaction(new byte[] { (byte)value, (byte)OpCode.RET });
            long quoted = transaction.CalculateNetworkFee(snapshot, Settings);
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, quoted - ByteFee(snapshot, transaction)));
        }

        [TestMethod]
        public void ApplicationMinimumFeeIncludesFixedCallbackAdmissionWithoutChangingLimit()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            byte[] script = BoundedWitness(snapshot, 250_000_000);
            long minimum;
            using (var estimate = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), settings: Settings, gas: 1_000_000_000))
            {
                estimate.LoadScript(script);
                Assert.AreEqual(VMState.HALT, estimate.Execute(), estimate.FaultException?.ToString());
                minimum = estimate.MinimumRequiredFee;
                Assert.IsGreaterThanOrEqualTo(250_000_000L, minimum);
                Assert.IsLessThan(minimum, estimate.FeeConsumed);
            }
            foreach (long supplied in new[] { minimum, minimum - 1 })
            {
                using var replay = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), settings: Settings, gas: supplied);
                replay.LoadScript(script);
                Assert.AreEqual(supplied == minimum ? VMState.HALT : VMState.FAULT, replay.Execute(), replay.FaultException?.ToString());
            }
        }

        [TestMethod]
        public void FaultedGasBurnKeepsFeeObservationRepresentable()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var script = new ScriptBuilder();
            script.EmitPush(long.MaxValue).EmitSysCall(ApplicationEngine.System_Runtime_BurnGas);
            using var engine = ApplicationEngine.Run(script.ToArray(), snapshot, settings: Settings, gas: 1_000_000_000);
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.IsGreaterThanOrEqualTo(0L, engine.MinimumRequiredFee);
            Assert.IsLessThanOrEqualTo(1_000_000_000L, engine.MinimumRequiredFee);
        }

        [TestMethod]
        public void WalletConstructsSystemFeeThatAdmitsTheActualBoundedExecution()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            // This is an in-memory test wallet; no file is saved and no transaction is broadcast.
            var wallet = new NEP6Wallet(null, "test-only", Settings);
            byte[] secret = new byte[32]; secret[^1] = 4;
            var account = wallet.CreateAccount(secret);
            var balance = snapshot.GetAndChange(NativeContract.GAS.CreateStorageKey(20, account.ScriptHash),
                () => new StorageItem(new AccountState()));
            balance.GetInteroperable<AccountState>().Balance = 1_000_000_000;
            byte[] script = BoundedWitness(snapshot, 250_000_000);
            var transaction = wallet.MakeTransaction(snapshot, script, account.ScriptHash, maxGas: 1_000_000_000);
            Assert.IsGreaterThanOrEqualTo(250_000_000L, transaction.SystemFee);
            using var replay = ApplicationEngine.Run(script, snapshot.CloneCache(), transaction,
                settings: Settings, gas: transaction.SystemFee);
            Assert.AreEqual(VMState.HALT, replay.State, replay.FaultException?.ToString());
            Assert.IsTrue(replay.ResultStack.Peek().GetBoolean());
        }

        [TestMethod]
        public void WalletRejectsCombinedFeeOverflowEvenWhenTheAccountHasEnoughGas()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var wallet = new NEP6Wallet(null, "test-only", Settings);
            byte[] secret = new byte[32]; secret[^1] = 5;
            var account = wallet.CreateAccount(secret);
            var balance = snapshot.GetAndChange(NativeContract.GAS.CreateStorageKey(20, account.ScriptHash),
                () => new StorageItem(new AccountState()));
            balance.GetInteroperable<AccountState>().Balance = (System.Numerics.BigInteger)long.MaxValue * 2;
            byte[] script = BoundedWitness(snapshot, long.MaxValue - 1_000_000);
            var error = Assert.Throws<InvalidOperationException>(() => wallet.MakeTransaction(snapshot,
                script, account.ScriptHash, maxGas: long.MaxValue));
            Assert.Contains("combined transaction fees", error.Message);
        }

        [TestMethod]
        public void FeeChargeWithoutBoundedContextDoesNotAllocateBudgetIterators()
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null,
                TestBlockchain.GetTestSnapshotCache(), settings: Settings);
            for (int i = 0; i < 1000; i++) engine.AddFee(0, false);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10_000; i++) engine.AddFee(0, false);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.IsLessThan(1024L, allocated, "The unbounded hot path must not allocate an iterator per GAS charge.");
        }

        [TestMethod]
        public void StandardSponsorSignsFeesAndExactCustomWitnessTransaction()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var privateKey = new byte[32]; privateKey[^1] = 1;
            var key = new KeyPair(privateKey);
            var transaction = Transaction(Contract.CreateSignatureRedeemScript(key.PublicKey), BoundedWitness(snapshot));
            using (var placeholder = new ScriptBuilder())
                transaction.Witnesses[0].InvocationScript = placeholder.EmitPush(new byte[64]).ToArray();
            transaction.NetworkFee = transaction.CalculateNetworkFee(snapshot, Settings);
            using (var invocation = new ScriptBuilder())
                transaction.Witnesses[0].InvocationScript = invocation.EmitPush(transaction.Sign(key, Settings.Network)).ToArray();
            Assert.AreEqual(VerifyResult.Succeed, transaction.VerifyStateIndependent(Settings));
            Assert.IsTrue(transaction.VerifyWitnesses(Settings, snapshot, transaction.NetworkFee - ByteFee(snapshot, transaction)));
            transaction.NetworkFee++;
            Assert.AreEqual(VerifyResult.InvalidSignature, transaction.VerifyStateIndependent(Settings));
        }
    }
}
