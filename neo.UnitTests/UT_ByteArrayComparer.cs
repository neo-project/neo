using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_ByteArrayComparer
    {
        [TestMethod]
        public void EqualsEmpty()
        {
            var comparer = new ByteArrayComparer();
            Assert.IsFalse(comparer.Equals(null, new byte[] { }));
            Assert.IsFalse(comparer.Equals(new byte[] { }, null));
            Assert.IsFalse(comparer.Equals(null, new byte[0]));
            Assert.IsFalse(comparer.Equals(new byte[0], null));

            Assert.IsTrue(comparer.Equals(new byte[] { }, new byte[] { }));
            Assert.IsTrue(comparer.Equals(new byte[0], new byte[] { }));
            Assert.IsTrue(comparer.Equals(new byte[] { }, new byte[0]));
            Assert.IsTrue(comparer.Equals(new byte[0], new byte[0]));
        }

        [TestMethod]
        public void EqualsDistinctSizes()
        {
            var comparer = new ByteArrayComparer();
            Assert.IsFalse(comparer.Equals(null, new byte[] {0}));
            Assert.IsFalse(comparer.Equals(new byte[] {0}, null));
            Assert.IsFalse(comparer.Equals(new byte[] {0, 1}, new byte[] {0}));
            Assert.IsFalse(comparer.Equals(new byte[] {0, 1}, new byte[] {0, 1, 2}));
        }

        [TestMethod]
        public void EqualsOneElement()
        {
            var comparer = new ByteArrayComparer();
            Assert.IsFalse(comparer.Equals(new byte[] {1}, new byte[] {0}));
            Assert.IsFalse(comparer.Equals(new byte[] {0}, new byte[] {1}));

            Assert.IsTrue(comparer.Equals(new byte[] {0}, new byte[] {0}));
            Assert.IsTrue(comparer.Equals(new byte[] {1}, new byte[] {1}));
        }

        [TestMethod]
        public void EqualsManyElements()
        {
            var comparer = new ByteArrayComparer();
            Assert.IsFalse(comparer.Equals(new byte[] {1, 0, 1}, new byte[] {1, 0, 2}));
            Assert.IsFalse(comparer.Equals(new byte[] {1, 1, 1, 1, 1, 11, 1, 1, 2, 11, 1},
                new byte[] {1, 1, 1, 1, 1, 11, 1, 1, 1, 11, 1}));

            Assert.IsTrue(comparer.Equals(new byte[] {0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8},
                new byte[] {0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8}));
            Assert.IsTrue(comparer.Equals(new byte[] {4, 45, 8, 89, 6, 0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8},
                new byte[] {4, 45, 8, 89, 6, 0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8}));
        }

        [TestMethod]
        public void GetHashCodeEmpty()
        {
            var comparer = new ByteArrayComparer();
            Assert.AreEqual(comparer.GetHashCode(new byte[] { }), comparer.GetHashCode(new byte[] { }));
            Assert.AreEqual(comparer.GetHashCode(new byte[0]), comparer.GetHashCode(new byte[] { }));
            Assert.AreEqual(comparer.GetHashCode(new byte[] { }), comparer.GetHashCode(new byte[0]));
            Assert.AreEqual(comparer.GetHashCode(new byte[0]), comparer.GetHashCode(new byte[0]));
        }

        [TestMethod]
        public void GetHashCodeDistinctSizes()
        {
            var comparer = new ByteArrayComparer();
            Assert.AreNotEqual(comparer.GetHashCode(new byte[] {0, 1}), comparer.GetHashCode(new byte[] {0}));
            Assert.AreNotEqual(comparer.GetHashCode(new byte[] {0, 1}), comparer.GetHashCode(new byte[] {0, 1, 2}));
        }

        [TestMethod]
        public void GetHashCodeOneElement()
        {
            var comparer = new ByteArrayComparer();
            Assert.AreNotEqual(comparer.GetHashCode(new byte[] {1}), comparer.GetHashCode(new byte[] {0}));
            Assert.AreNotEqual(comparer.GetHashCode(new byte[] {0}), comparer.GetHashCode(new byte[] {1}));

            Assert.AreEqual(comparer.GetHashCode(new byte[] {0}), comparer.GetHashCode(new byte[] {0}));
            Assert.AreEqual(comparer.GetHashCode(new byte[] {1}), comparer.GetHashCode(new byte[] {1}));
        }

        [TestMethod]
        public void GetHashCodeManyElements()
        {
            var comparer = new ByteArrayComparer();
            Assert.AreNotEqual(comparer.GetHashCode(new byte[] {1, 0, 1}), comparer.GetHashCode(new byte[] {1, 0, 2}));
            Assert.AreNotEqual(comparer.GetHashCode(new byte[] {1, 1, 1, 1, 1, 11, 1, 1, 2, 11, 1}),
                comparer.GetHashCode(new byte[] {1, 1, 1, 1, 1, 11, 1, 1, 1, 11, 1}));

            Assert.AreEqual(comparer.GetHashCode(new byte[] {0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8}),
                comparer.GetHashCode(new byte[] {0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8}));
            Assert.AreEqual(comparer.GetHashCode(new byte[] {4, 45, 8, 89, 6, 0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8}),
                comparer.GetHashCode(new byte[] {4, 45, 8, 89, 6, 0, 1, 2, 12, 3, 1, 1, 2, 5, 44, 8}));
        }

    }
}