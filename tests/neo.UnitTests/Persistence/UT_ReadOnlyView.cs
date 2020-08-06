using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using System;
using System.Linq;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_ReadOnlyView
    {
        [TestMethod]
        public void CommitException()
        {
            var r = new ReadOnlyView(new MemoryStore());
            Assert.ThrowsException<NotSupportedException>(() => r.Commit());
        }

        [TestMethod]
        public void Stores()
        {
            var r = new ReadOnlyView(new MemoryStore());

            Assert.AreEqual(uint.MaxValue, r.BlockHashIndex.Get().Index);
            Assert.AreEqual(UInt256.Zero, r.BlockHashIndex.Get().Hash);
            Assert.AreEqual(uint.MaxValue, r.HeaderHashIndex.Get().Index);
            Assert.AreEqual(UInt256.Zero, r.HeaderHashIndex.Get().Hash);
            Assert.AreEqual(0, r.ContractId.Get().NextId);
            Assert.AreEqual(0, r.Blocks.Find().Count());
            Assert.AreEqual(0, r.Transactions.Find().Count());
            Assert.AreEqual(0, r.Contracts.Find().Count());
            Assert.AreEqual(0, r.Storages.Find().Count());
            Assert.AreEqual(0, r.HeaderHashList.Find().Count());
        }
    }
}
