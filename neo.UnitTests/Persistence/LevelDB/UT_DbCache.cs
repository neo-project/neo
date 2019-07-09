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

        private static string DbPath => Path.GetFullPath(string.Format("Chain_{0}", 123456.ToString("X8")));

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
        public void TestEnd() {
            store.Dispose();
        }

        [ClassCleanup]
        public static void DeleteDir()
        {
            TestUtils.DeleteFile(DbPath);
        }

        [TestMethod]
        public void TestDeleteInternal() {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = new ContractState
            {
                Script = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                Manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"))
            };

            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = store.GetContracts();
            //delete doesn't work because batch is null
            contracts.DeleteInternal(state.ScriptHash);
            Assert.IsNotNull(contracts.TryGet(state.ScriptHash));

            //delete should work because batch isn't null
            snapshot.Contracts.Delete(state.ScriptHash);
            snapshot.Contracts.DeleteInternal(state.ScriptHash);
            contracts = store.GetContracts();
            Assert.IsNull(contracts.TryGet(state.ScriptHash));


        }
    }
}
