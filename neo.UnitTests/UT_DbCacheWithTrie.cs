using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Persistence.LevelDB;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.Persistence.LevelDb
{
    [TestClass]
    public class UT_DbCacheWithTrie
    {
        private LevelDBStore store;

        [TestInitialize]
        public void TestInitialize()
        {
            store = new LevelDBStore("./TestDbCacheWithTrie/");
        }

        [TestMethod]
        public void TestFind()
        {
            var snapshot = store.GetSnapshot();
            var skey = new StorageKey
            {
                ScriptHash = UInt160.Zero,
                Key = new byte[] { 1 },
            };
            var sitem = new StorageItem
            {
                Value = new byte[] { 2 },
                IsConstant = false,
            };
            snapshot.Storages.Add(skey, sitem);
            snapshot.Commit();

            var result = snapshot.Storages.Find().ToArray();
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(new KeyValuePair<StorageKey, StorageItem>(skey, sitem)));
        }
    }
}