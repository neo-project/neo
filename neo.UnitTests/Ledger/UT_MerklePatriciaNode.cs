using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatriciaNode
    {
        [TestMethod]
        public void CloneLeaf()
        {
            var mptItem = MerklePatriciaNode.LeafNode();
            mptItem.Path = Encoding.UTF8.GetBytes("2");
            mptItem.Key = Encoding.UTF8.GetBytes("oi").Sha256();
            mptItem.Value = Encoding.UTF8.GetBytes("abc");

            var cloned = mptItem.Clone();
            Assert.IsTrue(cloned.IsLeaf);
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Path = Encoding.UTF8.GetBytes("23f");
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            mptItem.Value = Encoding.UTF8.GetBytes("abc1");
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
        }

        [TestMethod]
        public void CloneExtension()
        {
            var mptItem = MerklePatriciaNode.ExtensionNode();
            mptItem.Path = Encoding.UTF8.GetBytes("2");
            mptItem.Next = Encoding.UTF8.GetBytes("oi").Sha256();

            var cloned = mptItem.Clone();
            Assert.IsTrue(cloned.IsExtension);
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Next));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Path = Encoding.UTF8.GetBytes("23");
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            mptItem.Next = Encoding.UTF8.GetBytes("oi4").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Next));
        }

        [TestMethod]
        public void CloneBranch()
        {
            var mptItem = MerklePatriciaNode.BranchNode();
            mptItem.Key = Encoding.UTF8.GetBytes("oi").Sha256();
            mptItem.Value = Encoding.UTF8.GetBytes("abc");
            mptItem[0] = Encoding.UTF8.GetBytes("turma").Sha256();
            mptItem[7] = Encoding.UTF8.GetBytes("turma7").Sha256();
            mptItem[10] = Encoding.UTF8.GetBytes("turma10").Sha256();

            var cloned = mptItem.Clone();
            Assert.IsTrue(cloned.IsBranch);
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma").Sha256().SequenceEqual(cloned[0]));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma7").Sha256().SequenceEqual(cloned[7]));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma10").Sha256().SequenceEqual(cloned[10]));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Key = Encoding.UTF8.GetBytes("oi11").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            mptItem.Value = Encoding.UTF8.GetBytes("abc45");
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
            mptItem[0] = Encoding.UTF8.GetBytes("turma0").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma").Sha256().SequenceEqual(cloned[0]));
        }

        [TestMethod]
        public void FromReplicaLeaf()
        {
            var mptItem = MerklePatriciaNode.LeafNode();
            mptItem.Path = Encoding.UTF8.GetBytes("2");
            mptItem.Key = Encoding.UTF8.GetBytes("oi").Sha256();
            mptItem.Value = Encoding.UTF8.GetBytes("abc");

            var cloned = MerklePatriciaNode.BranchNode();
            cloned.FromReplica(mptItem);
            Assert.IsTrue(cloned.IsLeaf);
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Path = Encoding.UTF8.GetBytes("23f");
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            mptItem.Value = Encoding.UTF8.GetBytes("abc1");
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
        }

        [TestMethod]
        public void FromReplicaExtension()
        {
            var mptItem = MerklePatriciaNode.ExtensionNode();
            mptItem.Path = Encoding.UTF8.GetBytes("2");
            mptItem.Next = Encoding.UTF8.GetBytes("oi").Sha256();

            var cloned = MerklePatriciaNode.BranchNode();
            cloned.FromReplica(mptItem);
            Assert.IsTrue(cloned.IsExtension);
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Next));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Path = Encoding.UTF8.GetBytes("23");
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            mptItem.Next = Encoding.UTF8.GetBytes("oi4").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Next));
        }

        [TestMethod]
        public void FromReplicaBranch()
        {
            var mptItem = MerklePatriciaNode.BranchNode();
            mptItem.Key = Encoding.UTF8.GetBytes("oi").Sha256();
            mptItem.Value = Encoding.UTF8.GetBytes("abc");
            mptItem[0] = Encoding.UTF8.GetBytes("turma").Sha256();
            mptItem[7] = Encoding.UTF8.GetBytes("turma7").Sha256();
            mptItem[10] = Encoding.UTF8.GetBytes("turma10").Sha256();

            var cloned = MerklePatriciaNode.LeafNode();
            cloned.FromReplica(mptItem);
            Assert.IsTrue(cloned.IsBranch);
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma").Sha256().SequenceEqual(cloned[0]));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma7").Sha256().SequenceEqual(cloned[7]));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma10").Sha256().SequenceEqual(cloned[10]));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Key = Encoding.UTF8.GetBytes("oi11").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            mptItem.Value = Encoding.UTF8.GetBytes("abc45");
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
            mptItem[0] = Encoding.UTF8.GetBytes("turma0").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma").Sha256().SequenceEqual(cloned[0]));
        }

        [TestMethod]
        public void SerializeLeaf()
        {
            var mptItem = MerklePatriciaNode.LeafNode();
            mptItem.Path = Encoding.UTF8.GetBytes("2");
            mptItem.Key = Encoding.UTF8.GetBytes("oi").Sha256();
            mptItem.Value = Encoding.UTF8.GetBytes("abc");

            var cloned = MerklePatriciaNode.BranchNode();
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    mptItem.Serialize(bw);
                    using (var br = new BinaryReader(bw.BaseStream))
                    {
                        br.BaseStream.Position = 0;
                        cloned.Deserialize(br);
                    }
                }
            }

            Assert.IsTrue(cloned.IsLeaf);
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Path = Encoding.UTF8.GetBytes("23f");
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            mptItem.Value = Encoding.UTF8.GetBytes("abc1");
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
        }

        [TestMethod]
        public void SerializeExtension()
        {
            var mptItem = MerklePatriciaNode.ExtensionNode();
            mptItem.Path = Encoding.UTF8.GetBytes("2");
            mptItem.Next = Encoding.UTF8.GetBytes("oi").Sha256();

            var cloned = MerklePatriciaNode.BranchNode();
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    mptItem.Serialize(bw);
                    using (var br = new BinaryReader(bw.BaseStream))
                    {
                        br.BaseStream.Position = 0;
                        cloned.Deserialize(br);
                    }
                }
            }

            Assert.IsTrue(cloned.IsExtension);
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Next));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Path = Encoding.UTF8.GetBytes("23");
            Assert.IsTrue(Encoding.UTF8.GetBytes("2").SequenceEqual(cloned.Path));
            mptItem.Next = Encoding.UTF8.GetBytes("oi4").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Next));
        }

        [TestMethod]
        public void SerializeBranch()
        {
            var mptItem = MerklePatriciaNode.BranchNode();
            mptItem.Key = Encoding.UTF8.GetBytes("oi").Sha256();
            mptItem.Value = Encoding.UTF8.GetBytes("abc");
            mptItem[0] = Encoding.UTF8.GetBytes("turma").Sha256();
            mptItem[7] = Encoding.UTF8.GetBytes("turma7").Sha256();
            mptItem[10] = Encoding.UTF8.GetBytes("turma10").Sha256();

            var cloned = MerklePatriciaNode.LeafNode();
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    mptItem.Serialize(bw);
                    using (var br = new BinaryReader(bw.BaseStream))
                    {
                        br.BaseStream.Position = 0;
                        cloned.Deserialize(br);
                    }
                }
            }

            Assert.IsTrue(cloned.IsBranch);
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma").Sha256().SequenceEqual(cloned[0]));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma7").Sha256().SequenceEqual(cloned[7]));
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma10").Sha256().SequenceEqual(cloned[10]));

            Assert.AreEqual(mptItem, cloned);

            mptItem.Key = Encoding.UTF8.GetBytes("oi11").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("oi").Sha256().SequenceEqual(cloned.Key));
            mptItem.Value = Encoding.UTF8.GetBytes("abc45");
            Assert.IsTrue(Encoding.UTF8.GetBytes("abc").SequenceEqual(cloned.Value));
            mptItem[0] = Encoding.UTF8.GetBytes("turma0").Sha256();
            Assert.IsTrue(Encoding.UTF8.GetBytes("turma").Sha256().SequenceEqual(cloned[0]));
        }

        [TestMethod]
        public void EqualsBranch()
        {
            var mpNode = MerklePatriciaNode.BranchNode();
            Assert.AreNotEqual(mpNode, null);
            Assert.AreEqual(mpNode, mpNode);
            Assert.AreNotEqual(mpNode, MerklePatriciaNode.ExtensionNode());
        }

        [TestMethod]
        public void EqualsLeaf()
        {
            var mpNode = MerklePatriciaNode.LeafNode();
            Assert.AreNotEqual(mpNode, null);
            Assert.AreEqual(mpNode, mpNode);
            Assert.AreNotEqual(mpNode, MerklePatriciaNode.ExtensionNode());
        }

        [TestMethod]
        public void EqualsExtension()
        {
            var mpNode = MerklePatriciaNode.ExtensionNode();
            Assert.AreNotEqual(mpNode, null);
            Assert.AreEqual(mpNode, mpNode);
            Assert.AreNotEqual(mpNode, MerklePatriciaNode.LeafNode());
        }

        [TestMethod]
        public void ToStringBranch()
        {
            var mpNode = MerklePatriciaNode.BranchNode();
            Assert.AreEqual("{}", $"{mpNode}");
        }

        [TestMethod]
        public void UsingOnSet()
        {
            var setObj = new HashSet<MerklePatriciaNode> {MerklePatriciaNode.BranchNode()};
            Assert.AreEqual(1, setObj.Count);
            setObj.Add(MerklePatriciaNode.ExtensionNode());
            Assert.AreEqual(2, setObj.Count);
            setObj.Add(MerklePatriciaNode.LeafNode());
            Assert.AreEqual(3, setObj.Count);
            setObj.Add(MerklePatriciaNode.ExtensionNode());
            Assert.AreEqual(3, setObj.Count);

            var node = MerklePatriciaNode.LeafNode();
            node.Key = new byte[] {0, 1};
            node.Value = new byte[] {0, 1};
            setObj.Add(node);
            Assert.AreEqual(4, setObj.Count);
        }

        [TestMethod]
        public void ToStringExtension()
        {
            var mpNode = MerklePatriciaNode.ExtensionNode();
            Assert.AreEqual("[null,null]", $"{mpNode}");
        }

        [TestMethod]
        public void ToStringLeaf()
        {
            var mpNode = MerklePatriciaNode.LeafNode();
            Assert.AreEqual("[null,null,null]", $"{mpNode}");
        }
    }
}