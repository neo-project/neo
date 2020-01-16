using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;
using System.Text;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_MPTNode
    {
        [TestMethod]
        public void TestDecode()
        {
            var n = new ValueNode();
            n.Value = Encoding.ASCII.GetBytes("hello");
            var code = n.Encode();
            var m = MPTNode.Decode(code);
            Assert.IsInstanceOfType(m, n.GetType());
        }

        [TestMethod]
        public void TestFlag()
        {
            var n = new ShortNode();
            Assert.IsTrue(n.Flag.Dirty);
        }
    }
}