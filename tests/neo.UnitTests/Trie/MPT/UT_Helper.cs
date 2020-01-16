using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestConcat()
        {
            var a = new byte[]{0x01};
            var b = new byte[]{0x02};
            a = a.Concat(b);
            Assert.AreEqual(2, a.Length);
        }

        [TestMethod]
        public void TestAdd()
        {
            var a = "ab".HexToBytes();
            byte b = 0x0c;
            a = a.Add(b);
            Assert.AreEqual("ab0c", a.ToHexString());

            a = new byte[0];
            a = a.Add(b);
            Assert.AreEqual(1, a.Length);
            Assert.AreEqual("0c", a.ToHexString());
        }

        [TestMethod]
        public void TestSkip()
        {
            var s = "abcd01".HexToBytes();
            s = s.Skip(2);
            Assert.AreEqual("01", s.ToHexString());

            s = new byte[]{0x01};
            s = s.Skip(1);
            Assert.AreEqual(0, s.Length);
            s = s.Skip(2);
            Assert.AreEqual(0, s.Length);
        }
    }
}