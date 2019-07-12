using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence.LevelDB;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Neo.Network.P2P.Payloads;
using Neo.Ledger;
using Neo.IO.Caching;
using Neo.SmartContract.Manifest;
using System.Threading;

namespace Neo.UnitTests.Persistence.LevelDB
{
    [TestClass]
    public class UT_DbCache
    {
        private LevelDBStore store;

        private string dbPath;

        [TestInitialize]
        public void TestSetup()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            dbPath = Path.GetFullPath(nameof(UT_DbCache) + string.Format("_Chain_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
            if (store == null)
            {
                store = new LevelDBStore(dbPath);
            }
        }

        [TestCleanup]
        public void DeleteDir()
        {
            store.Dispose();
            store = null;
            TestUtils.DeleteFile(dbPath);
        }

        [TestMethod]
        public void TestDeleteInternal()
        {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = CreateTestContractState();

            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = store.GetContracts();
            //delete doesn't work because snapshot not commit
            snapshot.Contracts.Delete(state.ScriptHash);
            contracts.DeleteInternal(state.ScriptHash);
            Assert.IsNotNull(contracts.TryGet(state.ScriptHash));

            //delete should work because batch isn't null
            snapshot.Contracts.Delete(state.ScriptHash);
            snapshot.Contracts.DeleteInternal(state.ScriptHash);
            snapshot.Commit();
            contracts = store.GetContracts();
            Assert.IsNull(contracts.TryGet(state.ScriptHash));
        }

        [TestMethod]
        public void TestGetInternal()
        {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = CreateTestContractState();

            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = store.GetContracts();
            var contractState = contracts[state.ScriptHash];
            Assert.AreEqual(state.Script.ToHexString(), contractState.Script.ToHexString());
            Assert.AreEqual(state.Manifest.Abi.Hash, contractState.Manifest.Abi.Hash);
            Assert.AreEqual(state.Manifest.ToString(), contractState.Manifest.ToString());

            //test key not found
            ContractState state2 = new ContractState
            {
                Script = new byte[] { 0x04, 0x03, 0x02, 0x01 },
                Manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"))
            };
            try
            {
                var contractState2 = contracts[state2.ScriptHash];
                Assert.Fail();
            }
            catch (Neo.IO.Data.LevelDB.LevelDBException) { }
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = CreateTestContractState();

            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = store.GetContracts();
            var contractState = contracts.TryGet(state.ScriptHash);
            Assert.AreEqual(state.Script.ToHexString(), contractState.Script.ToHexString());
            Assert.AreEqual(state.Manifest.Abi.Hash, contractState.Manifest.Abi.Hash);
            Assert.AreEqual(state.Manifest.ToString(), contractState.Manifest.ToString());

            //test key not found
            ContractState state2 = new ContractState
            {
                Script = new byte[] { 0x04, 0x03, 0x02, 0x01 },
                Manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"))
            };
            var contractState2 = contracts.TryGet(state2.ScriptHash);
            Assert.IsNull(contractState2);
        }

        [TestMethod]
        public void TestFindInternal()
        {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = CreateTestContractState();
            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = store.GetContracts();
            var ret = contracts.Find();
            foreach (var pair in ret)
            {
                Assert.AreEqual(pair.Key, state.ScriptHash);
                Assert.AreEqual(pair.Value.Script.ToHexString(), state.Script.ToHexString());
                Assert.AreEqual(pair.Value.Manifest.ToString(), state.Manifest.ToString());
            }
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = CreateTestContractState();
            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = store.GetContracts();
            snapshot = store.GetSnapshot();
            var storeState = snapshot.Contracts.GetAndChange(state.ScriptHash);
            storeState.Manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff11"));
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts2 = store.GetContracts();
            var updatedState = contracts2.TryGet(state.ScriptHash);
            Assert.AreEqual(updatedState.Manifest.ToString(), storeState.Manifest.ToString());
            Assert.AreEqual(updatedState.Script.ToHexString(), storeState.Script.ToHexString());

        }

        private static ContractState CreateTestContractState()
        {
            return new ContractState
            {
                Script = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                Manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"))
            };
        }
    }
}
