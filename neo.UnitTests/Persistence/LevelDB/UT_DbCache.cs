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


namespace Neo.UnitTests
{
    [TestClass]
    public class UT_DbCache
    {
        private Snapshot dbSnapshot;

        private LevelDBStore store;

        private static string DbPath => Path.GetFullPath(nameof(UT_DbCache) + string.Format("_Chain_{0}", 123456.ToString("X8")));

        [TestInitialize]
        public void TestSetup()
        {
            if (store == null)
            {
                store = new LevelDBStore(DbPath);
            }
            dbSnapshot = store.GetSnapshot();
        }

        [TestCleanup]
        public void TestEnd()
        {
            store.Dispose();
        }

        [ClassCleanup]
        public static void DeleteDir()
        {
            TestUtils.DeleteFile(DbPath);
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
            }
            catch (IO.Data.LevelDB.LevelDBException) { }
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
