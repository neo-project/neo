using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;
using Neo.IO;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_MPTNode
    {
        [TestMethod]
        public void TestNodeInit()
        {
            var b = new BranchNode();
            Assert.IsTrue(b.Dirty);
        }

        [TestMethod]
        public void TestSize()
        {
            var ln = new LeafNode(new byte[] { 1, 2, 8, 16, 32 });
            Assert.AreEqual(ln.ToArray().Length, ln.Size);

            var en = new ExtensionNode();
            en.Key = new byte[] { 0x0a, 0x01 };
            en.Next = ln;
            Assert.AreEqual(en.ToArray().Length, en.Size);

            var bn = new BranchNode();
            bn.Children[1] = ln;
            bn.Children[0x0a] = en;
            Assert.AreEqual(bn.ToArray().Length, bn.Size);
        }
    }
}
