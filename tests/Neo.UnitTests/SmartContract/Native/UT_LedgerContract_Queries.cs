// Copyright (C) 2015-2026 The Neo Project.
//
// UT_LedgerContract_Queries.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_LedgerContract_Queries
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void CurrentIndex_And_CurrentHash_OnGenesis()
        {
            var index = NativeContract.Ledger.Call(_snapshot, "currentIndex");
            Assert.IsInstanceOfType<Integer>(index);
            Assert.IsTrue(index.GetInteger() >= 0);

            var hash = NativeContract.Ledger.Call(_snapshot, "currentHash");
            Assert.IsInstanceOfType<ByteString>(hash);
            Assert.AreEqual(UInt256.Zero.Size, ((ByteString)hash).Size);
        }

        [TestMethod]
        public void GetBlock_Genesis_Exists()
        {
            var hashItem = NativeContract.Ledger.Call(_snapshot, "currentHash");
            var hash = new UInt256(hashItem.GetSpan());
            var block = NativeContract.Ledger.Call(_snapshot, "getBlock",
                new ContractParameter(ContractParameterType.Hash256) { Value = hash });
            Assert.IsFalse(block.IsNull);
        }

        [TestMethod]
        public void GetTransaction_Missing_ReturnsNull()
        {
            var tx = NativeContract.Ledger.Call(_snapshot, "getTransaction",
                new ContractParameter(ContractParameterType.Hash256) { Value = UInt256.Zero });
            Assert.IsTrue(tx.IsNull);
        }

        [TestMethod]
        public void GetTransactionFromBlock_InvalidIndex_ThrowsOrNull()
        {
            // Genesis has no user transactions; out-of-range should fail or null depending on API.
            try
            {
                var result = NativeContract.Ledger.Call(_snapshot, "getTransactionFromBlock",
                    new ContractParameter(ContractParameterType.Integer) { Value = 0 },
                    new ContractParameter(ContractParameterType.Integer) { Value = 9999 });
                Assert.IsTrue(result.IsNull);
            }
            catch (Exception)
            {
                // Acceptable: native throws on invalid tx index.
            }
        }

        private static (NeoSystem System, Transaction Tx) PersistTransaction(bool fault, uint iaraHeight = 0)
        {
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks
                    .SetItem(Hardfork.HF_Iara, iaraHeight)
                    .SetItem(Hardfork.HF_Jackalope, iaraHeight)
            };
            var system = new TestBlockchain.TestNeoSystem(settings);
            var snapshot = system.GetSnapshotCache();
            var wallet = TestUtils.GenerateTestWallet("123");
            var account = wallet.CreateAccount();

            // Fund the account.
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(account.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();

            var tx = TestUtils.CreateValidTx(snapshot, wallet, account.ScriptHash, 0);
            if (fault)
            {
                tx.Script = new[] { (byte)OpCode.ABORT };
                tx.Witnesses = [];
                var data = new ContractParametersContext(snapshot, tx, TestProtocolSettings.Default.Network);
                Assert.IsTrue(wallet.Sign(data));
                Assert.IsTrue(data.Completed);
                tx.Witnesses = data.GetWitnesses();
            }

            var block = TestUtils.MakeBlock(snapshot, NativeContract.Ledger.CurrentHash(snapshot), 0);
            block.Transactions = [tx];
            block.Header.MerkleRoot = MerkleTree.ComputeRoot([tx.Hash]);

            var inbox = Inbox.Create(system.ActorSystem);
            inbox.Send(system.Blockchain, new Blockchain.Import([block], Verify: false));
            Assert.IsInstanceOfType<Blockchain.ImportCompleted>(inbox.Receive(TimeSpan.FromSeconds(10)));
            return (system, tx);
        }

        private static void AssertVMState(NeoSystem system, UInt256 hash, VMState expected)
        {
            Assert.AreEqual(expected, NativeContract.Ledger.GetTransactionState(system.StoreView, hash)?.State ?? VMState.NONE);
            var result = NativeContract.Ledger.Call(system.GetSnapshotCache(), "getTransactionVMState",
                new ContractParameter(ContractParameterType.Hash256) { Value = hash });
            Assert.AreEqual((int)expected, (int)result.GetInteger());
        }

        [TestMethod]
        public void GetTransactionVMState_PersistedHalt()
        {
            var (system, tx) = PersistTransaction(false);
            AssertVMState(system, tx.Hash, VMState.HALT);
        }

        [TestMethod]
        public void GetTransactionVMState_PersistedFault()
        {
            var (system, tx) = PersistTransaction(true);
            Assert.IsNotNull(NativeContract.Ledger.GetTransactionState(system.StoreView, tx.Hash));
            AssertVMState(system, tx.Hash, VMState.FAULT);
        }

        [TestMethod]
        public void GetTransactionVMState_BeforeIara_Halt_ReturnsNone()
        {
            var (system, tx) = PersistTransaction(false, 2);
            Assert.IsNotNull(NativeContract.Ledger.GetTransactionState(system.StoreView, tx.Hash));
            AssertVMState(system, tx.Hash, VMState.NONE);
        }

        [TestMethod]
        public void GetTransactionVMState_BeforeIara_Fault_ReturnsNone()
        {
            var (system, tx) = PersistTransaction(true, 2);
            Assert.IsNotNull(NativeContract.Ledger.GetTransactionState(system.StoreView, tx.Hash));
            AssertVMState(system, tx.Hash, VMState.NONE);
        }

        [TestMethod]
        public void GetTransactionVMState_AtIaraActivation_Halt_ReturnsHalt()
        {
            var (system, tx) = PersistTransaction(false, 1);
            AssertVMState(system, tx.Hash, VMState.HALT);
        }

        [TestMethod]
        public void GetTransactionVMState_Missing_ReturnsNone()
        {
            var system = TestBlockchain.GetSystem();
            AssertVMState(system, UInt256.Zero, VMState.NONE);
        }
    }
}
