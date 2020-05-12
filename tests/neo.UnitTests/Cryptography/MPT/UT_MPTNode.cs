using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.MPT;
using System.Text;

namespace Neo.UnitTests.Cryptography.MPT
{
    [TestClass]
    public class UT_MPTNode
    {
        [TestMethod]
        public void TestDecode()
        {
            var n = new LeafNode();
            n.Value = Encoding.ASCII.GetBytes("hello");
            var code = n.Encode();
            var m = MPTNode.Decode(code);
            Assert.IsInstanceOfType(m, n.GetType());
        }

        [TestMethod]
        public void TestFlag()
        {
            var n = new ExtensionNode();
            Assert.IsTrue(n.Dirty);
        }

        [TestMethod]
        public void TestHashNode()
        {
            var hn = new HashNode(null);
            var data = hn.Encode();
            Assert.AreEqual("0200", data.ToHexString());
        }
    }
}
