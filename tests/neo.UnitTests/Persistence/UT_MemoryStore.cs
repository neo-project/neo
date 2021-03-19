using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using System;
using System.Linq;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_MemoryStore
    {
        [TestMethod]
        public void StoreTest()
        {
            using var store = new MemoryStore();

            store.Delete(new byte[] { 1 });
            Assert.AreEqual(null, store.TryGet(new byte[] { 1 }));
            store.Put(new byte[] { 1 }, new byte[] { 1, 2, 3 });
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, store.TryGet(new byte[] { 1 }));

            store.Put(new byte[] { 2 }, new byte[] { 4, 5, 6 });
            CollectionAssert.AreEqual(new byte[] { 1 }, store.Seek(Array.Empty<byte>(), SeekDirection.Forward).Select(u => u.Key).First());
            CollectionAssert.AreEqual(new byte[] { 2 }, store.Seek(Array.Empty<byte>(), SeekDirection.Backward).Select(u => u.Key).First());
            CollectionAssert.AreEqual(new byte[] { 1 }, store.Seek(new byte[] { 1 }, SeekDirection.Backward).Select(u => u.Key).First());

            store.Delete(new byte[] { 1 });
        }
    }
}
