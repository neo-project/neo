using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC;
using System;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestToBigInteger()
        {
            decimal amount = 1.23456789m;
            uint decimals = 9;
            var result = amount.ToBigInteger(decimals);
            Assert.AreEqual(1234567890, result);

            amount = 1.23456789m;
            decimals = 18;
            result = amount.ToBigInteger(decimals);
            Assert.AreEqual(BigInteger.Parse("1234567890000000000"), result);

            amount = 1.23456789m;
            decimals = 4;
            Assert.ThrowsException<OverflowException>(() => result = amount.ToBigInteger(decimals));
        }
    }
}
