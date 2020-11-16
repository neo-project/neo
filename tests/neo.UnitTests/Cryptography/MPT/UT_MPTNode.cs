using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.MPT;
using System;
using System.Text;

namespace Neo.UnitTests.Cryptography.MPT
{
    [TestClass]
    public class UT_MPTNode
    {
        [TestMethod]
        public void TestDecode()
        {
            var n = new LeafNode
            {
                Value = Encoding.ASCII.GetBytes("hello")
            };
            var code = n.EncodeWithReference();
            var m = MPTNode.Decode(code);
            Assert.IsInstanceOfType(m, n.GetType());
        }

        [TestMethod]
        public void TestHashNode()
        {
            var hn = new HashNode(null);
            var data = hn.Encode();
            Assert.AreEqual("0200", data.ToHexString());
        }

        [TestMethod]
        public void TestHashNodeDecode()
        {
            var data = new byte[] { 2, 0, 0 };
            var h = MPTNode.Decode(data);
            Assert.AreEqual(null, h.Hash);
            data = new byte[] { 2, 1, 0, 0 };
            Assert.ThrowsException<FormatException>(() => MPTNode.Decode(data));
        }

        [TestMethod]
        public void TestDecodeException()
        {
            var data = new byte[] { 4, 0, 0 };
            Assert.ThrowsException<InvalidOperationException>(() => MPTNode.Decode(data));
        }
    }
}
