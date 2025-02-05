// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MerkleTreeNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_MerkleTreeNode
    {
        private readonly MerkleTreeNode node = new MerkleTreeNode();

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

            Assert.AreEqual(hash, node.Hash);
            Assert.IsNull(node.Parent);
            Assert.IsNull(node.LeftChild);
            Assert.IsNull(node.RightChild);
        }

        [TestMethod]
        public void TestGetIsLeaf()
        {
            Assert.IsTrue(node.IsLeaf);

            MerkleTreeNode child = new MerkleTreeNode();
            node.LeftChild = child;
            Assert.IsFalse(node.IsLeaf);
        }

        [TestMethod]
        public void TestGetIsRoot()
        {
            Assert.IsTrue(node.IsRoot);
        }
    }
}
