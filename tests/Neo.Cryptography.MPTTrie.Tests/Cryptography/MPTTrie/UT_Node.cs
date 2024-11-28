// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Node.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.Cryptography.MPTTrie.Tests
{

    [TestClass]
    public class UT_Node
    {
        private byte[] NodeToArrayAsChild(Node n)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Neo.Utility.StrictUTF8, true);

            n.SerializeAsChild(writer);
            writer.Flush();
            return ms.ToArray();
        }

        [TestMethod]
        public void TestHashSerialize()
        {
            var n = Node.NewHash(UInt256.Zero);
            var expect = "030000000000000000000000000000000000000000000000000000000000000000";
            Assert.AreEqual(expect, n.ToArray().ToHexString());
            Assert.AreEqual(expect, NodeToArrayAsChild(n).ToHexString());
        }

        [TestMethod]
        public void TestEmptySerialize()
        {
            var n = new Node();
            var expect = "04";
            Assert.AreEqual(expect, n.ToArray().ToHexString());
            Assert.AreEqual(expect, NodeToArrayAsChild(n).ToHexString());
        }

        [TestMethod]
        public void TestLeafSerialize()
        {
            var n = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var expect = "02" + "04" + Encoding.ASCII.GetBytes("leaf").ToHexString();
            Assert.AreEqual(expect, n.ToArrayWithoutReference().ToHexString());
            expect += "01";
            Assert.AreEqual(expect, n.ToArray().ToHexString());
            Assert.AreEqual(7, n.Size);
        }

        [TestMethod]
        public void TestLeafSerializeAsChild()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var expect = "03" + Crypto.Hash256(new byte[] { 0x02, 0x04 }.Concat(Encoding.ASCII.GetBytes("leaf")).ToArray()).ToHexString();
            Assert.AreEqual(expect, NodeToArrayAsChild(l).ToHexString());
        }

        [TestMethod]
        public void TestExtensionSerialize()
        {
            var e = Node.NewExtension("010a".HexToBytes(), new Node());
            var expect = "01" + "02" + "010a" + "04";
            Assert.AreEqual(expect, e.ToArrayWithoutReference().ToHexString());
            expect += "01";
            Assert.AreEqual(expect, e.ToArray().ToHexString());
            Assert.AreEqual(6, e.Size);
        }

        [TestMethod]
        public void TestExtensionSerializeAsChild()
        {
            var e = Node.NewExtension("010a".HexToBytes(), new Node());
            var expect = "03" + Crypto.Hash256(new byte[] { 0x01, 0x02, 0x01, 0x0a, 0x04
             }).ToHexString();
            Assert.AreEqual(expect, NodeToArrayAsChild(e).ToHexString());
        }

        [TestMethod]
        public void TestBranchSerialize()
        {
            var n = Node.NewBranch();
            n.Children[1] = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf1"));
            n.Children[10] = Node.NewLeaf(Encoding.ASCII.GetBytes("leafa"));
            var expect = "00";
            for (int i = 0; i < Node.BranchChildCount; i++)
            {
                if (i == 1)
                    expect += "03" + Crypto.Hash256(new byte[] { 0x02, 0x05 }.Concat(Encoding.ASCII.GetBytes("leaf1")).ToArray()).ToHexString();
                else if (i == 10)
                    expect += "03" + Crypto.Hash256(new byte[] { 0x02, 0x05 }.Concat(Encoding.ASCII.GetBytes("leafa")).ToArray()).ToHexString();
                else
                    expect += "04";
            }
            expect += "01";
            Assert.AreEqual(expect, n.ToArray().ToHexString());
            Assert.AreEqual(83, n.Size);
        }

        [TestMethod]
        public void TestBranchSerializeAsChild()
        {
            var n = Node.NewBranch();
            var data = new List<byte>();
            data.Add(0x00);
            for (int i = 0; i < Node.BranchChildCount; i++)
            {
                data.Add(0x04);
            }
            var expect = "03" + Crypto.Hash256(data.ToArray()).ToHexString();
            Assert.AreEqual(expect, NodeToArrayAsChild(n).ToHexString());
        }

        [TestMethod]
        public void TestCloneBranch()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var n = Node.NewBranch();
            var n1 = n.Clone();
            n1.Children[0] = l;
            Assert.IsTrue(n.Children[0].IsEmpty);
        }

        [TestMethod]
        public void TestCloneExtension()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var n = Node.NewExtension(new byte[] { 0x01 }, new Node());
            var n1 = n.Clone();
            n1.Next = l;
            Assert.IsTrue(n.Next.IsEmpty);
        }

        [TestMethod]
        public void TestCloneLeaf()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var n = l.Clone();
            n.Value = Encoding.ASCII.GetBytes("value");
            Assert.AreEqual("leaf", Encoding.ASCII.GetString(l.Value.Span));
        }

        [TestMethod]
        public void TestNewExtensionException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => Node.NewExtension(null, new Node()));
            Assert.ThrowsException<ArgumentNullException>(() => Node.NewExtension(new byte[] { 0x01 }, null));
            Assert.ThrowsException<InvalidOperationException>(() => Node.NewExtension(Array.Empty<byte>(), new Node()));
        }

        [TestMethod]
        public void TestNewHashException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => Node.NewHash(null));
        }

        [TestMethod]
        public void TestNewLeafException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => Node.NewLeaf(null));
        }

        [TestMethod]
        public void TestSize()
        {
            var n = new Node();
            Assert.AreEqual(1, n.Size);
            n = Node.NewBranch();
            Assert.AreEqual(19, n.Size);
            n = Node.NewExtension(new byte[] { 0x00 }, new Node());
            Assert.AreEqual(5, n.Size);
            n = Node.NewLeaf(new byte[] { 0x00 });
            Assert.AreEqual(4, n.Size);
            n = Node.NewHash(UInt256.Zero);
            Assert.AreEqual(33, n.Size);
        }

        [TestMethod]
        public void TestFromReplica()
        {
            var l = Node.NewLeaf(new byte[] { 0x00 });
            var n = Node.NewBranch();
            n.Children[1] = l;
            var r = new Node();
            r.FromReplica(n);
            Assert.AreEqual(n.Hash, r.Hash);
            Assert.AreEqual(NodeType.HashNode, r.Children[1].Type);
            Assert.AreEqual(l.Hash, r.Children[1].Hash);
        }

        [TestMethod]
        public void TestEmptyLeaf()
        {
            var leaf = Node.NewLeaf(Array.Empty<byte>());
            var data = leaf.ToArray();
            Assert.AreEqual(3, data.Length);
            var l = data.AsSerializable<Node>();
            Assert.AreEqual(NodeType.LeafNode, l.Type);
            Assert.AreEqual(0, l.Value.Length);
        }
    }
}
