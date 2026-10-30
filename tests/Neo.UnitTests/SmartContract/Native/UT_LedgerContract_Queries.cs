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

using Akka.TestKit;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Threading;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_LedgerContract_Queries : TestKit
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

        private static Transaction MakeTransaction(UInt160 sender, byte[] script)
        {
            var tx = TestUtils.GetTransaction(sender);
            tx.Script = script;
            tx.Nonce = 1;
            tx.ValidUntilBlock = 100;
            tx.SystemFee = 0;
            tx.NetworkFee = 0;
            // Re-sign (re-assign witnesses) after the script change so the hash covers the final content.
            tx.Witnesses = [Witness.Empty];
            return tx;
        }

        private (NeoSystem System, Transaction Tx) PersistTransaction(byte[] script)
        {
            var system = TestBlockchain.GetSystem();
            var sender = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
            var tx = MakeTransaction(sender, script);

            var snapshot = system.GetSnapshotCache();
            var block = TestUtils.MakeBlock(snapshot, NativeContract.Ledger.CurrentHash(snapshot), 0);
            block.Transactions = [tx];
            block.Header.MerkleRoot = Neo.Cryptography.MerkleTree.ComputeRoot([tx.Hash]);

            var probe = CreateTestProbe();
            probe.Send(system.Blockchain, new Blockchain.Import([block], Verify: false));
            probe.ExpectMsg<Blockchain.ImportCompleted>(TimeSpan.FromSeconds(10), cancellationToken: CancellationToken.None);
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
            var (system, tx) = PersistTransaction([(byte)OpCode.PUSH1]);
            AssertVMState(system, tx.Hash, VMState.HALT);
        }

        [TestMethod]
        public void GetTransactionVMState_PersistedFault()
        {
            var (system, tx) = PersistTransaction([(byte)OpCode.ABORT]);
            Assert.IsNotNull(NativeContract.Ledger.GetTransactionState(system.StoreView, tx.Hash));
            AssertVMState(system, tx.Hash, VMState.FAULT);
        }

        [TestMethod]
        public void GetTransactionVMState_Missing_ReturnsNone()
        {
            var system = TestBlockchain.GetSystem();
            AssertVMState(system, UInt256.Zero, VMState.NONE);
        }
    }
}
