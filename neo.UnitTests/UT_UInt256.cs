using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_UInt256
    {
        [TestMethod]
        public void TestGernerator1()
        {
            UInt256 uInt256 = new UInt256();
            Assert.IsNotNull(uInt256);
        }

        [TestMethod]
        public void TestGernerator2()
        {
            UInt256 uInt256 = new UInt256(new byte[32]);
            Assert.IsNotNull(uInt256);
        }

        [TestMethod]
        public void TestCompareTo()
        {
            byte[] temp = new byte[32];
            temp[31] = 0x01;
            UInt256 result = new UInt256(temp);
            Assert.AreEqual(0, UInt256.Zero.CompareTo(UInt256.Zero));
            Assert.AreEqual(-1, UInt256.Zero.CompareTo(result));
            Assert.AreEqual(1, result.CompareTo(UInt256.Zero));
        }

        [TestMethod]
        public void TestEquals()
        {
            byte[] temp = new byte[32];
            temp[31] = 0x01;
            UInt256 result = new UInt256(temp);
            Assert.AreEqual(true, UInt256.Zero.Equals(UInt256.Zero));
            Assert.AreEqual(false, UInt256.Zero.Equals(result));
            Assert.AreEqual(false, result.Equals(null));
        }

        [TestMethod]
        public void TestParse()
        {
            Action action = () => UInt256.Parse(null);
            action.Should().Throw<ArgumentNullException>();
            UInt256 result = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000");
            Assert.AreEqual(UInt256.Zero, result);
            Action action1 = () => UInt256.Parse("000000000000000000000000000000000000000000000000000000000000000");
            action1.Should().Throw<FormatException>();
            UInt256 result1 = UInt256.Parse("0000000000000000000000000000000000000000000000000000000000000000");
            Assert.AreEqual(UInt256.Zero, result1);
        }

        [TestMethod]
        public void TestTryParse()
        {
            UInt256 temp = new UInt256();
            Assert.AreEqual(false, UInt256.TryParse(null, out temp));
            Assert.AreEqual(true, UInt256.TryParse("0x0000000000000000000000000000000000000000000000000000000000000000", out temp));
            Assert.AreEqual(UInt256.Zero, temp);
            Assert.AreEqual(false, UInt256.TryParse("000000000000000000000000000000000000000000000000000000000000000", out temp));
            Assert.AreEqual(false, UInt256.TryParse("0xKK00000000000000000000000000000000000000000000000000000000000000", out temp));
        }

        [TestMethod]
        public void TestOperatorLarger()
        {
            Assert.AreEqual(false, UInt256.Zero > UInt256.Zero);
        }

        [TestMethod]
        public void TestOperatorLargerAndEqual()
        {
            Assert.AreEqual(true, UInt256.Zero >= UInt256.Zero);
        }

        [TestMethod]
        public void TestOperatorSmaller()
        {
            Assert.AreEqual(false, UInt256.Zero < UInt256.Zero);
        }

        [TestMethod]
        public void TestOperatorSmallerAndEqual()
        {
            Assert.AreEqual(true, UInt256.Zero <= UInt256.Zero);
        }
    }
}
