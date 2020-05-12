using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.MPT;

namespace Neo.UnitTests.Cryptography.MPT
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestToNibbles()
        {
            var a = "1234abcd".HexToBytes();
            var n = a.ToNibbles();
            Assert.AreEqual("010203040a0b0c0d", n.ToHexString());

            a = null;
            Assert.AreEqual(0, a.ToNibbles().Length);
        }
    }
}
