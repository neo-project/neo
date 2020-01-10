using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_MPTTrie
    {
        private static Node root;

         [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var r = new ExtensionNode();
            var b = new BranchNode();
            var l1 = new LeafNode();
            var l2 = new LeafNode();
            var v1 = new ValueNode("abcd".HexToBytes());
            var v2 = new ValueNode("2222".HexToBytes());
            r.Key = "0a0c".HexToBytes();
            l1.Key = new byte[]{0x01};
            l2.Key = new byte[]{0x09};
            r.Next = b;
            b.Children[0] = l1;
            l1.Value = v1;
            b.Children[9] = l2;
            l2.Value = v2;
            root = r; 
        }

        [TestMethod]
        public void TestTryGet()
        {
            var mpt = new MPTTrie(null, root);
            var result = mpt.TryGet("0a0c0001".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            Assert.AreEqual("abcd", value.ToHexString());

            result = mpt.TryGet("0a0c0909".HexToBytes(), out value);
            Assert.IsTrue(result);
            Assert.AreEqual("2222", value.ToHexString());

            result = mpt.TryGet("0a0b0909".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("0a0c0309".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("0a0c0002".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("0a0c090901".HexToBytes(), out value);
            Assert.IsFalse(result);
        }
    }
}