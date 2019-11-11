using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_BloomFilter
    {
        [TestMethod]
        public void TestAddCheck()
        {
            int m = 7;
            uint nTweak = 123456;
            byte[] elements = { 0, 1, 2, 3, 4 };
            BloomFilter filter = new BloomFilter(m, nTweak);
            filter.Add(elements);
            filter.Check(elements).Should().BeTrue();
            byte[] anotherElements = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            filter.Check(anotherElements).Should().BeFalse();
        }

        [TestMethod]
        public void TestBloomFIlterConstructorGetKMTweak()
        {
            int m = -7;
            uint nTweak = 123456;
            Action action = () => new BloomFilter(m, nTweak);
            action.Should().Throw<ArgumentOutOfRangeException>();

            m = 7;

            BloomFilter filter = new BloomFilter(m, nTweak);
            filter.M.Should().Be(m);
            filter.Tweak.Should().Be(nTweak);

            byte[] shorterElements = { 0, 1, 2, 3, 4 };
            filter = new BloomFilter(m, nTweak, shorterElements);
            filter.M.Should().Be(m);
            filter.Tweak.Should().Be(nTweak);

            byte[] longerElements = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            filter = new BloomFilter(m, nTweak, longerElements);
            filter.M.Should().Be(m);
            filter.Tweak.Should().Be(nTweak);
        }

        [TestMethod]
        public void TestGetBits()
        {
            int m = 7;
            uint nTweak = 123456;
            byte[] elements = { 0, 1, 2, 3, 4 };
            BloomFilter filter = new BloomFilter(m, nTweak);
            byte[] result = new byte[m];
            filter.GetBits(result);
            foreach (byte value in result)
                value.Should().Be(0);
        }
    }
}
