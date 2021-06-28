using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Murmur128
    {
        [TestMethod]
        public void TestGetHashSize()
        {
            Murmur128 murmur128 = new Murmur128(1);
            murmur128.HashSize.Should().Be(128);
        }

        [TestMethod]
        public void TestHashCore()
        {
            byte[] array = Encoding.ASCII.GetBytes("hello");
            array.Murmur128(123u).ToHexString().ToString().Should().Be("0bc59d0ad25fde2982ed65af61227a0e");

            array = Encoding.ASCII.GetBytes("world");
            array.Murmur128(123u).ToHexString().ToString().Should().Be("3d3810fed480472bd214a14023bb407f");

            array = Encoding.ASCII.GetBytes("hello world");
            array.Murmur128(123u).ToHexString().ToString().Should().Be("e0a0632d4f51302c55e3b3e48d28795d");
        }
    }
}
