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

            var date = DateTime.UtcNow;
            var derivedkey = SCrypt.DeriveKey(new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x04, 0x05, 0x06 }, N, r, p, 64).ToHexString();
            var elapsed = (DateTime.UtcNow - date);

            Assert.AreEqual("f278f54e4a97e639c34d5f7e376d0ccd4a8c04bb8bad055d1f66a42c52c056a917ba0f4490c29209a410d9702c7e350309de8b102a617c8526a12bb16853c39a", derivedkey);
            Assert.IsTrue(elapsed.TotalSeconds > 3);
        }
    }
}
