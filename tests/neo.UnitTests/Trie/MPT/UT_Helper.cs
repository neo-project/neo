using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestAdd()
        {
            var a = "ab".HexToBytes();
            byte b = 0x0c;
            a.Add(b);
            Assert.AreEqual("ab", a.ToHexString());
        }

        [TestMethod]
        public void TestSkip()
        {
            var s = "abcd01".HexToBytes();
            s = s.Skip(2);
            Assert.AreEqual("01", s.ToHexString());
        }
    }
}