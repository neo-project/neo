// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Notary.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using VMTypes = Neo.VM.Types;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_Notary
    {
        private DataCache _snapshot;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshot = TestBlockchain.GetTestSnapshot();
            _persistingBlock = new Block { Header = new Header() };
        }

        [TestMethod]
        public void Check_Name() => NativeContract.Notary.Name.Should().Be(nameof(Notary));

        [TestMethod]
        public void Check_OnNEP17Payment()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

            // Set proper current index for deposit's Till parameter check.
            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            // Non-GAS transfer should fail.
            Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => NativeContract.NEO.Transfer(snapshot, from, NativeContract.Notary.Hash.ToArray(), BigInteger.Zero, true, persistingBlock));

            // GAS transfer with invalid data format should fail.
            Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => NativeContract.GAS.Transfer(snapshot, from, NativeContract.Notary.Hash.ToArray(), BigInteger.Zero, true, persistingBlock, 5));

            // GAS transfer with wrong number of data elements should fail.
            var data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Boolean, Value = true } } };
            Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => NativeContract.GAS.Transfer(snapshot, from, NativeContract.Notary.Hash.ToArray(), BigInteger.Zero, true, persistingBlock, data));

            // Gas transfer with invalid Till parameter should fail.
            data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = persistingBlock.Index } } };
            Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), BigInteger.Zero, true, persistingBlock, data));

            // Insufficient first deposit.
            data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = persistingBlock.Index + 100 } } };
            Assert.ThrowsException<System.Reflection.TargetInvocationException>(() => NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), 2 * 1000_0000 - 1, true, persistingBlock, data));

            // Good deposit.
            data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = persistingBlock.Index + 100 } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), 2 * 1000_0000 + 1, true, persistingBlock, data));
        }

        [TestMethod]
        public void Check_ExpirationOf()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

            // Set proper current index for deposit's Till parameter check.
            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            // Check that 'till' of an empty deposit is 0 by default.
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(0);

            // Make initial deposit.
            var till = persistingBlock.Index + 123;
            var data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = till } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), 2 * 1000_0000 + 1, true, persistingBlock, data));

            // Ensure deposit's 'till' value is properly set.
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(till);

            // Make one more deposit with updated 'till' parameter.
            till += 5;
            data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = till } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), 5, true, persistingBlock, data));

            // Ensure deposit's 'till' value is properly updated.
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(till);

            // Make deposit to some side account with custom 'till' value.
            UInt160 to = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
            data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Hash160, Value = to }, new ContractParameter { Type = ContractParameterType.Integer, Value = till } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), 2 * 1000_0000 + 1, true, persistingBlock, data));

            // Default 'till' value should be set for to's deposit.
            var defaultDeltaTill = 5760;
            Call_ExpirationOf(snapshot, to.ToArray(), persistingBlock).Should().Be(persistingBlock.Index - 1 + defaultDeltaTill);

            // Withdraw own deposit.
            persistingBlock.Header.Index = till + 1;
            var currentBlock = snapshot.GetAndChange(storageKey, () => new StorageItem(new HashIndexState()));
            currentBlock.GetInteroperable<HashIndexState>().Index = till + 1;
            Call_Withdraw(snapshot, from, from, persistingBlock);

            // Check that 'till' value is properly updated.
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(0);
        }

        [TestMethod]
        public void Check_LockDepositUntil()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();

            // Set proper current index for deposit's Till parameter check.
            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            // Check that 'till' of an empty deposit is 0 by default.
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(0);

            // Update `till` value of an empty deposit should fail.
            Call_LockDepositUntil(snapshot, from, 123, persistingBlock).Should().Be(false);
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(0);

            // Make initial deposit.
            var till = persistingBlock.Index + 123;
            var data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = till } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), 2 * 1000_0000 + 1, true, persistingBlock, data));

            // Ensure deposit's 'till' value is properly set.
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(till);

            // Update deposit's `till` value for side account should fail.
            UInt160 other = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
            Call_LockDepositUntil(snapshot, other.ToArray(), till + 10, persistingBlock).Should().Be(false);
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(till);

            // Decrease deposit's `till` value should fail.
            Call_LockDepositUntil(snapshot, from, till - 1, persistingBlock).Should().Be(false);
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(till);

            // Good.
            till += 10;
            Call_LockDepositUntil(snapshot, from, till, persistingBlock).Should().Be(true);
            Call_ExpirationOf(snapshot, from, persistingBlock).Should().Be(till);
        }

        [TestMethod]
        public void Check_BalanceOf()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            UInt160 fromAddr = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
            byte[] from = fromAddr.ToArray();

            // Set proper current index for deposit expiration.
            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));

            // Ensure that default deposit is 0.
            Call_BalanceOf(snapshot, from, persistingBlock).Should().Be(0);

            // Make initial deposit.
            var till = persistingBlock.Index + 123;
            var deposit1 = 2 * 1_0000_0000;
            var data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = till } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), deposit1, true, persistingBlock, data));

            // Ensure value is deposited.
            Call_BalanceOf(snapshot, from, persistingBlock).Should().Be(deposit1);

            // Make one more deposit with updated 'till' parameter.
            var deposit2 = 5;
            data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Any }, new ContractParameter { Type = ContractParameterType.Integer, Value = till } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), deposit2, true, persistingBlock, data));

            // Ensure deposit's 'till' value is properly updated.
            Call_BalanceOf(snapshot, from, persistingBlock).Should().Be(deposit1 + deposit2);

            // Make deposit to some side account.
            UInt160 to = UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4");
            data = new ContractParameter { Type = ContractParameterType.Array, Value = new List<ContractParameter>() { new ContractParameter { Type = ContractParameterType.Hash160, Value = to }, new ContractParameter { Type = ContractParameterType.Integer, Value = till } } };
            Assert.IsTrue(NativeContract.GAS.TransferWithTransaction(snapshot, from, NativeContract.Notary.Hash.ToArray(), deposit1, true, persistingBlock, data));

            Call_BalanceOf(snapshot, to.ToArray(), persistingBlock).Should().Be(deposit1);

            // Process some Notary transaction and check that some deposited funds have been withdrawn.
            var tx1 = TestUtils.GetTransaction(NativeContract.Notary.Hash, fromAddr);
            tx1.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = 4 } };
            tx1.NetworkFee = 1_0000_0000;

            // Build block to check transaction fee distribution during Gas OnPersist.
            persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Transactions = new Transaction[] { tx1 }
            };
            // Designate Notary node.
            byte[] privateKey1 = new byte[32];
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var ret = NativeContract.RoleManagement.Call(
                snapshot,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                new Block { Header = new Header() },
                "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.P2PNotary) },
                new ContractParameter(ContractParameterType.Array)
                {
                    Value = new List<ContractParameter>(){
                    new ContractParameter(ContractParameterType.ByteArray){Value = key1.PublicKey.ToArray()},
                }
                }
            );
            snapshot.Commit();

            // Execute OnPersist script.
            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.IsTrue(engine.Execute() == VMState.HALT);
            snapshot.Commit();

            // Check that transaction's fees were paid by from's deposit.
            Call_BalanceOf(snapshot, from, persistingBlock).Should().Be(deposit1 + deposit2 - tx1.NetworkFee - tx1.SystemFee);

            // Withdraw own deposit.
            persistingBlock.Header.Index = till + 1;
            var currentBlock = snapshot.GetAndChange(storageKey, () => new StorageItem(new HashIndexState()));
            currentBlock.GetInteroperable<HashIndexState>().Index = till + 1;
            Call_Withdraw(snapshot, from, from, persistingBlock);

            // Check that no deposit is left.
            Call_BalanceOf(snapshot, from, persistingBlock).Should().Be(0);
        }

        internal static BigInteger Call_BalanceOf(DataCache snapshot, byte[] address, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Notary.Hash, "balanceOf", address);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return result.GetInteger();
        }

        internal static BigInteger Call_ExpirationOf(DataCache snapshot, byte[] address, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Notary.Hash, "expirationOf", address);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));

            return result.GetInteger();
        }

        internal static bool Call_LockDepositUntil(DataCache snapshot, byte[] address, uint till, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, new Transaction() { Signers = new Signer[] { new Signer() { Account = new UInt160(address), Scopes = WitnessScope.Global } }, Attributes = System.Array.Empty<TransactionAttribute>() }, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Notary.Hash, "lockDepositUntil", address, till);
            engine.LoadScript(script.ToArray());

            engine.Execute().Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return result.GetBoolean();
        }

        internal static bool Call_Withdraw(DataCache snapshot, byte[] from, byte[] to, Block persistingBlock)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, new Transaction() { Signers = new Signer[] { new Signer() { Account = new UInt160(from), Scopes = WitnessScope.Global } }, Attributes = System.Array.Empty<TransactionAttribute>() }, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Notary.Hash, "withdraw", from, to);
            engine.LoadScript(script.ToArray());

            if (engine.Execute() != VMState.HALT)
            {
                throw engine.FaultException;
            }

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Boolean));

            return result.GetBoolean();
        }

        [TestMethod]
        public void Check_GetMaxNotValidBeforeDelta()
        {
            const int defaultMaxNotValidBeforeDelta = 140;
            NativeContract.Notary.GetMaxNotValidBeforeDelta(_snapshot).Should().Be(defaultMaxNotValidBeforeDelta);
        }

        [TestMethod]
        public void Check_SetMaxNotValidBeforeDelta()
        {
            var snapshot = _snapshot.CreateSnapshot();
            var persistingBlock = new Block { Header = new Header { Index = 1000 } };
            UInt160 committeeAddress = NativeContract.NEO.GetCommitteeAddress(snapshot);

            using var engine = ApplicationEngine.Create(TriggerType.Application, new Nep17NativeContractExtensions.ManualWitness(committeeAddress), snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Notary.Hash, "setMaxNotValidBeforeDelta", 100);
            engine.LoadScript(script.ToArray());
            VMState vMState = engine.Execute();
            vMState.Should().Be(VMState.HALT);
            NativeContract.Notary.GetMaxNotValidBeforeDelta(snapshot).Should().Be(100);
        }

        [TestMethod]
        public void Check_OnPersist_FeePerKeyUpdate()
        {
            // Hardcode test values.
            const uint defaultNotaryAssistedFeePerKey = 1000_0000;
            const uint newNotaryAssistedFeePerKey = 5000_0000;
            const byte NKeys = 4;

            // Generate one transaction with NotaryAssisted attribute with hardcoded NKeys values.
            var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
            var tx2 = TestUtils.GetTransaction(from);
            tx2.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = NKeys } };
            var netFee = 1_0000_0000; // enough to cover defaultNotaryAssistedFeePerKey, but not enough to cover newNotaryAssistedFeePerKey.
            tx2.NetworkFee = netFee;
            tx2.SystemFee = 1000_0000;

            // Calculate overall expected Notary nodes reward.
            var expectedNotaryReward = (NKeys + 1) * defaultNotaryAssistedFeePerKey;

            // Build block to check transaction fee distribution during Gas OnPersist.
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Transactions = new Transaction[] { tx2 }
            };
            var snapshot = _snapshot.CreateSnapshot();

            // Designate Notary node.
            byte[] privateKey1 = new byte[32];
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var ret = NativeContract.RoleManagement.Call(
                snapshot,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                new Block { Header = new Header() },
                "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.P2PNotary) },
                new ContractParameter(ContractParameterType.Array)
                {
                    Value = new List<ContractParameter>(){
                    new ContractParameter(ContractParameterType.ByteArray){Value = key1.PublicKey.ToArray()}
                }
                }
            );
            snapshot.Commit();

            // Imitate Blockchain's Persist behaviour: OnPersist + transactions processing.
            // Execute OnPersist firstly:
            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());
            Assert.IsTrue(engine.Execute() == VMState.HALT);
            snapshot.Commit();

            // Process transaction that changes NotaryServiceFeePerKey after OnPersist.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), persistingBlock,
                "setAttributeFee", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.NotaryAssisted }, new ContractParameter(ContractParameterType.Integer) { Value = newNotaryAssistedFeePerKey });
            ret.IsNull.Should().BeTrue();
            snapshot.Commit();

            // Process tx2 with NotaryAssisted attribute.
            engine = ApplicationEngine.Create(TriggerType.Application, tx2, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings, tx2.SystemFee);
            engine.LoadScript(tx2.Script);
            Assert.IsTrue(engine.Execute() == VMState.HALT);
            snapshot.Commit();

            // Ensure that Notary reward is distributed based on the old value of NotaryAssisted price
            // and no underflow happens during GAS distribution.
            ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
            var primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
            NativeContract.GAS.BalanceOf(snapshot, primary).Should().Be(netFee - expectedNotaryReward);
            NativeContract.GAS.BalanceOf(engine.Snapshot, key1.PublicKey.EncodePoint(true).ToScriptHash()).Should().Be(expectedNotaryReward);
        }

        [TestMethod]
        public void Check_OnPersist_NotaryRewards()
        {
            // Hardcode test values.
            const uint defaultNotaryssestedFeePerKey = 1000_0000;
            const byte NKeys1 = 4;
            const byte NKeys2 = 6;

            // Generate two transactions with NotaryAssisted attributes with hardcoded NKeys values.
            var from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
            var tx1 = TestUtils.GetTransaction(from);
            tx1.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = NKeys1 } };
            var netFee1 = 1_0000_0000;
            tx1.NetworkFee = netFee1;
            var tx2 = TestUtils.GetTransaction(from);
            tx2.Attributes = new TransactionAttribute[] { new NotaryAssisted() { NKeys = NKeys2 } };
            var netFee2 = 2_0000_0000;
            tx2.NetworkFee = netFee2;

            // Calculate overall expected Notary nodes reward.
            var expectedNotaryReward = (NKeys1 + 1) * defaultNotaryssestedFeePerKey + (NKeys2 + 1) * defaultNotaryssestedFeePerKey;

            // Build block to check transaction fee distribution during Gas OnPersist.
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = (uint)TestProtocolSettings.Default.CommitteeMembersCount,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                },
                Transactions = new Transaction[] { tx1, tx2 }
            };
            var snapshot = _snapshot.CreateSnapshot();

            // Designate several Notary nodes.
            byte[] privateKey1 = new byte[32];
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            rng.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var ret = NativeContract.RoleManagement.Call(
                snapshot,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                new Block { Header = new Header() },
                "designateAsRole",
                new ContractParameter(ContractParameterType.Integer) { Value = new BigInteger((int)Role.P2PNotary) },
                new ContractParameter(ContractParameterType.Array)
                {
                    Value = new List<ContractParameter>(){
                    new ContractParameter(ContractParameterType.ByteArray){Value = key1.PublicKey.ToArray()},
                    new ContractParameter(ContractParameterType.ByteArray){Value = key2.PublicKey.ToArray()},
                }
                }
            );
            snapshot.Commit();

            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            var engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestBlockchain.TheNeoSystem.Settings);

            // Check that block's Primary balance is 0.
            ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(engine.Snapshot, engine.ProtocolSettings.ValidatorsCount);
            var primary = Contract.CreateSignatureRedeemScript(validators[engine.PersistingBlock.PrimaryIndex]).ToScriptHash();
            NativeContract.GAS.BalanceOf(engine.Snapshot, primary).Should().Be(0);

            // Execute OnPersist script.
            engine.LoadScript(script.ToArray());
            Assert.IsTrue(engine.Execute() == VMState.HALT);

            // Check that proper amount of GAS was minted to block's Primary and the rest
            // is evenly devided between designated Notary nodes as a reward.
            Assert.AreEqual(2 + 1 + 2, engine.Notifications.Count()); // burn tx1 and tx2 network fee + mint primary reward + transfer reward to Notary1 and Notary2
            Assert.AreEqual(netFee1 + netFee2 - expectedNotaryReward, engine.Notifications[2].State[2]);
            NativeContract.GAS.BalanceOf(engine.Snapshot, primary).Should().Be(netFee1 + netFee2 - expectedNotaryReward);
            Assert.AreEqual(expectedNotaryReward / 2, engine.Notifications[3].State[2]);
            NativeContract.GAS.BalanceOf(engine.Snapshot, key1.PublicKey.EncodePoint(true).ToScriptHash()).Should().Be(expectedNotaryReward / 2);
            Assert.AreEqual(expectedNotaryReward / 2, engine.Notifications[4].State[2]);
            NativeContract.GAS.BalanceOf(engine.Snapshot, key2.PublicKey.EncodePoint(true).ToScriptHash()).Should().Be(expectedNotaryReward / 2);
        }

        internal static StorageKey CreateStorageKey(byte prefix, uint key)
        {
            return CreateStorageKey(prefix, BitConverter.GetBytes(key));
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
            buffer[0] = prefix;
            key?.CopyTo(buffer.AsSpan(1));
            return new()
            {
                Id = NativeContract.GAS.Id,
                Key = buffer
            };
        }
    }
}
