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
        private MPTDatabase mptdb;

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
            this.mptdb = new MPTDatabase(snapshot);
            mptdb.PutRoot(root.GetHash());
            mptdb.Put(r);
            mptdb.Put(b);
            mptdb.Put(l1);
            mptdb.Put(l2);
            mptdb.Put(l3);
            mptdb.Put(v1);
            mptdb.Put(v2);
            mptdb.Put(v3);
            snapshot.Commit();
            this.mptdb = new MPTDatabase(store.GetSnapshot());
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
            var db = new MPTDatabase(store.GetSnapshot());
            var mpt1 = new MPTTrie(mptdb);
            Assert.AreEqual("c32dc0dee8cec33436eff759ee460c65d1a22c0a65a5edd27c68dd80ac3963b4", mpt1.GetRoot().ToHexString());
            var mpt = new MPTTrie(db);
            mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            mpt.Put("ac99".HexToBytes(), "2222".HexToBytes());
            mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("hello"));
            Assert.AreEqual("c32dc0dee8cec33436eff759ee460c65d1a22c0a65a5edd27c68dd80ac3963b4", mpt.GetRoot().ToHexString());
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
            Assert.AreEqual("76248d1bf457f0b95c1f6d05d787dca152906f106bcbafacbf7a69c6ae1797c4", r1.GetHash().ToHexString());
            Assert.AreEqual("f3ad94e8fb6e1e85a8b573b2343845e3b0e0b96b61fcd0e20b6df159fde137a7", r.GetHash().ToHexString());

            var mpt = new MPTTrie(mptdb);
            var result = true;
            result = mpt.TryGet("ac99".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac99".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("acae".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("76248d1bf457f0b95c1f6d05d787dca152906f106bcbafacbf7a69c6ae1797c4", mpt.GetRoot().ToHexString());
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
            var dict = mpt.GetProof("ac01".HexToBytes());
            Assert.AreEqual(4, dict.Count);
            Assert.IsTrue(dict.TryGetValue(mpt.GetRoot(), out byte[] value));
        }
    }
}
