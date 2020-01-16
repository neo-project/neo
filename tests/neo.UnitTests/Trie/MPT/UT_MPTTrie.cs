using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Trie.MPT;
using Neo.Persistence;
using Neo.IO;
using System;

namespace Neo.UnitTests.Trie.MPT
{
    [TestClass]
    public class UT_MPTTrie
    {
        private MPTNode root;

         [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
             
        }

        [TestInitialize]
        public void TestInit()
        {
            var r = new ShortNode();
            r.Key = "0a0c".HexToBytes();
            var b = new FullNode();
            var l1 = new ShortNode();
            l1.Key = new byte[]{0x01};
            var l2 = new ShortNode();
            l2.Key = new byte[]{0x09};
            var v1 = new ValueNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new ValueNode();
            v2.Value = "2222".HexToBytes();
            var h1 = new HashNode();
            h1.Hash = Encoding.ASCII.GetBytes("hello");
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
        }

        [TestMethod]
        public void TestTryGet()
        {
            var store = new MemoryStore();
            var mptdb = new MPTDatabase(store);
            var mpt = new MPTTrie(mptdb, root);
            var result = mpt.TryGet("0a0c0001".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            Assert.AreEqual("abcd", value.ToHexString());

            result = mpt.TryGet("0a0c0909".HexToBytes(), out value);
            Assert.IsTrue(result);
            Assert.AreEqual("2222", value.ToHexString());

            result = mpt.TryGet("0a0b0909".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("0a0c0309".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("0a0c0002".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("0a0c090901".HexToBytes(), out value);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestTryGetResolve()
        {
            var n = new ValueNode();
            n.Value = Encoding.ASCII.GetBytes("hello");
            var store = new MemoryStore();
            store.Put(MPTDatabase.TABLE, n.GetHash(), n.Encode());
            var mptdb = new MPTDatabase(store);
            var mpt = new MPTTrie(mptdb, root);
            var result = mpt.TryGet("0a0c0a0e".HexToBytes(), out byte[] value);

            Assert.IsTrue(result);
            Assert.IsTrue(value.Equal(n.Value));
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mptdb = new MPTDatabase(store);
            var sn = (ShortNode)root;
            Assert.IsTrue(root is ShortNode);
            Assert.AreEqual("c32dc0dee8cec33436eff759ee460c65d1a22c0a65a5edd27c68dd80ac3963b4", sn.GetHash().ToHexString());
            var mpt = new MPTTrie(mptdb, new byte[]{});
            mpt.TryPut("0a0c0001".HexToBytes(), "abcd".HexToBytes());
            mpt.TryPut("0a0c0909".HexToBytes(), "2222".HexToBytes());
            mpt.TryPut("0a0c0a0e".HexToBytes(), Encoding.ASCII.GetBytes("hello"));
            Assert.AreEqual("c32dc0dee8cec33436eff759ee460c65d1a22c0a65a5edd27c68dd80ac3963b4", mpt.GetRoot().ToHexString());
        }

        [TestMethod]
        public void TestTryDelete()
        {
            var store = new MemoryStore();
            var mptdb = new MPTDatabase(store);

            var r1 = new ShortNode();
            r1.Key = "0a0c0001".HexToBytes();
            

            var r = new ShortNode();
            r.Key = "0a0c".HexToBytes();


            var b = new FullNode();
            r.Next = b;

            var l1 = new ShortNode();
            l1.Key = new byte[]{0x01};
            var v1 = new ValueNode();
            v1.Value = "abcd".HexToBytes();
            l1.Next = v1;
            b.Children[0] = l1;

            var l2 = new ShortNode();
            l2.Key = new byte[]{0x09};
            var v2 = new ValueNode();
            v2.Value = "2222".HexToBytes();
            l2.Next = v2;
            b.Children[9] = l2;
            
            r1.Next = v1;
            Assert.AreEqual("76248d1bf457f0b95c1f6d05d787dca152906f106bcbafacbf7a69c6ae1797c4", r1.GetHash().ToHexString());
            Assert.AreEqual("f3ad94e8fb6e1e85a8b573b2343845e3b0e0b96b61fcd0e20b6df159fde137a7", r.GetHash().ToHexString());

            var mpt = new MPTTrie(mptdb, r);
            var result = true;
            result = mpt.TryGet("0a0c0909".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            result = mpt.TryDelete("0a0c0909".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("0a0c0a0e".HexToBytes());
            Assert.IsFalse(result);
            Assert.AreEqual("76248d1bf457f0b95c1f6d05d787dca152906f106bcbafacbf7a69c6ae1797c4", mpt.GetRoot().ToHexString());
        }

        [TestMethod]
        public void TestGetProof()
        {
            var r = new ShortNode();
            r.Key = "0a0c".HexToBytes();
            var b = new FullNode();
            var l1 = new ShortNode();
            l1.Key = new byte[]{0x01};
            var l2 = new ShortNode();
            l2.Key = new byte[]{0x09};
            var v1 = new ValueNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new ValueNode();
            v2.Value = "2222".HexToBytes();
            var h1 = new HashNode();
            h1.Hash = Encoding.ASCII.GetBytes("hello");
            var l3 = new ShortNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();

            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;

            var store = new MemoryStore();
            var mptdb = new MPTDatabase(store);
            var mpt = new MPTTrie(mptdb, r);

            Assert.AreEqual("c32dc0dee8cec33436eff759ee460c65d1a22c0a65a5edd27c68dd80ac3963b4", mpt.GetRoot().ToHexString());
            var dict = mpt.GetProof("0a0c0001".HexToBytes());
            Assert.AreEqual("c32dc0dee8cec33436eff759ee460c65d1a22c0a65a5edd27c68dd80ac3963b4", mpt.GetRoot().ToHexString());
            Assert.AreEqual(4, dict.Count);
            Assert.IsTrue(dict.TryGetValue(mpt.GetRoot(), out byte[] value));
            Assert.IsTrue(dict.TryGetValue(b.GetHash(), out value));
            Assert.IsTrue(dict.TryGetValue(l1.GetHash(), out value));
            Assert.IsTrue(dict.TryGetValue(v1.GetHash(), out value));                                                                        
        }
    }
}