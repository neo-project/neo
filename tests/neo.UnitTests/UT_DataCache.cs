using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
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
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var storages = snapshot.CreateSnapshot();
            var cache = new ClonedCache(storages);

            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x01 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x03 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                cache.Find(new byte[5]).Select(u => u.Key.Key[1]).ToArray(),
                new byte[] { 0x01, 0x02, 0x03 }
                );
        }

        [TestMethod]
        public void TestCachedFind_Last()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var storages = snapshot.CreateSnapshot();
            var cache = new ClonedCache(storages);

            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x01 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            CollectionAssert.AreEqual(cache.Find(new byte[5]).Select(u => u.Key.Key[1]).ToArray(),
                new byte[] { 0x01, 0x02 }
                );
        }

        [TestMethod]
        public void TestCachedFind_Empty()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var storages = snapshot.CreateSnapshot();
            var cache = new ClonedCache(storages);

            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, Id = 0 },
                new StorageItem() { IsConstant = false, Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                cache.Find(new byte[5]).Select(u => u.Key.Key[1]).ToArray(),
                new byte[] { 0x02 }
                );
        }
    }
}
