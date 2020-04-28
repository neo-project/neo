using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Persistence;
using Neo.Trie.MPT;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Neo.UnitTests.Trie.MPT
{
    public class TestKey : ISerializable
    {
        private byte[] key;

        public int Size => key.Length;

        public TestKey()
        {

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

        private UInt256 rootHash;

        [TestInitialize]
        public void TestInit()
        {
            var r = new ExtensionNode();
            r.Key = "0a0c".HexToBytes();
            var b = new BranchNode();
            var l1 = new ExtensionNode();
            l1.Key = new byte[] { 0x01 };
            var l2 = new ExtensionNode();
            l2.Key = new byte[] { 0x09 };
            var v1 = new LeafNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new LeafNode();
            v2.Value = "2222".HexToBytes();
            var v3 = new LeafNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");
            var h1 = new HashNode();
            h1.Hash = v3.GetHash();
            var l3 = new ExtensionNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();

            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;
            root = r;
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var db = new MPTDb(snapshot, 0);
            this.rootHash = root.GetHash();
            db.Put(r);
            db.Put(b);
            db.Put(l1);
            db.Put(l2);
            db.Put(l3);
            db.Put(v1);
            db.Put(v2);
            db.Put(v3);
            snapshot.Commit();
            this.mptdb = store;
        }

        [TestMethod]
        public void TestTryGet()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(rootHash, mptdb.GetSnapshot(), 0);
            TestValue value = mpt.Get("ac01".HexToBytes());
            Assert.IsNotNull(value);
            Assert.AreEqual("abcd", value.ToString());

            value = mpt.Get("ac99".HexToBytes());
            Assert.IsNotNull(value);
            Assert.AreEqual("2222", value.ToString());

            value = mpt.Get("ab99".HexToBytes());
            Assert.IsNull(value);

            value = mpt.Get("ac39".HexToBytes());
            Assert.IsNull(value);

            value = mpt.Get("ac02".HexToBytes());
            Assert.IsNull(value);

            value = mpt.Get("ac9910".HexToBytes());
            Assert.IsNull(value);
        }

        [TestMethod]
        public void TestTryGetResolve()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(rootHash, mptdb.GetSnapshot(), 0);
            TestValue value = mpt.Get("acae".HexToBytes());
            Assert.IsNotNull(value);
            Assert.AreEqual(Encoding.ASCII.GetBytes("hello").ToHexString(), value.ToString());
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie<TestKey, TestValue>(null, store.GetSnapshot(), 0);
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac99".HexToBytes(), "2222".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("hello"));
            Assert.IsTrue(result);
            Assert.AreEqual(rootHash.ToString(), mpt.GetRoot().ToString());
        }

        [TestMethod]
        public void TestTryDelete()
        {
            var r1 = new ExtensionNode();
            r1.Key = "0a0c0001".HexToBytes();

            var r = new ExtensionNode();
            r.Key = "0a0c".HexToBytes();

            var b = new BranchNode();
            r.Next = b;

            var l1 = new ExtensionNode();
            l1.Key = new byte[] { 0x01 };
            var v1 = new LeafNode();
            v1.Value = "abcd".HexToBytes();
            l1.Next = v1;
            b.Children[0] = l1;

            var l2 = new ExtensionNode();
            l2.Key = new byte[] { 0x09 };
            var v2 = new LeafNode();
            v2.Value = "2222".HexToBytes();
            l2.Next = v2;
            b.Children[9] = l2;

            r1.Next = v1;
            Assert.AreEqual("0xdea3ab46e9461e885ed7091c1e533e0a8030b248d39cbc638962394eaca0fbb3", r1.GetHash().ToString());
            Assert.AreEqual("0x93e8e1ffe2f83dd92fca67330e273bcc811bf64b8f8d9d1b25d5e7366b47d60d", r.GetHash().ToString());

            var mpt = new MPTTrie<TestKey, TestValue>(rootHash, mptdb.GetSnapshot(), 0);
            var result = true;
            TestValue value = mpt.Get("ac99".HexToBytes());
            Assert.IsNotNull(value);
            result = mpt.TryDelete("ac99".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("acae".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("0xdea3ab46e9461e885ed7091c1e533e0a8030b248d39cbc638962394eaca0fbb3", mpt.GetRoot().ToString());
        }

        [TestMethod]
        public void TestDeleteSameValue()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(null, snapshot, 0);
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac02".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            TestValue value = mpt.Get("ac01".HexToBytes());
            Assert.IsNotNull(value);
            value = mpt.Get("ac02".HexToBytes());
            Assert.IsNotNull(value);
            result = mpt.TryDelete("ac01".HexToBytes());
            value = mpt.Get("ac02".HexToBytes());
            Assert.IsNotNull(value);
            snapshot.Commit();

            var mpt0 = new MPTTrie<TestKey, TestValue>(mpt.GetRoot(), store.GetSnapshot(), 0);
            value = mpt0.Get("ac02".HexToBytes());
            Assert.IsNotNull(value);
        }

        [TestMethod]
        public void TestBranchNodeRemainValue()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie<TestKey, TestValue>(null, store.GetSnapshot(), 0);
            var result = mpt.Put("ac11".HexToBytes(), "ac11".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac22".HexToBytes(), "ac22".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac".HexToBytes(), "ac".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac11".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac22".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("{\"key\":\"0a0c\",\"next\":{\"value\":\"ac\"}}", mpt.ToJson().ToString());
        }

        [TestMethod]
        public void TestGetProof()
        {
            var r = new ExtensionNode();
            r.Key = "0a0c".HexToBytes();
            var b = new BranchNode();
            var l1 = new ExtensionNode();
            l1.Key = new byte[] { 0x01 };
            var l2 = new ExtensionNode();
            l2.Key = new byte[] { 0x09 };
            var v1 = new LeafNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new LeafNode();
            v2.Value = "2222".HexToBytes();
            var v3 = new LeafNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");
            var h1 = new HashNode();
            h1.Hash = v3.GetHash();
            var l3 = new ExtensionNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();

            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;

            var mpt = new MPTTrie<TestKey, TestValue>(rootHash, mptdb.GetSnapshot(), 0);
            Assert.AreEqual(r.GetHash().ToString(), mpt.GetRoot().ToString());
            var result = mpt.GetProof("ac01".HexToBytes(), out HashSet<byte[]> proof);
            Assert.IsTrue(result);
            Assert.AreEqual(4, proof.Count);
            Assert.IsTrue(proof.Contains(b.Encode()));
            Assert.IsTrue(proof.Contains(r.Encode()));
            Assert.IsTrue(proof.Contains(l1.Encode()));
            Assert.IsTrue(proof.Contains(v1.Encode()));
        }

        [TestMethod]
        public void TestVerifyProof()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(rootHash, mptdb.GetSnapshot(), 0);
            var result = mpt.GetProof("ac01".HexToBytes(), out HashSet<byte[]> proof);
            Assert.IsTrue(result);
            TestValue value = MPTTrie<TestKey, TestValue>.VerifyProof(rootHash, "ac01".HexToBytes(), proof);
            Assert.IsNotNull(value);
            Assert.AreEqual(value.ToString(), "abcd");
        }

        [TestMethod]
        public void TestAddLongerKey()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(null, snapshot, 0);
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
            var mpt1 = new MPTTrie<TestKey, TestValue>(null, snapshot, 0);
            Assert.IsTrue(mpt1.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 }));
            Assert.IsTrue(mpt1.Put(new byte[] { 0xab }, new byte[] { 0x02 }));
            Assert.IsTrue(mpt1.GetProof(new byte[] { 0xab, 0xcd }, out HashSet<byte[]> set1));
            Assert.AreEqual(4, set1.Count);
            var mpt2 = new MPTTrie<TestKey, TestValue>(null, snapshot, 0);
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab }, new byte[] { 0x02 }));
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 }));
            Assert.IsTrue(mpt2.GetProof(new byte[] { 0xab, 0xcd }, out HashSet<byte[]> set2));
            Assert.AreEqual(4, set2.Count);
            Assert.AreEqual(mpt1.GetRoot(), mpt2.GetRoot());
        }
    }
}
