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
        public void ReferenceEquals()
        {
            var r = new ReadOnlyView(new MemoryStore());
            Assert.IsTrue(object.ReferenceEquals(r.Blocks, r.Blocks));
            Assert.IsTrue(object.ReferenceEquals(r.Transactions, r.Transactions));
            Assert.IsTrue(object.ReferenceEquals(r.Contracts, r.Contracts));
            Assert.IsTrue(object.ReferenceEquals(r.Storages, r.Storages));
            Assert.IsTrue(object.ReferenceEquals(r.HeaderHashList, r.HeaderHashList));
            Assert.IsTrue(object.ReferenceEquals(r.BlockHashIndex, r.BlockHashIndex));
            Assert.IsTrue(object.ReferenceEquals(r.HeaderHashIndex, r.HeaderHashIndex));
            Assert.IsTrue(object.ReferenceEquals(r.ContractId, r.ContractId));
        }

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
