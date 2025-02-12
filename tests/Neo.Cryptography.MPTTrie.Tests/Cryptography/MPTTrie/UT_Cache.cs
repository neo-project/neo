// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Cache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Persistence.Providers;
using System.Text;

namespace Neo.Cryptography.MPTTrie.Tests
{

    [TestClass]
    public class UT_Cache
    {
        private readonly byte Prefix = 0xf0;

        [TestMethod]
        public void TestResolveLeaf()
        {
            var n = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var store = new MemoryStore();
            store.Put(n.Hash.ToKey(), n.ToArray());
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            var resolved = cache.Resolve(n.Hash);
            Assert.AreEqual(n.Hash, resolved.Hash);
            Assert.AreEqual(n.Value.Span.ToHexString(), resolved.Value.Span.ToHexString());
        }

        [TestMethod]
        public void TestResolveBranch()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var b = Node.NewBranch();
            b.Children[1] = l;
            var store = new MemoryStore();
            store.Put(b.Hash.ToKey(), b.ToArray());
            store.Put(l.Hash.ToKey(), l.ToArray());
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            var resolved_b = cache.Resolve(b.Hash);
            Assert.AreEqual(b.Hash, resolved_b.Hash);
            Assert.AreEqual(l.Hash, resolved_b.Children[1].Hash);
            var resolved_l = cache.Resolve(l.Hash);
            Assert.AreEqual(l.Value.Span.ToHexString(), resolved_l.Value.Span.ToHexString());
        }

        [TestMethod]
        public void TestResolveExtension()
        {
            var e = Node.NewExtension(new byte[] { 0x01 }, new Node());
            var store = new MemoryStore();
            store.Put(e.Hash.ToKey(), e.ToArray());
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            var re = cache.Resolve(e.Hash);
            Assert.AreEqual(e.Hash, re.Hash);
            Assert.AreEqual(e.Key.Span.ToHexString(), re.Key.Span.ToHexString());
            Assert.IsTrue(re.Next.IsEmpty);
        }

        [TestMethod]
        public void TestGetAndChangedBranch()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var b = Node.NewBranch();
            var store = new MemoryStore();
            store.Put(b.Hash.ToKey(), b.ToArray());
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            var resolved_b = cache.Resolve(b.Hash);
            Assert.AreEqual(resolved_b.Hash, b.Hash);
            foreach (var n in resolved_b.Children)
            {
                Assert.IsTrue(n.IsEmpty);
            }
            resolved_b.Children[1] = l;
            resolved_b.SetDirty();
            var resovled_b1 = cache.Resolve(b.Hash);
            Assert.AreEqual(resovled_b1.Hash, b.Hash);
            foreach (var n in resovled_b1.Children)
            {
                Assert.IsTrue(n.IsEmpty);
            }
        }

        [TestMethod]
        public void TestGetAndChangedExtension()
        {
            var e = Node.NewExtension(new byte[] { 0x01 }, new Node());
            var store = new MemoryStore();
            store.Put(e.Hash.ToKey(), e.ToArray());
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            var re = cache.Resolve(e.Hash);
            Assert.AreEqual(e.Hash, re.Hash);
            Assert.AreEqual(e.Key.Span.ToHexString(), re.Key.Span.ToHexString());
            Assert.IsTrue(re.Next.IsEmpty);
            re.Key = new byte[] { 0x02 };
            re.SetDirty();
            var re1 = cache.Resolve(e.Hash);
            Assert.AreEqual(e.Hash, re1.Hash);
            Assert.AreEqual(e.Key.Span.ToHexString(), re1.Key.Span.ToHexString());
            Assert.IsTrue(re1.Next.IsEmpty);
        }

        [TestMethod]
        public void TestGetAndChangedLeaf()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var store = new MemoryStore();
            store.Put(l.Hash.ToKey(), l.ToArray());
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            var rl = cache.Resolve(l.Hash);
            Assert.AreEqual(l.Hash, rl.Hash);
            Assert.AreEqual("leaf", Encoding.ASCII.GetString(rl.Value.Span));
            rl.Value = new byte[] { 0x01 };
            rl.SetDirty();
            var rl1 = cache.Resolve(l.Hash);
            Assert.AreEqual(l.Hash, rl1.Hash);
            Assert.AreEqual("leaf", Encoding.ASCII.GetString(rl1.Value.Span));
        }

        [TestMethod]
        public void TestPutAndChangedBranch()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var b = Node.NewBranch();
            var h = b.Hash;
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            cache.PutNode(b);
            var rb = cache.Resolve(h);
            Assert.AreEqual(h, rb.Hash);
            foreach (var n in rb.Children)
            {
                Assert.IsTrue(n.IsEmpty);
            }
            rb.Children[1] = l;
            rb.SetDirty();
            var rb1 = cache.Resolve(h);
            Assert.AreEqual(h, rb1.Hash);
            foreach (var n in rb1.Children)
            {
                Assert.IsTrue(n.IsEmpty);
            }
        }

        [TestMethod]
        public void TestPutAndChangedExtension()
        {
            var e = Node.NewExtension(new byte[] { 0x01 }, new Node());
            var h = e.Hash;
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            cache.PutNode(e);
            var re = cache.Resolve(e.Hash);
            Assert.AreEqual(e.Hash, re.Hash);
            Assert.AreEqual(e.Key.Span.ToHexString(), re.Key.Span.ToHexString());
            Assert.IsTrue(re.Next.IsEmpty);
            e.Key = new byte[] { 0x02 };
            e.Next = e;
            e.SetDirty();
            var re1 = cache.Resolve(h);
            Assert.AreEqual(h, re1.Hash);
            Assert.AreEqual("01", re1.Key.Span.ToHexString());
            Assert.IsTrue(re1.Next.IsEmpty);
        }

        [TestMethod]
        public void TestPutAndChangedLeaf()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var h = l.Hash;
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            cache.PutNode(l);
            var rl = cache.Resolve(l.Hash);
            Assert.AreEqual(h, rl.Hash);
            Assert.AreEqual("leaf", Encoding.ASCII.GetString(rl.Value.Span));
            l.Value = new byte[] { 0x01 };
            l.SetDirty();
            var rl1 = cache.Resolve(h);
            Assert.AreEqual(h, rl1.Hash);
            Assert.AreEqual("leaf", Encoding.ASCII.GetString(rl1.Value.Span));
        }

        [TestMethod]
        public void TestReference1()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            cache.PutNode(l);
            cache.Commit();
            snapshot.Commit();
            var snapshot1 = store.GetSnapshot();
            var cache1 = new Cache(snapshot1, Prefix);
            cache1.PutNode(l);
            cache1.Commit();
            snapshot1.Commit();
            var snapshot2 = store.GetSnapshot();
            var cache2 = new Cache(snapshot2, Prefix);
            var rl = cache2.Resolve(l.Hash);
            Assert.AreEqual(2, rl.Reference);
        }

        [TestMethod]
        public void TestReference2()
        {
            var l = Node.NewLeaf(Encoding.ASCII.GetBytes("leaf"));
            var store = new MemoryStore();
            var snapshot = store.GetSnapshot();
            var cache = new Cache(snapshot, Prefix);
            cache.PutNode(l);
            cache.PutNode(l);
            cache.DeleteNode(l.Hash);
            var rl = cache.Resolve(l.Hash);
            Assert.AreEqual(1, rl.Reference);
        }
    }
}
