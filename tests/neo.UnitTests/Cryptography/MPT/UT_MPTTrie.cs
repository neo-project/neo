using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.MPT;
using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.Cryptography.MPT
{
    public class TestKey : ISerializable
    {
        private byte[] key;

        public int Size => key.Length;

        public TestKey()
        {
            this.key = Array.Empty<byte>();
        }

        public TestKey(byte[] key)
        {
            this.key = key;
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(key);
        }

        public void Deserialize(BinaryReader reader)
        {
            key = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public override string ToString()
        {
            return key.ToHexString();
        }

        public static implicit operator TestKey(byte[] key)
        {
            return new TestKey(key);
        }
    }

    public class TestValue : ISerializable
    {
        private byte[] value;

        public int Size => value.Length;

        public TestValue()
        {
            this.value = Array.Empty<byte>();
        }

        public TestValue(byte[] value)
        {
            this.value = value;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public void Deserialize(BinaryReader reader)
        {
            value = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public override string ToString()
        {
            return value.ToHexString();
        }

        public static implicit operator TestValue(byte[] value)
        {
            return new TestValue(value);
        }
    }

    [TestClass]
    public class UT_MPTTrie
    {
        private MPTNode root;
        private IStore mptdb;

        private void PutToStore(MPTNode node)
        {
            mptdb.Put(0xf0, node.Hash.ToArray(), node.Encode());
        }

        [TestInitialize]
        public void TestInit()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0a0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };
            var v2 = new LeafNode { Value = "2222".HexToBytes() };
            var v3 = new LeafNode { Value = Encoding.ASCII.GetBytes("hello") };
            var h1 = new HashNode(v3.Hash);
            var l1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            var l2 = new ExtensionNode { Key = new byte[] { 0x09 }, Next = v2 };
            var l3 = new ExtensionNode { Key = "0e".HexToBytes(), Next = h1 };
            b.Children[0] = l1;
            b.Children[9] = l2;
            b.Children[10] = l3;
            this.root = r;
            this.mptdb = new MemoryStore();
            PutToStore(r);
            PutToStore(b);
            PutToStore(l1);
            PutToStore(l2);
            PutToStore(l3);
            PutToStore(v1);
            PutToStore(v2);
            PutToStore(v3);
        }

        [TestMethod]
        public void TestTryGet()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.AreEqual("abcd", mpt["ac01".HexToBytes()].ToString());
            Assert.AreEqual("2222", mpt["ac99".HexToBytes()].ToString());
            Assert.IsNull(mpt["ab99".HexToBytes()]);
            Assert.IsNull(mpt["ac39".HexToBytes()]);
            Assert.IsNull(mpt["ac02".HexToBytes()]);
            Assert.IsNull(mpt["ac9910".HexToBytes()]);
        }

        [TestMethod]
        public void TestTryGetResolve()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.AreEqual(Encoding.ASCII.GetBytes("hello").ToHexString(), mpt["acae".HexToBytes()].ToString());
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie<TestKey, TestValue>(store.GetSnapshot(), null);
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac99".HexToBytes(), "2222".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("hello"));
            Assert.IsTrue(result);
            Assert.AreEqual(root.Hash.ToString(), mpt.Root.Hash.ToString());
        }

        [TestMethod]
        public void TestTryDelete()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0a0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };
            var v2 = new LeafNode { Value = "2222".HexToBytes() };
            var r1 = new ExtensionNode { Key = "0a0c0001".HexToBytes(), Next = v1 };
            var l1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            var l2 = new ExtensionNode { Key = new byte[] { 0x09 }, Next = v2 };
            b.Children[0] = l1;
            b.Children[9] = l2;

            Assert.AreEqual("0xdea3ab46e9461e885ed7091c1e533e0a8030b248d39cbc638962394eaca0fbb3", r1.Hash.ToString());
            Assert.AreEqual("0x93e8e1ffe2f83dd92fca67330e273bcc811bf64b8f8d9d1b25d5e7366b47d60d", r.Hash.ToString());

            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.IsNotNull(mpt["ac99".HexToBytes()]);
            bool result = mpt.Delete("ac99".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Delete("acae".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("0xdea3ab46e9461e885ed7091c1e533e0a8030b248d39cbc638962394eaca0fbb3", mpt.Root.Hash.ToString());
        }

        [TestMethod]
        public void TestDeleteSameValue()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(snapshot, null);
            Assert.IsTrue(mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes()));
            Assert.IsTrue(mpt.Put("ac02".HexToBytes(), "abcd".HexToBytes()));
            Assert.IsNotNull(mpt["ac01".HexToBytes()]);
            Assert.IsNotNull(mpt["ac02".HexToBytes()]);
            mpt.Delete("ac01".HexToBytes());
            Assert.IsNotNull(mpt["ac02".HexToBytes()]);
            snapshot.Commit();

            var mpt0 = new MPTTrie<TestKey, TestValue>(store.GetSnapshot(), mpt.Root.Hash);
            Assert.IsNotNull(mpt0["ac02".HexToBytes()]);
        }

        [TestMethod]
        public void TestBranchNodeRemainValue()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie<TestKey, TestValue>(store.GetSnapshot(), null);
            Assert.IsTrue(mpt.Put("ac11".HexToBytes(), "ac11".HexToBytes()));
            Assert.IsTrue(mpt.Put("ac22".HexToBytes(), "ac22".HexToBytes()));
            Assert.IsTrue(mpt.Put("ac".HexToBytes(), "ac".HexToBytes()));
            Assert.IsTrue(mpt.Delete("ac11".HexToBytes()));
            mpt.Delete("ac22".HexToBytes());
            Assert.IsNotNull(mpt["ac".HexToBytes()]);
        }

        [TestMethod]
        public void TestGetProof()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0a0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };
            var v2 = new LeafNode { Value = "2222".HexToBytes() };
            var v3 = new LeafNode { Value = Encoding.ASCII.GetBytes("hello") };
            var h1 = new HashNode(v3.Hash);
            var l1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            var l2 = new ExtensionNode { Key = new byte[] { 0x09 }, Next = v2 };
            var l3 = new ExtensionNode { Key = "0e".HexToBytes(), Next = h1 };
            b.Children[0] = l1;
            b.Children[9] = l2;
            b.Children[10] = l3;

            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.AreEqual(r.Hash.ToString(), mpt.Root.Hash.ToString());
            HashSet<byte[]> proof = mpt.GetProof("ac01".HexToBytes());
            Assert.AreEqual(4, proof.Count);
            Assert.IsTrue(proof.Contains(b.Encode()));
            Assert.IsTrue(proof.Contains(r.Encode()));
            Assert.IsTrue(proof.Contains(l1.Encode()));
            Assert.IsTrue(proof.Contains(v1.Encode()));
        }

        [TestMethod]
        public void TestVerifyProof()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            HashSet<byte[]> proof = mpt.GetProof("ac01".HexToBytes());
            TestValue value = MPTTrie<TestKey, TestValue>.VerifyProof(root.Hash, "ac01".HexToBytes(), proof);
            Assert.IsNotNull(value);
            Assert.AreEqual(value.ToString(), "abcd");
        }

        [TestMethod]
        public void TestAddLongerKey()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(snapshot, null);
            var result = mpt.Put(new byte[] { 0xab }, new byte[] { 0x01 });
            Assert.IsTrue(result);
            result = mpt.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x02 });
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestSplitKey()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt1 = new MPTTrie<TestKey, TestValue>(snapshot, null);
            Assert.IsTrue(mpt1.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 }));
            Assert.IsTrue(mpt1.Put(new byte[] { 0xab }, new byte[] { 0x02 }));
            HashSet<byte[]> set1 = mpt1.GetProof(new byte[] { 0xab, 0xcd });
            Assert.AreEqual(4, set1.Count);
            var mpt2 = new MPTTrie<TestKey, TestValue>(snapshot, null);
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab }, new byte[] { 0x02 }));
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 }));
            HashSet<byte[]> set2 = mpt2.GetProof(new byte[] { 0xab, 0xcd });
            Assert.AreEqual(4, set2.Count);
            Assert.AreEqual(mpt1.Root.Hash, mpt2.Root.Hash);
        }

        [TestMethod]
        public void TestFind()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt1 = new MPTTrie<TestKey, TestValue>(snapshot, null);
            var results = mpt1.Find(ReadOnlySpan<byte>.Empty).ToArray();
            Assert.AreEqual(0, results.Count());
            var mpt2 = new MPTTrie<TestKey, TestValue>(snapshot, null);
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab, 0xcd, 0xef }, new byte[] { 0x01 }));
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab, 0xcd, 0xe1 }, new byte[] { 0x02 }));
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab }, new byte[] { 0x03 }));
            results = mpt2.Find(ReadOnlySpan<byte>.Empty).ToArray();
            Assert.AreEqual(3, results.Count());
            results = mpt2.Find(new byte[] { 0xab }).ToArray();
            Assert.AreEqual(3, results.Count());
            results = mpt2.Find(new byte[] { 0xab, 0xcd }).ToArray();
            Assert.AreEqual(2, results.Count());
            results = mpt2.Find(new byte[] { 0xac }).ToArray();
            Assert.AreEqual(0, results.Count());
        }

        [TestMethod]
        public void TestFindLeadNode()
        {
            // r.Key = 0x0a0c
            // b.Key = 0x00
            // l1.Key = 0x01
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            var prefix = new byte[] { 0xac, 0x01 }; // =  FromNibbles(path = { 0x0a, 0x0c, 0x00, 0x01 });
            var results = mpt.Find(prefix).ToArray();
            Assert.AreEqual(1, results.Count());

            prefix = new byte[] { 0xac }; // =  FromNibbles(path = { 0x0a, 0x0c });
            results = mpt.Find(prefix).ToArray();
            Assert.AreEqual(3, results.Count());
        }
    }
}
