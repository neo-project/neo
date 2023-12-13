using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Murmur32
    {
        [TestMethod]
        public void TestGetHashSize()
        {
            Murmur32 murmur3 = new Murmur32(1);
            murmur3.HashSize.Should().Be(32);
        }

        [TestMethod]
        public void TestHashCore()
        {
            byte[] array = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1 };
            array.Murmur32(10u).Should().Be(378574820u);
        }
    }
}
