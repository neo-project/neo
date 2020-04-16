using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;

namespace Neo.UnitTests.Trie.MPTH
{
    [TestClass]
    public class UT_MPTHelper
    {
        [TestMethod]
        public void TestConcat()
        {
            var a = new byte[] { 0x01 };
            var b = new byte[] { 0x02 };
            a = a.Concat(b);
            Assert.AreEqual(2, a.Length);
        }

        [TestMethod]
        public void TestSkip()
        {
            var s = "abcd01".HexToBytes();
            s = s.Skip(2);
            Assert.AreEqual("01", s.ToHexString());

            s = new byte[] { 0x01 };
            s = s.Skip(1);
            Assert.AreEqual(0, s.Length);
            s = s.Skip(2);
            Assert.AreEqual(0, s.Length);
        }

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
        }

        [TestMethod]
        public void TestToNibbles()
        {
            var a = "1234abcd".HexToBytes();
            var n = a.ToNibbles();
            Assert.AreEqual("010203040a0b0c0d", n.ToHexString());
        }
    }
}
