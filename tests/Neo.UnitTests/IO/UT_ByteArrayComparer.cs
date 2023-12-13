using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_ByteArrayComparer
    {
        [TestMethod]
        public void TestCompare()
        {
            ByteArrayComparer comparer = ByteArrayComparer.Default;
            byte[] x = new byte[0], y = new byte[0];
            comparer.Compare(x, y).Should().Be(0);

            x = new byte[] { 1 };
            comparer.Compare(x, y).Should().Be(1);
            y = x;
            comparer.Compare(x, y).Should().Be(0);

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            comparer.Compare(x, y).Should().Be(-1);

            comparer = ByteArrayComparer.Reverse;
            x = new byte[] { 3 };
            comparer.Compare(x, y).Should().Be(-1);
            y = x;
            comparer.Compare(x, y).Should().Be(0);

            x = new byte[] { 1 };
            y = new byte[] { 2 };
            comparer.Compare(x, y).Should().Be(1);
        }
    }
}
