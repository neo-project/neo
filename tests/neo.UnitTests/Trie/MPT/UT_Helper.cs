using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestCommonPrefix()
        {
            var a = "1234abcd".HexToBytes();
            var b = "".HexToBytes();
            var prefix = a.CommonPrefix(b);
            Assert.IsTrue(prefix.Length == 0);

            b = "100000".HexToBytes();
            prefix = a.CommonPrefix(b);
            Assert.IsTrue(prefix.Length == 0);

            b = "1234".HexToBytes();
            prefix = a.CommonPrefix(b);
            Assert.AreEqual("1234", prefix.ToHexString());

            b = a;
            prefix = a.CommonPrefix(b);
            Assert.AreEqual("1234abcd", prefix.ToHexString());

            a = new byte[0];
            b = new byte[0];
            prefix = a.CommonPrefix(b);
            Assert.IsTrue(prefix.Length == 0);

            a = "1234abcd".HexToBytes();
            b = null;
            Assert.AreEqual(0, a.CommonPrefix(b).Length);

            a = null;
            b = null;
            Assert.AreEqual(0, a.CommonPrefix(b).Length);
        }

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
