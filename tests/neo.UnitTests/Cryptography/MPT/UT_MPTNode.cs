using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.MPT;
using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.Cryptography.MPT
{

    [TestClass]
    public class UT_MPTNode
    {
        private byte[] NodeToArrayAsChild(MPTNode n)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Utility.StrictUTF8);

            n.SerializeAsChild(writer);
            writer.Flush();
            return ms.ToArray();
        }

        [TestMethod]
        public void TestHashSerialize()
        {
            var n = MPTNode.NewHash(UInt256.Zero);
            var expect = "030000000000000000000000000000000000000000000000000000000000000000";
            Assert.AreEqual(expect, n.ToArray().ToHexString());
            Assert.AreEqual(expect, NodeToArrayAsChild(n).ToHexString());
        }

        [TestMethod]
        public void TestEmptySerialize()
        {
            var n = new MPTNode();
            var expect = "04";
            Assert.AreEqual(expect, n.ToArray().ToHexString());
            Assert.AreEqual(expect, NodeToArrayAsChild(n).ToHexString());
        }

        [TestMethod]
        public void TestLeafSerialize()
        {
            var n = MPTNode.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var expect = "02" + "04" + Encoding.ASCII.GetBytes("leaf").ToHexString();
            Assert.AreEqual(expect, n.ToArrayWithoutReference().ToHexString());
            expect += "01";
            Assert.AreEqual(expect, n.ToArray().ToHexString());
            Assert.AreEqual(7, n.Size);
        }

        [TestMethod]
        public void TestLeafSerializeAsChild()
        {
            var l = MPTNode.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var expect = "03" + Crypto.Hash256(new byte[] { 0x02, 0x04 }.Concat(Encoding.ASCII.GetBytes("leaf")).ToArray()).ToHexString();
            Assert.AreEqual(expect, NodeToArrayAsChild(l).ToHexString());
        }

        [TestMethod]
        public void TestExtensionSerialize()
        {
            var e = MPTNode.NewExtension("010a".HexToBytes(), new MPTNode());
            var expect = "01" + "02" + "010a" + "04";
            Assert.AreEqual(expect, e.ToArrayWithoutReference().ToHexString());
            expect += "01";
            Assert.AreEqual(expect, e.ToArray().ToHexString());
            Assert.AreEqual(6, e.Size);
        }

        [TestMethod]
        public void TestExtensionSerializeAsChild()
        {
            var e = MPTNode.NewExtension("010a".HexToBytes(), new MPTNode());
            var expect = "03" + Crypto.Hash256(new byte[] { 0x01, 0x02, 0x01, 0x0a, 0x04
             }).ToHexString();
            Assert.AreEqual(expect, NodeToArrayAsChild(e).ToHexString());
        }

        [TestMethod]
        public void TestBranchSerialize()
        {
            var n = MPTNode.NewBranch();
            n.Children[1] = MPTNode.NewLeaf(Encoding.ASCII.GetBytes("leaf1"));
            n.Children[10] = MPTNode.NewLeaf(Encoding.ASCII.GetBytes("leafa"));
            var expect = "00";
            for (int i = 0; i < MPTNode.BranchChildCount; i++)
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
            var n = MPTNode.NewBranch();
            var data = new List<byte>();
            data.Add(0x00);
            for (int i = 0; i < MPTNode.BranchChildCount; i++)
            {
                data.Add(0x04);
            }
            var expect = "03" + Crypto.Hash256(data.ToArray()).ToHexString();
            Assert.AreEqual(expect, NodeToArrayAsChild(n).ToHexString());
        }

        [TestMethod]
        public void TestCloneBranch()
        {
            var l = MPTNode.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var n = MPTNode.NewBranch();
            var n1 = n.Clone();
            n1.Children[0] = l;
            Assert.IsTrue(n.Children[0].IsEmpty);
        }

        [TestMethod]
        public void TestCloneExtension()
        {
            var l = MPTNode.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var n = MPTNode.NewExtension(new byte[] { 0x01 }, new MPTNode());
            var n1 = n.Clone();
            n1.Next = l;
            Assert.IsTrue(n.Next.IsEmpty);
        }

        [TestMethod]
        public void TestCloneLeaf()
        {
            var l = MPTNode.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var n = l.Clone();
            n.Value = Encoding.ASCII.GetBytes("value");
            Assert.AreEqual("leaf", Encoding.ASCII.GetString(l.Value));
        }

        [TestMethod]
        public void TestNewExtensionException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => MPTNode.NewExtension(null, new MPTNode()));
            Assert.ThrowsException<ArgumentNullException>(() => MPTNode.NewExtension(new byte[] { 0x01 }, null));
            Assert.ThrowsException<InvalidOperationException>(() => MPTNode.NewExtension(Array.Empty<byte>(), new MPTNode()));
        }

        [TestMethod]
        public void TestNewHashException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => MPTNode.NewHash(null));
        }

        [TestMethod]
        public void TestNewLeafException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => MPTNode.NewLeaf(null));
            Assert.ThrowsException<InvalidOperationException>(() => MPTNode.NewLeaf(Array.Empty<byte>()));
        }

        [TestMethod]
        public void TestSize()
        {
            var n = new MPTNode();
            Assert.AreEqual(1, n.Size);
            n = MPTNode.NewBranch();
            Assert.AreEqual(19, n.Size);
            n = MPTNode.NewExtension(new byte[] { 0x00 }, new MPTNode());
            Assert.AreEqual(5, n.Size);
            n = MPTNode.NewLeaf(new byte[] { 0x00 });
            Assert.AreEqual(4, n.Size);
            n = MPTNode.NewHash(UInt256.Zero);
            Assert.AreEqual(33, n.Size);
        }

        [TestMethod]
        public void TestFromReplica()
        {
            var l = MPTNode.NewLeaf(new byte[] { 0x00 });
            var n = MPTNode.NewBranch();
            n.Children[1] = l;
            var r = new MPTNode();
            r.FromReplica(n);
            Assert.AreEqual(n.Hash, r.Hash);
            Assert.AreEqual(NodeType.HashNode, r.Children[1].Type);
            Assert.AreEqual(l.Hash, r.Children[1].Hash);
        }
    }
}
