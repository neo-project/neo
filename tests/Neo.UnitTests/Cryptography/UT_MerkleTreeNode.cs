// Copyright (C) 2015-2024 The Neo Project.
//
// UT_MerkleTreeNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System.Text;

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
