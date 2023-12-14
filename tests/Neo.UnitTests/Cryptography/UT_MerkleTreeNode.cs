using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_MerkleTreeNode
    {
        private MerkleTreeNode node = new MerkleTreeNode();

        [TestInitialize]
        public void TestSetup()
        {
            node.Hash = null;
            node.Parent = null;
            node.LeftChild = null;
            node.RightChild = null;
        }

        [TestMethod]
        public void TestConstructor()
        {
            byte[] byteArray = Encoding.ASCII.GetBytes("hello world");
            var hash = new UInt256(Crypto.Hash256(byteArray));
            node.Hash = hash;

            node.Hash.Should().Be(hash);
            node.Parent.Should().BeNull();
            node.LeftChild.Should().BeNull();
            node.RightChild.Should().BeNull();
        }

        [TestMethod]
        public void TestGetIsLeaf()
        {
            node.IsLeaf.Should().BeTrue();

            MerkleTreeNode child = new MerkleTreeNode();
            node.LeftChild = child;
            node.IsLeaf.Should().BeFalse();
        }

        [TestMethod]
        public void TestGetIsRoot()
        {
            node.IsRoot.Should().BeTrue();
        }
    }
}
