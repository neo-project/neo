using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatriciaTools
    {
        [TestMethod]
        public void DistinctRoot()
        {
            Assert.AreEqual("faf01", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString(false, false));
            Assert.AreEqual("fa f0 1", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString(true, false));
            Assert.AreEqual("faf001", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString(false));
            Assert.AreEqual("fa f0 01", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString());
        }
    }
}