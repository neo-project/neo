using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.Trie.MPT;
using System.Text;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_MPTTrie
    {
        private MPTNode root;
        private ISnapshot mptdb;

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
            var h1 = new HashNode();
            h1.Hash = Encoding.ASCII.GetBytes("hello");
            var l3 = new ShortNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();
            var v3 = new ValueNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");

            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;
            root = r;
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var db = new MPTDb(snapshot);
            db.PutRoot(root.GetHash());
            db.Put(r);
            db.Put(b);
            db.Put(l1);
            db.Put(l2);
            db.Put(l3);
            db.Put(v1);
            db.Put(v2);
            db.Put(v3);
            snapshot.Commit();
            this.mptdb = store.GetSnapshot();
        }

        [TestMethod]
        public void TestTryGet()
        {
            var mpt = new MPTTrie(mptdb);
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
            var mpt = new MPTTrie(mptdb);
            var result = mpt.TryGet("acae".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            Assert.IsTrue(Encoding.ASCII.GetBytes("hello").Equal(value));
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mpt1 = new MPTTrie(mptdb);
            Assert.AreEqual("743d2d1f400ae407d14ec19d68c5b6d8791633277a8917d75ab97be4ffed7172", mpt1.GetRoot().ToHexString());
            var mpt = new MPTTrie(store.GetSnapshot());
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac99".HexToBytes(), "2222".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("hello"));
            Assert.IsTrue(result);
            Assert.AreEqual("743d2d1f400ae407d14ec19d68c5b6d8791633277a8917d75ab97be4ffed7172", mpt.GetRoot().ToHexString());
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
            Assert.AreEqual("aae7f2cd9bcd3b3dadca286ccbecf03d7269fb4cc547fa40ba799abb89c3731f", r1.GetHash().ToHexString());
            Assert.AreEqual("a388b72bf6f8af80eed633fe95d7397bf51dcb23ceb2026979bb0b831893368b", r.GetHash().ToHexString());

            var mpt = new MPTTrie(mptdb);
            var result = true;
            result = mpt.TryGet("ac99".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac99".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("acae".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("aae7f2cd9bcd3b3dadca286ccbecf03d7269fb4cc547fa40ba799abb89c3731f", mpt.GetRoot().ToHexString());
        }

        [TestMethod]
        public void TestDeleteSameValue()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new MPTTrie(snapshot);
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
            mpt.Commit();
            snapshot.Commit();
            var mpt0 = new MPTTrie(store.GetSnapshot());
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
            var h1 = new HashNode();
            h1.Hash = Encoding.ASCII.GetBytes("hello");
            var l3 = new ShortNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();
            var v3 = new ValueNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");

            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;

            var mpt = new MPTTrie(mptdb);
            Assert.AreEqual(r.GetHash().ToHexString(), mpt.GetRoot().ToHexString());
            var proof = mpt.GetProof("ac01".HexToBytes());
            Assert.AreEqual(4, proof.Count);
            bool exist = false;
            foreach (byte[] item in proof)
            {
                if (item.Equal(b.Encode()))
                {
                    exist = true;
                    break;
                }
            }
            Assert.IsTrue(exist);
        }
    }
}
