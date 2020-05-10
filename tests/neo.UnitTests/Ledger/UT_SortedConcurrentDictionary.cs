using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_SortedConcurrentDictionary
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestDictionary()
        {
            var dic = new SortedConcurrentDictionary<int, string>(Comparer<KeyValuePair<int, string>>.Create(Sort), 3);

            Assert.AreEqual(0, dic.Count);
            Assert.AreEqual(3, dic.Capacity);

            Assert.IsTrue(dic.TryAdd(3, "3"));
            Assert.IsTrue(dic.TryAdd(2, "2"));
            Assert.IsTrue(dic.TryAdd(1, "1"));

            dic.Set(3, "3.");
            dic.Set(2, "2.");
            dic.Set(1, "1.");

            var en = dic.GetEnumerator();

            for (int x = 0; x < 3; x++)
            {
                Assert.IsTrue(en.MoveNext());
                Assert.AreEqual(x + 1, en.Current.Key);
                Assert.AreEqual((x + 1).ToString() + ".", en.Current.Value);
            }

            Assert.IsTrue(dic.TryGetValue(3, out var v) && v == "3.");

            Assert.AreEqual(3, dic.Count);
            Assert.IsTrue(dic.TryAdd(0, "0."));
            Assert.AreEqual(3, dic.Count);

            Assert.IsFalse(dic.TryGetValue(3, out v));

            en = dic.GetEnumerator();

            for (int x = 0; x < 3; x++)
            {
                Assert.IsTrue(en.MoveNext());
                Assert.AreEqual(x, en.Current.Key);
                Assert.AreEqual((x).ToString() + ".", en.Current.Value);
            }

            Assert.IsTrue(dic.TryRemove(0, out v));
            Assert.IsFalse(dic.TryRemove(0, out v));
            Assert.AreEqual(2, dic.Count);

            Assert.IsTrue(dic.TryPop(out v) && v == "1.");
            Assert.AreEqual(1, dic.Count);

            dic.Clear();

            Assert.AreEqual(0, dic.Count);
            Assert.AreEqual(3, dic.Capacity);
        }

        private int Sort(KeyValuePair<int, string> x, KeyValuePair<int, string> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}
