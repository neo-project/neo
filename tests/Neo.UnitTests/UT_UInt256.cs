#pragma warning disable CS1718

using System;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_UInt256
    {
        [TestMethod]
        public void TestFail()
        {
            Assert.ThrowsException<FormatException>(() => new UInt256(new byte[UInt256.Length + 1]));
        }

        [TestMethod]
        public void TestGernerator1()
        {
            UInt256 uInt256 = new();
            Assert.IsNotNull(uInt256);
        }

        [TestMethod]
        public void TestGernerator2()
        {
            UInt256 uInt256 = new(new byte[32]);
            Assert.IsNotNull(uInt256);
        }

        [TestMethod]
        public void TestCompareTo()
        {
            byte[] temp = new byte[32];
            temp[31] = 0x01;
            UInt256 result = new(temp);
            Assert.AreEqual(0, UInt256.Zero.CompareTo(UInt256.Zero));
            Assert.AreEqual(-1, UInt256.Zero.CompareTo(result));
            Assert.AreEqual(1, result.CompareTo(UInt256.Zero));
        }

        [TestMethod]
        public void TestDeserialize()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.Write(new byte[20]);
            UInt256 uInt256 = new();
            Assert.ThrowsException<FormatException>(() =>
            {
                MemoryReader reader = new(stream.ToArray());
                ((ISerializable)uInt256).Deserialize(ref reader);
            });
        }

        [TestMethod]
        public void TestEquals()
        {
            byte[] temp = new byte[32];
            temp[31] = 0x01;
            UInt256 result = new(temp);
            Assert.AreEqual(true, UInt256.Zero.Equals(UInt256.Zero));
            Assert.AreEqual(false, UInt256.Zero.Equals(result));
            Assert.AreEqual(false, result.Equals(null));
        }

        [TestMethod]
        public void TestEquals1()
        {
            UInt256 temp1 = new();
            UInt256 temp2 = new();
            UInt160 temp3 = new();
            Assert.AreEqual(false, temp1.Equals(null));
            Assert.AreEqual(true, temp1.Equals(temp1));
            Assert.AreEqual(true, temp1.Equals(temp2));
            Assert.AreEqual(false, temp1.Equals(temp3));
        }

        [TestMethod]
        public void TestEquals2()
        {
            UInt256 temp1 = new();
            object temp2 = null;
            object temp3 = new();
            Assert.AreEqual(false, temp1.Equals(temp2));
            Assert.AreEqual(false, temp1.Equals(temp3));
        }

        [TestMethod]
        public void TestParse()
        {
            Action action = () => UInt256.Parse(null);
            action.Should().Throw<FormatException>();
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
            Assert.AreEqual(false, UInt256.TryParse(null, out _));
            Assert.AreEqual(true, UInt256.TryParse("0x0000000000000000000000000000000000000000000000000000000000000000", out var temp));
            Assert.AreEqual(UInt256.Zero, temp);
            Assert.AreEqual(true, UInt256.TryParse("0x1230000000000000000000000000000000000000000000000000000000000000", out temp));
            Assert.AreEqual("0x1230000000000000000000000000000000000000000000000000000000000000", temp.ToString());
            Assert.AreEqual(false, UInt256.TryParse("000000000000000000000000000000000000000000000000000000000000000", out _));
            Assert.AreEqual(false, UInt256.TryParse("0xKK00000000000000000000000000000000000000000000000000000000000000", out _));
        }

        [TestMethod]
        public void TestOperatorEqual()
        {
            Assert.IsFalse(new UInt256() == null);
            Assert.IsFalse(null == new UInt256());
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
