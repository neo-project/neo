using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.MPT;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Neo.Helper;

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

    class TestSnapshot : ISnapshot
    {
        public Dictionary<byte[], byte[]> store = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        private byte[] StoreKey(byte prefix, byte[] key)
        {
            return Concat(new byte[] { prefix }, key);
        }

        public void Put(byte prefix, byte[] key, byte[] value)
        {
            store[StoreKey(prefix, key)] = value;
        }

        public void Delete(byte prefix, byte[] key)
        {
            store.Remove(StoreKey(prefix, key));
        }

        public void Commit() { throw new System.NotImplementedException(); }

        public bool Contains(byte prefix, byte[] key) { throw new System.NotImplementedException(); }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte table, byte[] key, SeekDirection direction) { throw new System.NotImplementedException(); }

        public byte[] TryGet(byte prefix, byte[] key)
        {
            var result = store.TryGetValue(StoreKey(prefix, key), out byte[] value);
            if (result) return value;
            return null;
        }

        public void Dispose() { throw new System.NotImplementedException(); }

        public int Size => store.Count;
    }

    [TestClass]
    public class UT_MPTTrie
    {
        private MPTNode root;
        private IStore mptdb;

        private void PutToStore(IStore store, MPTNode node)
        {
            store.Put(0xf0, node.Hash.ToArray(), node.EncodeWithReference());
        }

        [TestInitialize]
        public void TestInit()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0a0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };//key=ac01
            var v2 = new LeafNode { Value = "2222".HexToBytes() };//key=ac
            var v3 = new LeafNode { Value = Encoding.ASCII.GetBytes("existing") };//key=acae
            var v4 = new LeafNode { Value = Encoding.ASCII.GetBytes("missing") };
            var h3 = new HashNode(v3.Hash);
            var e1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            var e3 = new ExtensionNode { Key = new byte[] { 0x0e }, Next = h3 };
            var e4 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v4 };
            b.Children[0] = e1;
            b.Children[10] = e3;
            b.Children[16] = v2;
            b.Children[15] = new HashNode(e4.Hash);
            this.root = r;
            this.mptdb = new MemoryStore();
            PutToStore(mptdb, r);
            PutToStore(mptdb, b);
            PutToStore(mptdb, e1);
            PutToStore(mptdb, e3);
            PutToStore(mptdb, v1);
            PutToStore(mptdb, v2);
            PutToStore(mptdb, v3);
        }

        [TestMethod]
        public void TestTryGet()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.IsNull(mpt[Array.Empty<byte>()]);
            Assert.AreEqual("abcd", mpt["ac01".HexToBytes()].ToString());
            Assert.AreEqual("2222", mpt["ac".HexToBytes()].ToString());
            Assert.IsNull(mpt["ab99".HexToBytes()]);
            Assert.IsNull(mpt["ac39".HexToBytes()]);
            Assert.IsNull(mpt["ac02".HexToBytes()]);
            Assert.IsNull(mpt["ac0100".HexToBytes()]);
            Assert.IsNull(mpt["ac9910".HexToBytes()]);
            Assert.ThrowsException<InvalidOperationException>(() => mpt["acf1".HexToBytes()]);
        }

        [TestMethod]
        public void TestTryGetResolve()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.AreEqual(Encoding.ASCII.GetBytes("existing").ToHexString(), mpt["acae".HexToBytes()].ToString());
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie<TestKey, TestValue>(store.GetSnapshot(), null);
            Assert.IsTrue(mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes()));
            Assert.IsTrue(mpt.Put("ac".HexToBytes(), "2222".HexToBytes()));
            Assert.IsTrue(mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("existing")));
            Assert.IsTrue(mpt.Put("acf1".HexToBytes(), Encoding.ASCII.GetBytes("missing")));
            Assert.AreEqual(root.Hash.ToString(), mpt.Root.Hash.ToString());
            Assert.IsFalse(mpt.Put(Array.Empty<byte>(), "01".HexToBytes()));
            Assert.IsFalse(mpt.Put("01".HexToBytes(), Array.Empty<byte>()));
            Assert.IsFalse(mpt.Put(new byte[ExtensionNode.MaxKeyLength / 2 + 1], Array.Empty<byte>()));
            Assert.IsFalse(mpt.Put("01".HexToBytes(), new byte[LeafNode.MaxValueLength + 1]));
            Assert.IsTrue(mpt.Put("ac01".HexToBytes(), "ab".HexToBytes()));
        }

        [TestMethod]
        public void TestPutCantResolve()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Put("acf111".HexToBytes(), new byte[] { 1 }));
        }

        [TestMethod]
        public void TestTryDelete()
        {
            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), root.Hash);
            Assert.IsNotNull(mpt["ac".HexToBytes()]);
            Assert.IsFalse(mpt.Delete("0c99".HexToBytes()));
            Assert.IsFalse(mpt.Delete(Array.Empty<byte>()));
            Assert.IsFalse(mpt.Delete("ac20".HexToBytes()));
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Delete("acf1".HexToBytes()));
            Assert.IsTrue(mpt.Delete("ac".HexToBytes()));
            Assert.IsFalse(mpt.Delete("acae01".HexToBytes()));
            Assert.IsTrue(mpt.Delete("acae".HexToBytes()));
            Assert.AreEqual("0x1741d4cb65bcf6700062f2acf2d74bb4657514b564ea8de969a267fe4198950e", mpt.Root.Hash.ToString());
        }

        [TestMethod]
        public void TestDeleteRemainCanResolve()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt1 = new MPTTrie<TestKey, TestValue>(snapshot, null);
            Assert.IsTrue(mpt1.Put("ac00".HexToBytes(), "abcd".HexToBytes()));
            Assert.IsTrue(mpt1.Put("ac10".HexToBytes(), "abcd".HexToBytes()));
            snapshot.Commit();
            var mpt2 = new MPTTrie<TestKey, TestValue>(store.GetSnapshot(), mpt1.Root.Hash);
            Assert.IsTrue(mpt2.Delete("ac00".HexToBytes()));
            Assert.IsTrue(mpt2.Delete("ac10".HexToBytes()));
        }

        [TestMethod]
        public void TestDeleteRemainCantResolve()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0a0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };//key=ac01
            var v4 = new LeafNode { Value = Encoding.ASCII.GetBytes("missing") };
            var e1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            var e4 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v4 };
            b.Children[0] = e1;
            b.Children[15] = new HashNode(e4.Hash);
            var store = new MemoryStore();
            PutToStore(store, r);
            PutToStore(store, b);
            PutToStore(store, e1);
            PutToStore(store, v1);

            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(snapshot, r.Hash);
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Delete("ac01".HexToBytes()));
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
            var snapshot = new TestSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(snapshot, null);
            Assert.IsTrue(mpt.Put("ac11".HexToBytes(), "ac11".HexToBytes()));
            Assert.IsTrue(mpt.Put("ac22".HexToBytes(), "ac22".HexToBytes()));
            Assert.IsTrue(mpt.Put("ac".HexToBytes(), "ac".HexToBytes()));
            Assert.AreEqual(7, snapshot.Size);
            Assert.IsTrue(mpt.Delete("ac11".HexToBytes()));
            Assert.AreEqual(5, snapshot.Size);
            Assert.IsTrue(mpt.Delete("ac22".HexToBytes()));
            Assert.IsNotNull(mpt["ac".HexToBytes()]);
            Assert.AreEqual(2, snapshot.Size);
        }

        [TestMethod]
        public void TestGetProof()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0a0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };//key=ac01
            var v2 = new LeafNode { Value = "2222".HexToBytes() };//key=ac
            var v3 = new LeafNode { Value = Encoding.ASCII.GetBytes("existing") };//key=acae
            var v4 = new LeafNode { Value = Encoding.ASCII.GetBytes("missing") };
            var h3 = new HashNode(v3.Hash);
            var e1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            var e3 = new ExtensionNode { Key = new byte[] { 0x0e }, Next = h3 };
            var e4 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v4 };
            b.Children[0] = e1;
            b.Children[10] = e3;
            b.Children[16] = v2;
            b.Children[15] = new HashNode(e4.Hash);

            var mpt = new MPTTrie<TestKey, TestValue>(mptdb.GetSnapshot(), r.Hash);
            Assert.AreEqual(r.Hash.ToString(), mpt.Root.Hash.ToString());
            HashSet<byte[]> proof = mpt.GetProof("ac01".HexToBytes());
            Assert.AreEqual(4, proof.Count);
            Assert.IsTrue(proof.Contains(b.Encode()));
            Assert.IsTrue(proof.Contains(r.Encode()));
            Assert.IsTrue(proof.Contains(e1.Encode()));
            Assert.IsTrue(proof.Contains(v1.Encode()));

            proof = mpt.GetProof("ac".HexToBytes());
            Assert.AreEqual(3, proof.Count());

            proof = mpt.GetProof("ac10".HexToBytes());
            Assert.IsNull(proof);

            proof = mpt.GetProof("acae".HexToBytes());
            Assert.AreEqual(4, proof.Count());

            proof = mpt.GetProof(Array.Empty<byte>());
            Assert.IsNull(proof);

            proof = mpt.GetProof("ac0100".HexToBytes());
            Assert.IsNull(proof);

            Assert.ThrowsException<InvalidOperationException>(() => mpt.GetProof("acf1".HexToBytes()));
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
            Assert.AreEqual("01", mpt[new byte[] { 0xab }].ToArray().ToHexString());
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
            results = mpt2.Find(new byte[] { 0xab, 0xcd, 0xef, 0x00 }).ToArray();
            Assert.AreEqual(0, results.Count());
        }

        [TestMethod]
        public void TestFindCantResolve()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0a0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };//key=ac01
            var v4 = new LeafNode { Value = Encoding.ASCII.GetBytes("missing") };
            var e1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            var e4 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v4 };
            b.Children[0] = e1;
            b.Children[15] = new HashNode(e4.Hash);
            var store = new MemoryStore();
            PutToStore(store, r);
            PutToStore(store, b);
            PutToStore(store, e1);
            PutToStore(store, v1);

            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(snapshot, r.Hash);
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Find("ac".HexToBytes()).Count());
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
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Find(prefix).ToArray());
        }

        [TestMethod]
        public void TestFromNibblesException()
        {
            var b = new BranchNode();
            var r = new ExtensionNode { Key = "0c".HexToBytes(), Next = b };
            var v1 = new LeafNode { Value = "abcd".HexToBytes() };//key=ac01
            var v2 = new LeafNode { Value = "2222".HexToBytes() };//key=ac
            var e1 = new ExtensionNode { Key = new byte[] { 0x01 }, Next = v1 };
            b.Children[0] = e1;
            b.Children[16] = v2;
            var store = new MemoryStore();
            PutToStore(store, r);
            PutToStore(store, b);
            PutToStore(store, e1);
            PutToStore(store, v1);
            PutToStore(store, v2);

            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(snapshot, r.Hash);
            Assert.ThrowsException<FormatException>(() => mpt.Find(Array.Empty<byte>()).Count());
        }

        [TestMethod]
        public void TestReference()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie<TestKey, TestValue>(snapshot, null);
            mpt.Put("a101".HexToBytes(), "01".HexToBytes());
            mpt.Put("a201".HexToBytes(), "01".HexToBytes());
            mpt.Put("a301".HexToBytes(), "01".HexToBytes());
            snapshot.Commit();
            var snapshot1 = store.GetSnapshot();
            var mpt1 = new MPTTrie<TestKey, TestValue>(snapshot1, mpt.Root.Hash);
            mpt1.Delete("a301".HexToBytes());
            snapshot1.Commit();
            var snapshot2 = store.GetSnapshot();
            var mpt2 = new MPTTrie<TestKey, TestValue>(snapshot2, mpt1.Root.Hash);
            mpt2.Delete("a201".HexToBytes());
            Assert.AreEqual("01", mpt2["a101".HexToBytes()]?.ToArray().ToHexString());
        }
    }
}
