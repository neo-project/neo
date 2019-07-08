using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Scrypt
    {
        [TestMethod]
        public void DeriveKeyTest()
        {
            int N = 16384, r = 8, p = 8;

            var derivedkey = SCrypt.DeriveKey(new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x04, 0x05, 0x06 }, N, r, p, 64).ToHexString();
            Assert.AreEqual("2bb9c7bb9c392f0dd37821b76e42b01944902520f48d00946a51e72c960fba0a3c62a87d835c9df10a8ad66a04cdf02fbb10b9d7396c20959f28d6cb3ddfdffb", derivedkey);
        }
    }
}
