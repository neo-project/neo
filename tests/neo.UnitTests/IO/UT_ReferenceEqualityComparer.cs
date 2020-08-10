using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ReferenceEqualityComparer
    {
        private class FakeEquals : IEquatable<FakeEquals>
        {
            public bool Equals([AllowNull] FakeEquals other)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 123;
            }
        }

        [TestMethod]
        public void TestEqual()
        {
            var a = new FakeEquals();
            var b = new FakeEquals();
            var check = ReferenceEqualityComparer.Default;

            Assert.IsFalse(check.Equals(a, b));
            Assert.AreNotEqual(check.GetHashCode(a), check.GetHashCode(b));
            Assert.AreNotEqual(123, check.GetHashCode(a));
            Assert.AreNotEqual(123, check.GetHashCode(b));
        }
    }
}
