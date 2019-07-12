using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Base58
    {
        byte[] decoded = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        string encoded = "1kA3B2yGe2z4";

        [TestMethod]
        public void TestEncode()
        {
            Base58.Encode(decoded).Should().Be(encoded);
        }

        [TestMethod]
        public void TestDecode()
        {
            Base58.Decode(encoded).Should().BeEquivalentTo(decoded);
        }
    }
}