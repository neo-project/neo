using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtUtility
    {
        [TestMethod]
        public void SqrtTest()
        {
            Assert.ThrowsException<InvalidOperationException>(() => BigInteger.MinusOne.Sqrt());

            Assert.AreEqual(BigInteger.Zero, BigInteger.Zero.Sqrt());
            Assert.AreEqual(new BigInteger(1), new BigInteger(1).Sqrt());
            Assert.AreEqual(new BigInteger(1), new BigInteger(2).Sqrt());
            Assert.AreEqual(new BigInteger(1), new BigInteger(3).Sqrt());
            Assert.AreEqual(new BigInteger(2), new BigInteger(4).Sqrt());
            Assert.AreEqual(new BigInteger(9), new BigInteger(81).Sqrt());
        }

        [TestMethod]
        public void ModInverseTest()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => BigInteger.One.ModInverse(BigInteger.Zero));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => BigInteger.One.ModInverse(BigInteger.One));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => BigInteger.Zero.ModInverse(BigInteger.Zero));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => BigInteger.Zero.ModInverse(BigInteger.One));
            Assert.ThrowsException<InvalidOperationException>(() => new BigInteger(ushort.MaxValue).ModInverse(byte.MaxValue));

            Assert.AreEqual(new BigInteger(52), new BigInteger(19).ModInverse(141));
        }
    }
}
