using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;

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
    }
}
