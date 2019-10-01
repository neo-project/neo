using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using Neo.Ledger;
using System.Linq;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_DataCache
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestCachedFind_Between()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            var storages = snapshot.Storages;
            var cache = new CloneCache<StorageKey, StorageItem>(storages);

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);

            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x01 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x01 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x03 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                cache.Find(new byte[21]).Select(u => u.Key.Key[1]).ToArray(),
                new byte[] { 0x01, 0x02, 0x03 }
                );

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);
        }

        [TestMethod]
        public void TestCachedFind_Last()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            var storages = snapshot.Storages;
            var cache = new CloneCache<StorageKey, StorageItem>(storages);

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);

            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x01 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x01 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                cache.Find(new byte[21]).Select(u => u.Key.Key[1]).ToArray(),
                new byte[] { 0x01, 0x02 }
                );

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);
        }

        [TestMethod]
        public void TestCachedFind_Empty()
        {
            var snapshot = TestBlockchain.GetStore().GetSnapshot();
            var storages = snapshot.Storages;
            var cache = new CloneCache<StorageKey, StorageItem>(storages);

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);

            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, ScriptHash = UInt160.Zero },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                cache.Find(new byte[21]).Select(u => u.Key.Key[1]).ToArray(),
                new byte[] { 0x02 }
                );

            storages.DeleteWhere((k, v) => k.ScriptHash == UInt160.Zero);
        }
    }
}
