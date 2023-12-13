using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_DataCache
    {
        [TestMethod]
        public void TestCachedFind_Between()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var storages = snapshot.CreateSnapshot();
            var cache = new ClonedCache(storages);

            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x01 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x03 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                cache.Find(new byte[5]).Select(u => u.Key.Key.Span[1]).ToArray(),
                new byte[] { 0x01, 0x02, 0x03 }
                );
        }

        [TestMethod]
        public void TestCachedFind_Last()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var storages = snapshot.CreateSnapshot();
            var cache = new ClonedCache(storages);

            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x01 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            CollectionAssert.AreEqual(cache.Find(new byte[5]).Select(u => u.Key.Key.Span[1]).ToArray(),
                new byte[] { 0x01, 0x02 }
                );
        }

        [TestMethod]
        public void TestCachedFind_Empty()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var storages = snapshot.CreateSnapshot();
            var cache = new ClonedCache(storages);

            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x02 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );
            cache.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x02 }, Id = 0 },
                new StorageItem() { Value = new byte[] { } }
                );

            CollectionAssert.AreEqual(
                cache.Find(new byte[5]).Select(u => u.Key.Key.Span[1]).ToArray(),
                new byte[] { 0x02 }
                );
        }
    }
}
