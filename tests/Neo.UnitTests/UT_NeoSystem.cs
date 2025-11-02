// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NeoSystem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Linq;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NeoSystem
    {
        private NeoSystem _system;

        [TestInitialize]
        public void Setup()
        {
            _system = TestBlockchain.GetSystem();
        }

        [TestMethod]
        public void TestGetBlockchain() => Assert.IsNotNull(_system.Blockchain);

        [TestMethod]
        public void TestGetLocalNode() => Assert.IsNotNull(_system.LocalNode);

        [TestMethod]
        public void TestGetTaskManager() => Assert.IsNotNull(_system.TaskManager);

        [TestMethod]
        public void TestAddAndGetService()
        {
            var service = new object();
            _system.AddService(service);

            var result = _system.GetService<object>();
            Assert.AreEqual(service, result);
        }

        [TestMethod]
        public void TestGetServiceWithFilter()
        {
            _system.AddService("match");
            _system.AddService("skip");

            var result = _system.GetService<string>(s => s == "match");
            Assert.AreEqual("match", result);
        }

        [TestMethod]
        public void TestResumeNodeStartup()
        {
            _system.SuspendNodeStartup();
            _system.SuspendNodeStartup();
            Assert.IsFalse(_system.ResumeNodeStartup());
            Assert.IsTrue(_system.ResumeNodeStartup()); // now it should resume
        }

        [TestMethod]
        public void TestStartNodeWhenNoSuspended()
        {
            var config = new ChannelsConfig();
            _system.StartNode(config);
        }

        [TestMethod]
        public void TestStartNodeWhenSuspended()
        {
            _system.SuspendNodeStartup();
            _system.SuspendNodeStartup();
            var config = new ChannelsConfig();
            _system.StartNode(config);
            Assert.IsFalse(_system.ResumeNodeStartup());
            Assert.IsTrue(_system.ResumeNodeStartup());
        }

        [TestMethod]
        public void TestEnsureStoppedStopsActor()
        {
            var sys = TestBlockchain.GetSystem();
            sys.EnsureStopped(sys.LocalNode);
        }

        [TestMethod]
        public void TestContainsTransactionNotExist()
        {
            var txHash = new UInt256(new byte[32]);
            var result = _system.ContainsTransaction(txHash);
            Assert.AreEqual(ContainsTransactionType.NotExist, result);
        }

        [TestMethod]
        public void TestContainsTransactionExistsInPool()
        {
            using var snapshot = _system.GetSnapshotCache();
            var wallet = TestUtils.GenerateTestWallet("mempool");
            var account = wallet.CreateAccount();

            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(account.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();

            var tx = TestUtils.CreateValidTx(snapshot, wallet, account.ScriptHash, 0);

            Assert.AreEqual(VerifyResult.Succeed, _system.MemPool.TryAdd(tx, _system.GetSnapshotCache()));
            var result = _system.ContainsTransaction(tx.Hash);
            Assert.AreEqual(ContainsTransactionType.ExistsInPool, result);
        }

        [TestMethod]
        public void TestContainsConflictHashDetectsValidConflicts()
        {
            var snapshot = _system.GetSnapshotCache();
            var walletA = TestUtils.GenerateTestWallet("abc");
            var accA = walletA.CreateAccount();
            var walletB = TestUtils.GenerateTestWallet("xyz");
            var accB = walletB.CreateAccount();

            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(accA.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();

            key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(accB.ScriptHash);
            entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();

            var tx1 = TestUtils.CreateValidTx(snapshot, walletA, accA.ScriptHash, 1);
            var tx2 = TestUtils.CreateValidTx(snapshot, walletA, accA.ScriptHash, 2);
            var tx3 = TestUtils.CreateValidTx(snapshot, walletB, accB.ScriptHash, 3);

            tx1.Attributes = [new Conflicts { Hash = tx2.Hash }, new Conflicts { Hash = tx3.Hash }];

            var block = new Block
            {
                Header = new Header
                {
                    Index = 10,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = Witness.Empty,
                },
                Transactions = [tx1],
            };

            byte[] onPersistScript;
            using (ScriptBuilder sb = new())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
                onPersistScript = sb.ToArray();
            }

            using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, block, _system.Settings, 0))
            {
                engine.LoadScript(onPersistScript);
                if (engine.Execute() != VMState.HALT) throw engine.FaultException;
                engine.SnapshotCache.Commit();
            }
            snapshot.Commit();

            byte[] postPersistScript;
            using (ScriptBuilder sb = new())
            {
                sb.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
                postPersistScript = sb.ToArray();
            }
            using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, block, _system.Settings, 0))
            {
                engine.LoadScript(postPersistScript);
                if (engine.Execute() != VMState.HALT) throw engine.FaultException;
                engine.SnapshotCache.Commit();
            }
            snapshot.Commit();

            Assert.IsTrue(_system.ContainsConflictHash(tx2.Hash, new[] { accA.ScriptHash }));
            Assert.IsFalse(_system.ContainsConflictHash(tx3.Hash, new[] { accB.ScriptHash }));
            Assert.IsFalse(_system.ContainsConflictHash(tx2.Hash, new[] { accB.ScriptHash }));
        }
    }
}
