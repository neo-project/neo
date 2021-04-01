using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System.Linq;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_FifoSet
    {
        [TestMethod]
        public void FifoSetTest()
        {
            var a = UInt256.Zero;
            var b = new UInt256();
            var c = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            });

            var set = new FIFOSet<UInt256>(3);

            Assert.IsTrue(set.Add(a));
            Assert.IsFalse(set.Add(a));
            Assert.IsFalse(set.Add(b));
            Assert.IsTrue(set.Add(c));

            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a, c });

            set = new FIFOSet<UInt256>(10)
            {
                a,
                c
            };
            var bb = set.ToArray();
            set.ExceptWith(new UInt256[] { a });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { c });

            set = new FIFOSet<UInt256>(10)
            {
                a,
                c
            };
            set.ExceptWith(new UInt256[] { c });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a });
        }
    }
}
