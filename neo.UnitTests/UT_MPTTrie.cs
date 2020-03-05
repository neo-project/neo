using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Trie;
using Neo.Trie.MPT;
using System;
using System.Text;
using System.Collections.Generic;

namespace Neo.UnitTests.Trie.MPT
{
    class MemoryStore : IKVStore
    {
        private Dictionary<byte[], byte[]> store = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        public void Put(byte[] key, byte[] value)
        {
            store[key] = value;
        }

        public void Delete(byte[] key)
        {
            store.Remove(key);
        }

        public byte[] Get(byte[] key)
        {
            var result = store.TryGetValue(key, out byte[] value);
            if (result) return value;
            return Array.Empty<byte>();
        }
    }

    [TestClass]
    public class UT_MPTTrie
    {
        private MPTNode root;
        private IKVStore mptdb;

        private byte[] rootHash;

        [TestInitialize]
        public void TestInit()
        {
            var r = new ShortNode();
            r.Key = "0a0c".HexToBytes();
            var b = new FullNode();
            var l1 = new ShortNode();
            l1.Key = new byte[] { 0x01 };
            var l2 = new ShortNode();
            l2.Key = new byte[] { 0x09 };
            var v1 = new ValueNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new ValueNode();
            v2.Value = "2222".HexToBytes();
            var v3 = new ValueNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");
            var h1 = new HashNode();
            h1.Hash = v3.GetHash();
            var l3 = new ShortNode();
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
            var db = new MPTDb(store);
            this.rootHash = root.GetHash();
            db.Put(r);
            db.Put(b);
            db.Put(l1);
            db.Put(l2);
            db.Put(l3);
            db.Put(v1);
            db.Put(v2);
            db.Put(v3);
            this.mptdb = store;
        }

        [TestMethod]
        public void TestTryGet()
        {
            var mpt = new MPTTrie(rootHash, mptdb);
            var result = mpt.TryGet("ac01".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            Assert.AreEqual("abcd", value.ToHexString());

            result = mpt.TryGet("ac99".HexToBytes(), out value);
            Assert.IsTrue(result);
            Assert.AreEqual("2222", value.ToHexString());

            result = mpt.TryGet("ab99".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("ac39".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("ac02".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("ac9910".HexToBytes(), out value);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestTryGetResolve()
        {
            var mpt = new MPTTrie(rootHash, mptdb);
            var result = mpt.TryGet("acae".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            Assert.IsTrue(Encoding.ASCII.GetBytes("hello").Equal(value));
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie(null, store);
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac99".HexToBytes(), "2222".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("hello"));
            Assert.IsTrue(result);
            Assert.AreEqual(rootHash.ToHexString(), mpt.GetRoot().ToHexString());
        }

        [TestMethod]
        public void TestTryDelete()
        {
            var r1 = new ShortNode();
            r1.Key = "0a0c0001".HexToBytes();

            var r = new ShortNode();
            r.Key = "0a0c".HexToBytes();

            var b = new FullNode();
            r.Next = b;

            var l1 = new ShortNode();
            l1.Key = new byte[] { 0x01 };
            var v1 = new ValueNode();
            v1.Value = "abcd".HexToBytes();
            l1.Next = v1;
            b.Children[0] = l1;

            var l2 = new ShortNode();
            l2.Key = new byte[] { 0x09 };
            var v2 = new ValueNode();
            v2.Value = "2222".HexToBytes();
            l2.Next = v2;
            b.Children[9] = l2;

            r1.Next = v1;
            Assert.AreEqual("5e84cdc5d24f65c25bb87889c83a2120d2acfffcb38e6cdae55eecf777ea201c", r1.GetHash().ToHexString());
            Assert.AreEqual("74ec7c792212ee5822b7a87154668d97073e8d45e42afe8c1997bf98b2119498", r.GetHash().ToHexString());

            var mpt = new MPTTrie(rootHash, mptdb);
            var result = true;
            result = mpt.TryGet("ac99".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac99".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("acae".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("5e84cdc5d24f65c25bb87889c83a2120d2acfffcb38e6cdae55eecf777ea201c", mpt.GetRoot().ToHexString());
        }

        [TestMethod]
        public void TestDeleteSameValue()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie(null, store);
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac02".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryGet("ac01".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            result = mpt.TryGet("ac02".HexToBytes(), out value);
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac01".HexToBytes());
            result = mpt.TryGet("ac02".HexToBytes(), out value);
            Assert.IsTrue(result);

            var mpt0 = new MPTTrie(mpt.GetRoot(), store);
            result = mpt0.TryGet("ac02".HexToBytes(), out value);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestGetProof()
        {
            var r = new ShortNode();
            r.Key = "0a0c".HexToBytes();
            var b = new FullNode();
            var l1 = new ShortNode();
            l1.Key = new byte[] { 0x01 };
            var l2 = new ShortNode();
            l2.Key = new byte[] { 0x09 };
            var v1 = new ValueNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new ValueNode();
            v2.Value = "2222".HexToBytes();
            var v3 = new ValueNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");
            var h1 = new HashNode();
            h1.Hash = v3.GetHash();
            var l3 = new ShortNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();
            

            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;

            var mpt = new MPTTrie(rootHash, mptdb);
            Assert.AreEqual(r.GetHash().ToHexString(), mpt.GetRoot().ToHexString());
            var result = mpt.GetProof("ac01".HexToBytes(), out HashSet<byte[]> proof);
            Assert.IsTrue(result);
            Assert.AreEqual(4, proof.Count);
            Assert.IsTrue(proof.Contains(b.Encode()));
            Assert.IsTrue(proof.Contains(r.Encode()));
            Assert.IsTrue(proof.Contains(l1.Encode()));
            Assert.IsTrue(proof.Contains(v1.Encode()));
        }
    }
}
