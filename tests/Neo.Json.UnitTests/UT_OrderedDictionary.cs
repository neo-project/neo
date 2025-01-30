// Copyright (C) 2015-2025 The Neo Project.
//
// UT_OrderedDictionary.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections;

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_OrderedDictionary
    {
        private OrderedDictionary<string, uint> od;

        [TestInitialize]
        public void SetUp()
        {
            od = new OrderedDictionary<string, uint>
            {
                { "a", 1 },
                { "b", 2 },
                { "c", 3 }
            };
        }
        [TestMethod]
        public void TestClear()
        {
            od.Clear();
            Assert.AreEqual(0, od.Count);
            Assert.IsFalse(od.TryGetValue("a", out uint i));
        }

        [TestMethod]
        public void TestCount()
        {
            Assert.AreEqual(3, od.Count);
            od.Add("d", 4);
            Assert.AreEqual(4, od.Count);
        }

        [TestMethod]
        public void TestIsReadOnly()
        {
            Assert.IsFalse(od.IsReadOnly);
        }

        [TestMethod]
        public void TestSetAndGetItem()
        {
            var val = od["a"];
            Assert.AreEqual(1u, val);
            od["d"] = 10;
            Assert.AreEqual(10u, od["d"]);
            od["d"] = 15;
            Assert.AreEqual(15u, od["d"]);
        }

        [TestMethod]
        public void TestGetKeys()
        {
            var keys = od.Keys;
            Assert.IsTrue(keys.Contains("a"));
            Assert.AreEqual(3, keys.Count);
        }

        [TestMethod]
        public void TestGetValues()
        {
            var values = od.Values;
            Assert.IsTrue(values.Contains(1u));
            Assert.AreEqual(3, values.Count);
        }

        [TestMethod]
        public void TestRemove()
        {
            od.Remove("a");
            Assert.AreEqual(2, od.Count);
            Assert.IsFalse(od.ContainsKey("a"));
        }

        [TestMethod]
        public void TestTryGetValue()
        {
            Assert.IsTrue(od.TryGetValue("a", out uint i));
            Assert.AreEqual(1u, i);
            Assert.IsFalse(od.TryGetValue("d", out uint j));
            Assert.AreEqual(0u, j);
        }

        [TestMethod]
        public void TestCollectionAddAndContains()
        {
            var pair = new KeyValuePair<string, uint>("d", 4);
            ICollection<KeyValuePair<string, uint>> collection = od;
            collection.Add(pair);
            Assert.IsTrue(collection.Contains(pair));
        }

        [TestMethod]
        public void TestCollectionCopyTo()
        {
            var arr = new KeyValuePair<string, uint>[3];
            ICollection<KeyValuePair<string, uint>> collection = od;
            collection.CopyTo(arr, 0);
            Assert.AreEqual("a", arr[0].Key);
            Assert.AreEqual(1u, arr[0].Value);
            Assert.AreEqual("b", arr[1].Key);
            Assert.AreEqual(2u, arr[1].Value);
            Assert.AreEqual("c", arr[2].Key);
            Assert.AreEqual(3u, arr[2].Value);
        }

        [TestMethod]
        public void TestCollectionRemove()
        {
            ICollection<KeyValuePair<string, uint>> collection = od;
            var pair = new KeyValuePair<string, uint>("a", 1);
            collection.Remove(pair);
            Assert.IsFalse(collection.Contains(pair));
            Assert.AreEqual(2, collection.Count);
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            IEnumerable collection = od;
            Assert.IsTrue(collection.GetEnumerator().MoveNext());
        }
    }
}
