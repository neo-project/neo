using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ByteArrayEqualityComparer
    {
        [TestMethod]
        public void TestEqual()
        {
            var a = new byte[] { 1, 2, 3, 4, 1, 2, 3, 4, 5 };
            var b = new byte[] { 1, 2, 3, 4, 1, 2, 3, 4, 5 };
            var check = ByteArrayEqualityComparer.Default;

            Assert.IsTrue(check.Equals(a, a));
            Assert.IsTrue(check.Equals(a, b));
            Assert.IsFalse(check.Equals(null, b));
            Assert.IsFalse(check.Equals(a, null));
            Assert.IsTrue(check.Equals(null, null));

            Assert.IsFalse(check.Equals(a, new byte[] { 1, 2, 3 }));
            Assert.IsTrue(check.Equals(Array.Empty<byte>(), Array.Empty<byte>()));

            b[8]++;
            Assert.IsFalse(check.Equals(a, b));
            b[8]--;
            b[0]--;
            Assert.IsFalse(check.Equals(a, b));
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            var a = new byte[] { 1, 2, 3, 4, 1, 2, 3, 4, 5 };
            var b = new byte[] { 1, 2, 3, 4, 1, 2, 3, 4, 5 };
            var check = ByteArrayEqualityComparer.Default;

            Assert.AreEqual(check.GetHashCode(a), check.GetHashCode(b));
            Assert.AreNotEqual(check.GetHashCode(a), check.GetHashCode(b.Take(8).ToArray()));
        }
    }
}
