using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Base58
    {
        byte[] decoded1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        string encoded1 = "1kA3B2yGe2z4";
        byte[] decoded2 = { 0, 0, 0, 0, 0 };
        string encoded2 = "1111";

        [TestMethod]
        public void TestEncode()
        {
            Base58.Encode(decoded1).Should().Be(encoded1);
        }

        [TestMethod]
        public void TestDecode()
        {
            Base58.Decode(encoded1).Should().BeEquivalentTo(decoded1);
            Base58.Decode(encoded2).Should().BeEquivalentTo(decoded2);
            Action action = () => Base58.Decode(encoded1 + "l").Should().BeEquivalentTo(decoded1);
            action.ShouldThrow<FormatException>();
        }
    }
}
