// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Trie.cs file belongs to the neo project and is free
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
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Neo.Helper;

namespace Neo.Cryptography.MPTTrie.Tests
{
    class TestSnapshot : ISnapshot
    {
        public Dictionary<byte[], byte[]> store = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        private byte[] StoreKey(byte[] key)
        {
            return [.. key];
        }

        public void Put(byte[] key, byte[] value)
        {
            store[key] = value;
        }

        public void Delete(byte[] key)
        {
            store.Remove(StoreKey(key));
        }

        public void Commit() { throw new NotImplementedException(); }

        public bool Contains(byte[] key) { throw new System.NotImplementedException(); }

        public IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[] key, SeekDirection direction) { throw new System.NotImplementedException(); }

        public byte[] TryGet(byte[] key)
        {
            var result = store.TryGetValue(StoreKey(key), out byte[] value);
            if (result) return value;
            return null;
        }

        public void Dispose() { throw new System.NotImplementedException(); }

        public int Size => store.Count;
    }

    [TestClass]
    public class UT_Trie
    {
        private Node root;
        private IStore mptdb;

        private void PutToStore(IStore store, Node node)
        {
            store.Put([.. new byte[] { 0xf0 }, .. node.Hash.ToArray()], node.ToArray());
        }

        [TestInitialize]
        public void TestInit()
        {
            var b = Node.NewBranch();
            var r = Node.NewExtension("0a0c".HexToBytes(), b);
            var v1 = Node.NewLeaf("abcd".HexToBytes());//key=ac01
            var v2 = Node.NewLeaf("2222".HexToBytes());//key=ac
            var v3 = Node.NewLeaf(Encoding.ASCII.GetBytes("existing"));//key=acae
            var v4 = Node.NewLeaf(Encoding.ASCII.GetBytes("missing"));
            var h3 = Node.NewHash(v3.Hash);
            var e1 = Node.NewExtension(new byte[] { 0x01 }, v1);
            var e3 = Node.NewExtension(new byte[] { 0x0e }, h3);
            var e4 = Node.NewExtension(new byte[] { 0x01 }, v4);
            b.Children[0] = e1;
            b.Children[10] = e3;
            b.Children[16] = v2;
            b.Children[15] = Node.NewHash(e4.Hash);
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
            var mpt = new Trie(mptdb.GetSnapshot(), root.Hash);
            Assert.ThrowsException<ArgumentException>(() => mpt[Array.Empty<byte>()]);
            Assert.AreEqual("abcd", mpt["ac01".HexToBytes()].ToHexString());
            Assert.AreEqual("2222", mpt["ac".HexToBytes()].ToHexString());
            Assert.ThrowsException<KeyNotFoundException>(() => mpt["ab99".HexToBytes()]);
            Assert.ThrowsException<KeyNotFoundException>(() => mpt["ac39".HexToBytes()]);
            Assert.ThrowsException<KeyNotFoundException>(() => mpt["ac02".HexToBytes()]);
            Assert.ThrowsException<KeyNotFoundException>(() => mpt["ac0100".HexToBytes()]);
            Assert.ThrowsException<KeyNotFoundException>(() => mpt["ac9910".HexToBytes()]);
            Assert.ThrowsException<InvalidOperationException>(() => mpt["acf1".HexToBytes()]);
        }

        [TestMethod]
        public void TestTryGetResolve()
        {
            var mpt = new Trie(mptdb.GetSnapshot(), root.Hash);
            Assert.AreEqual(Encoding.ASCII.GetBytes("existing").ToHexString(), mpt["acae".HexToBytes()].ToHexString());
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mpt = new Trie(store.GetSnapshot(), null);
            mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            mpt.Put("ac".HexToBytes(), "2222".HexToBytes());
            mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("existing"));
            mpt.Put("acf1".HexToBytes(), Encoding.ASCII.GetBytes("missing"));
            Assert.AreEqual(root.Hash.ToString(), mpt.Root.Hash.ToString());
            Assert.ThrowsException<ArgumentException>(() => mpt.Put(Array.Empty<byte>(), "01".HexToBytes()));
            mpt.Put("01".HexToBytes(), Array.Empty<byte>());
            Assert.ThrowsException<ArgumentException>(() => mpt.Put(new byte[Node.MaxKeyLength / 2 + 1], Array.Empty<byte>()));
            Assert.ThrowsException<ArgumentException>(() => mpt.Put("01".HexToBytes(), new byte[Node.MaxValueLength + 1]));
            mpt.Put("ac01".HexToBytes(), "ab".HexToBytes());
        }

        [TestMethod]
        public void TestPutCantResolve()
        {
            var mpt = new Trie(mptdb.GetSnapshot(), root.Hash);
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Put("acf111".HexToBytes(), new byte[] { 1 }));
        }

        [TestMethod]
        public void TestTryDelete()
        {
            var mpt = new Trie(mptdb.GetSnapshot(), root.Hash);
            Assert.IsNotNull(mpt["ac".HexToBytes()]);
            Assert.IsFalse(mpt.Delete("0c99".HexToBytes()));
            Assert.ThrowsException<ArgumentException>(() => mpt.Delete(Array.Empty<byte>()));
            Assert.IsFalse(mpt.Delete("ac20".HexToBytes()));
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Delete("acf1".HexToBytes()));
            Assert.IsTrue(mpt.Delete("ac".HexToBytes()));
            Assert.IsFalse(mpt.Delete("acae01".HexToBytes()));
            Assert.IsTrue(mpt.Delete("acae".HexToBytes()));
            Assert.AreEqual("0xcb06925428b7c727375c7fdd943a302fe2c818cf2e2eaf63a7932e3fd6cb3408", mpt.Root.Hash.ToString());
        }

        [TestMethod]
        public void TestDeleteRemainCanResolve()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt1 = new Trie(snapshot, null);
            mpt1.Put("ac00".HexToBytes(), "abcd".HexToBytes());
            mpt1.Put("ac10".HexToBytes(), "abcd".HexToBytes());
            mpt1.Commit();
            snapshot.Commit();
            var snapshot2 = store.GetSnapshot();
            var mpt2 = new Trie(snapshot2, mpt1.Root.Hash);
            Assert.IsTrue(mpt2.Delete("ac00".HexToBytes()));
            mpt2.Commit();
            snapshot2.Commit();
            Assert.IsTrue(mpt2.Delete("ac10".HexToBytes()));
        }

        [TestMethod]
        public void TestDeleteRemainCantResolve()
        {
            var b = Node.NewBranch();
            var r = Node.NewExtension("0a0c".HexToBytes(), b);
            var v1 = Node.NewLeaf("abcd".HexToBytes());//key=ac01
            var v4 = Node.NewLeaf(Encoding.ASCII.GetBytes("missing"));
            var e1 = Node.NewExtension(new byte[] { 0x01 }, v1);
            var e4 = Node.NewExtension(new byte[] { 0x01 }, v4);
            b.Children[0] = e1;
            b.Children[15] = Node.NewHash(e4.Hash);
            var store = new MemoryStore();
            PutToStore(store, r);
            PutToStore(store, b);
            PutToStore(store, e1);
            PutToStore(store, v1);

            var snapshot = store.GetSnapshot();
            var mpt = new Trie(snapshot, r.Hash);
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Delete("ac01".HexToBytes()));
        }


        [TestMethod]
        public void TestDeleteSameValue()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            mpt.Put("ac02".HexToBytes(), "abcd".HexToBytes());
            Assert.IsNotNull(mpt["ac01".HexToBytes()]);
            Assert.IsNotNull(mpt["ac02".HexToBytes()]);
            mpt.Delete("ac01".HexToBytes());
            Assert.IsNotNull(mpt["ac02".HexToBytes()]);
            mpt.Commit();
            snapshot.Commit();
            var mpt0 = new Trie(store.GetSnapshot(), mpt.Root.Hash);
            Assert.IsNotNull(mpt0["ac02".HexToBytes()]);
        }

        [TestMethod]
        public void TestBranchNodeRemainValue()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("ac11".HexToBytes(), "ac11".HexToBytes());
            mpt.Put("ac22".HexToBytes(), "ac22".HexToBytes());
            mpt.Put("ac".HexToBytes(), "ac".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(7, snapshot.Size);
            Assert.IsTrue(mpt.Delete("ac11".HexToBytes()));
            mpt.Commit();
            Assert.AreEqual(5, snapshot.Size);
            Assert.IsTrue(mpt.Delete("ac22".HexToBytes()));
            Assert.IsNotNull(mpt["ac".HexToBytes()]);
            mpt.Commit();
            Assert.AreEqual(2, snapshot.Size);
        }

        [TestMethod]
        public void TestGetProof()
        {
            var b = Node.NewBranch();
            var r = Node.NewExtension("0a0c".HexToBytes(), b);
            var v1 = Node.NewLeaf("abcd".HexToBytes());//key=ac01
            var v2 = Node.NewLeaf("2222".HexToBytes());//key=ac
            var v3 = Node.NewLeaf(Encoding.ASCII.GetBytes("existing"));//key=acae
            var v4 = Node.NewLeaf(Encoding.ASCII.GetBytes("missing"));
            var h3 = Node.NewHash(v3.Hash);
            var e1 = Node.NewExtension(new byte[] { 0x01 }, v1);
            var e3 = Node.NewExtension(new byte[] { 0x0e }, h3);
            var e4 = Node.NewExtension(new byte[] { 0x01 }, v4);
            b.Children[0] = e1;
            b.Children[10] = e3;
            b.Children[16] = v2;
            b.Children[15] = Node.NewHash(e4.Hash);

            var mpt = new Trie(mptdb.GetSnapshot(), r.Hash);
            Assert.AreEqual(r.Hash.ToString(), mpt.Root.Hash.ToString());
            var result = mpt.TryGetProof("ac01".HexToBytes(), out var proof);
            Assert.IsTrue(result);
            Assert.AreEqual(4, proof.Count);
            Assert.IsTrue(proof.Contains(b.ToArrayWithoutReference()));
            Assert.IsTrue(proof.Contains(r.ToArrayWithoutReference()));
            Assert.IsTrue(proof.Contains(e1.ToArrayWithoutReference()));
            Assert.IsTrue(proof.Contains(v1.ToArrayWithoutReference()));

            result = mpt.TryGetProof("ac".HexToBytes(), out proof);
            Assert.AreEqual(3, proof.Count);

            result = mpt.TryGetProof("ac10".HexToBytes(), out proof);
            Assert.IsFalse(result);

            result = mpt.TryGetProof("acae".HexToBytes(), out proof);
            Assert.AreEqual(4, proof.Count);

            Assert.ThrowsException<ArgumentException>(() => mpt.TryGetProof(Array.Empty<byte>(), out proof));

            result = mpt.TryGetProof("ac0100".HexToBytes(), out proof);
            Assert.IsFalse(result);

            Assert.ThrowsException<InvalidOperationException>(() => mpt.TryGetProof("acf1".HexToBytes(), out var proof));
        }

        [TestMethod]
        public void TestVerifyProof()
        {
            var mpt = new Trie(mptdb.GetSnapshot(), root.Hash);
            var result = mpt.TryGetProof("ac01".HexToBytes(), out var proof);
            Assert.IsTrue(result);
            var value = Trie.VerifyProof(root.Hash, "ac01".HexToBytes(), proof);
            Assert.IsNotNull(value);
            Assert.AreEqual(value.ToHexString(), "abcd");
        }

        [TestMethod]
        public void TestAddLongerKey()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put(new byte[] { 0xab }, new byte[] { 0x01 });
            mpt.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x02 });
            Assert.AreEqual("01", mpt[new byte[] { 0xab }].ToHexString());
        }

        [TestMethod]
        public void TestSplitKey()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt1 = new Trie(snapshot, null);
            mpt1.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 });
            mpt1.Put(new byte[] { 0xab }, new byte[] { 0x02 });
            var r = mpt1.TryGetProof(new byte[] { 0xab, 0xcd }, out var set1);
            Assert.IsTrue(r);
            Assert.AreEqual(4, set1.Count);
            var mpt2 = new Trie(snapshot, null);
            mpt2.Put(new byte[] { 0xab }, new byte[] { 0x02 });
            mpt2.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 });
            r = mpt2.TryGetProof(new byte[] { 0xab, 0xcd }, out var set2);
            Assert.IsTrue(r);
            Assert.AreEqual(4, set2.Count);
            Assert.AreEqual(mpt1.Root.Hash, mpt2.Root.Hash);
        }

        [TestMethod]
        public void TestFind()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt1 = new Trie(snapshot, null);
            var results = mpt1.Find(ReadOnlySpan<byte>.Empty).ToArray();
            Assert.AreEqual(0, results.Length);
            var mpt2 = new Trie(snapshot, null);
            mpt2.Put(new byte[] { 0xab, 0xcd, 0xef }, new byte[] { 0x01 });
            mpt2.Put(new byte[] { 0xab, 0xcd, 0xe1 }, new byte[] { 0x02 });
            mpt2.Put(new byte[] { 0xab }, new byte[] { 0x03 });
            results = mpt2.Find(ReadOnlySpan<byte>.Empty).ToArray();
            Assert.AreEqual(3, results.Length);
            results = mpt2.Find(new byte[] { 0xab }).ToArray();
            Assert.AreEqual(3, results.Length);
            results = mpt2.Find(new byte[] { 0xab, 0xcd }).ToArray();
            Assert.AreEqual(2, results.Length);
            results = mpt2.Find(new byte[] { 0xac }).ToArray();
            Assert.AreEqual(0, results.Length);
            results = mpt2.Find(new byte[] { 0xab, 0xcd, 0xef, 0x00 }).ToArray();
            Assert.AreEqual(0, results.Length);
        }

        [TestMethod]
        public void TestFindCantResolve()
        {
            var b = Node.NewBranch();
            var r = Node.NewExtension("0a0c".HexToBytes(), b);
            var v1 = Node.NewLeaf("abcd".HexToBytes());//key=ac01
            var v4 = Node.NewLeaf(Encoding.ASCII.GetBytes("missing"));
            var e1 = Node.NewExtension(new byte[] { 0x01 }, v1);
            var e4 = Node.NewExtension(new byte[] { 0x01 }, v4);
            b.Children[0] = e1;
            b.Children[15] = Node.NewHash(e4.Hash);
            var store = new MemoryStore();
            PutToStore(store, r);
            PutToStore(store, b);
            PutToStore(store, e1);
            PutToStore(store, v1);

            var snapshot = store.GetSnapshot();
            var mpt = new Trie(snapshot, r.Hash);
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Find("ac".HexToBytes()).Count());
        }

        [TestMethod]
        public void TestFindLeadNode()
        {
            // r.Key = 0x0a0c
            // b.Key = 0x00
            // l1.Key = 0x01
            var mpt = new Trie(mptdb.GetSnapshot(), root.Hash);
            var prefix = new byte[] { 0xac, 0x01 }; // =  FromNibbles(path = { 0x0a, 0x0c, 0x00, 0x01 });
            var results = mpt.Find(prefix).ToArray();
            Assert.AreEqual(1, results.Count());

            prefix = new byte[] { 0xac }; // =  FromNibbles(path = { 0x0a, 0x0c });
            Assert.ThrowsException<InvalidOperationException>(() => mpt.Find(prefix).ToArray());
        }

        [TestMethod]
        public void TestFromNibblesException()
        {
            var b = Node.NewBranch();
            var r = Node.NewExtension("0c".HexToBytes(), b);
            var v1 = Node.NewLeaf("abcd".HexToBytes());//key=ac01
            var v2 = Node.NewLeaf("2222".HexToBytes());//key=ac
            var e1 = Node.NewExtension(new byte[] { 0x01 }, v1);
            b.Children[0] = e1;
            b.Children[16] = v2;
            var store = new MemoryStore();
            PutToStore(store, r);
            PutToStore(store, b);
            PutToStore(store, e1);
            PutToStore(store, v1);
            PutToStore(store, v2);

            var snapshot = store.GetSnapshot();
            var mpt = new Trie(snapshot, r.Hash);
            Assert.ThrowsException<FormatException>(() => mpt.Find(Array.Empty<byte>()).Count());
        }

        [TestMethod]
        public void TestReference1()
        {
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("a101".HexToBytes(), "01".HexToBytes());
            mpt.Put("a201".HexToBytes(), "01".HexToBytes());
            mpt.Put("a301".HexToBytes(), "01".HexToBytes());
            mpt.Commit();
            snapshot.Commit();
            var snapshot1 = store.GetSnapshot();
            var mpt1 = new Trie(snapshot1, mpt.Root.Hash);
            mpt1.Delete("a301".HexToBytes());
            mpt1.Commit();
            snapshot1.Commit();
            var snapshot2 = store.GetSnapshot();
            var mpt2 = new Trie(snapshot2, mpt1.Root.Hash);
            mpt2.Delete("a201".HexToBytes());
            Assert.AreEqual("01", mpt2["a101".HexToBytes()].ToHexString());
        }

        [TestMethod]
        public void TestReference2()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("a101".HexToBytes(), "01".HexToBytes());
            mpt.Put("a201".HexToBytes(), "01".HexToBytes());
            mpt.Put("a301".HexToBytes(), "01".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(4, snapshot.Size);
            mpt.Delete("a301".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(4, snapshot.Size);
            mpt.Delete("a201".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(2, snapshot.Size);
            Assert.AreEqual("01", mpt["a101".HexToBytes()].ToHexString());
        }


        [TestMethod]
        public void TestExtensionDeleteDirty()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("a1".HexToBytes(), "01".HexToBytes());
            mpt.Put("a2".HexToBytes(), "02".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(4, snapshot.Size);
            var mpt1 = new Trie(snapshot, mpt.Root.Hash);
            mpt1.Delete("a1".HexToBytes());
            mpt1.Commit();
            Assert.AreEqual(2, snapshot.Size);
            var mpt2 = new Trie(snapshot, mpt1.Root.Hash);
            mpt2.Delete("a2".HexToBytes());
            mpt2.Commit();
            Assert.AreEqual(0, snapshot.Size);
        }

        [TestMethod]
        public void TestBranchDeleteDirty()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("10".HexToBytes(), "01".HexToBytes());
            mpt.Put("20".HexToBytes(), "02".HexToBytes());
            mpt.Put("30".HexToBytes(), "03".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(7, snapshot.Size);
            var mpt1 = new Trie(snapshot, mpt.Root.Hash);
            mpt1.Delete("10".HexToBytes());
            mpt1.Commit();
            Assert.AreEqual(5, snapshot.Size);
            var mpt2 = new Trie(snapshot, mpt1.Root.Hash);
            mpt2.Delete("20".HexToBytes());
            mpt2.Commit();
            Assert.AreEqual(2, snapshot.Size);
            var mpt3 = new Trie(snapshot, mpt2.Root.Hash);
            mpt3.Delete("30".HexToBytes());
            mpt3.Commit();
            Assert.AreEqual(0, snapshot.Size);
        }

        [TestMethod]
        public void TestExtensionPutDirty()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("a1".HexToBytes(), "01".HexToBytes());
            mpt.Put("a2".HexToBytes(), "02".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(4, snapshot.Size);
            var mpt1 = new Trie(snapshot, mpt.Root.Hash);
            mpt1.Put("a3".HexToBytes(), "03".HexToBytes());
            mpt1.Commit();
            Assert.AreEqual(5, snapshot.Size);
        }

        [TestMethod]
        public void TestBranchPutDirty()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("10".HexToBytes(), "01".HexToBytes());
            mpt.Put("20".HexToBytes(), "02".HexToBytes());
            mpt.Commit();
            Assert.AreEqual(5, snapshot.Size);
            var mpt1 = new Trie(snapshot, mpt.Root.Hash);
            mpt1.Put("30".HexToBytes(), "03".HexToBytes());
            mpt1.Commit();
            Assert.AreEqual(7, snapshot.Size);
        }

        [TestMethod]
        public void TestEmptyValueIssue633()
        {
            var key = "01".HexToBytes();
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put(key, Array.Empty<byte>());
            var val = mpt[key];
            Assert.IsNotNull(val);
            Assert.AreEqual(0, val.Length);
            var r = mpt.TryGetProof(key, out var proof);
            Assert.IsTrue(r);
            val = Trie.VerifyProof(mpt.Root.Hash, key, proof);
            Assert.IsNotNull(val);
            Assert.AreEqual(0, val.Length);
        }

        [TestMethod]
        public void TestFindWithFrom()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("aa".HexToBytes(), "02".HexToBytes());
            mpt.Put("aa10".HexToBytes(), "03".HexToBytes());
            mpt.Put("aa50".HexToBytes(), "04".HexToBytes());
            var r = mpt.Find("aa".HexToBytes()).ToList();
            Assert.AreEqual(3, r.Count);
            r = mpt.Find("aa".HexToBytes(), "aa30".HexToBytes()).ToList();
            Assert.AreEqual(1, r.Count);
            r = mpt.Find("aa".HexToBytes(), "aa60".HexToBytes()).ToList();
            Assert.AreEqual(0, r.Count);
            r = mpt.Find("aa".HexToBytes(), "aa10".HexToBytes()).ToList();
            Assert.AreEqual(1, r.Count);
        }

        [TestMethod]
        public void TestFindStatesIssue652()
        {
            var snapshot = new TestSnapshot();
            var mpt = new Trie(snapshot, null);
            mpt.Put("abc1".HexToBytes(), "01".HexToBytes());
            mpt.Put("abc3".HexToBytes(), "02".HexToBytes());
            var r = mpt.Find("ab".HexToBytes(), "abd2".HexToBytes()).ToList();
            Assert.AreEqual(0, r.Count);
            r = mpt.Find("ab".HexToBytes(), "abb2".HexToBytes()).ToList();
            Assert.AreEqual(2, r.Count);
            r = mpt.Find("ab".HexToBytes(), "abc2".HexToBytes()).ToList();
            Assert.AreEqual(1, r.Count);
        }
    }
}
