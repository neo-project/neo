// Copyright (C) 2015-2025 The Neo Project.
//
// UT_KeyedCollectionSlim.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_KeyedCollectionSlim
    {
        [TestMethod]
        public void Add_ShouldAddItem()
        {
            var collection = new TestKeyedCollectionSlim();
            var item = new TestItem { Id = 1, Name = "Item1" };
            var ok = collection.TryAdd(item);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, collection.Count);
            Assert.IsTrue(collection.Contains(1));
            Assert.AreEqual(item, collection.FirstOrDefault);
        }

        [TestMethod]
        public void AddTest()
        {
            // Arrange
            var collection = new TestKeyedCollectionSlim();
            var item1 = new TestItem { Id = 1, Name = "Item1" };
            var item2 = new TestItem { Id = 1, Name = "Item2" }; // Same ID as item1

            var ok = collection.TryAdd(item1);
            Assert.IsTrue(ok);

            ok = collection.TryAdd(item2);
            Assert.IsFalse(ok);

            collection.Clear();
            Assert.AreEqual(0, collection.Count);
            Assert.IsNull(collection.FirstOrDefault);
        }

        [TestMethod]
        public void Remove_ShouldRemoveItem()
        {
            // Arrange
            var collection = new TestKeyedCollectionSlim();
            var item = new TestItem { Id = 1, Name = "Item1" };
            collection.TryAdd(item);

            // Act
            var ok = collection.Remove(1);

            // Assert
            Assert.IsTrue(ok);
            Assert.AreEqual(0, collection.Count);
            Assert.IsFalse(collection.Contains(1));
        }

        [TestMethod]
        public void RemoveFirst_ShouldRemoveFirstItem()
        {
            // Arrange
            var collection = new TestKeyedCollectionSlim();
            var item1 = new TestItem { Id = 1, Name = "Item1" };
            var item2 = new TestItem { Id = 2, Name = "Item2" };
            collection.TryAdd(item1);
            collection.TryAdd(item2);

            // Act
            Assert.IsTrue(collection.RemoveFirst());

            // Assert
            Assert.AreEqual(1, collection.Count);
            Assert.IsFalse(collection.Contains(1));
            Assert.IsTrue(collection.Contains(2));
        }

        public class TestItem
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public override int GetHashCode() => Id;

            public override bool Equals(object obj) => obj is TestItem item && Id == item.Id;
        }

        internal class TestKeyedCollectionSlim : KeyedCollectionSlim<int, TestItem>
        {
            protected override int GetKeyForItem(TestItem item)
            {
                return item?.Id ?? throw new ArgumentNullException(nameof(item), "Item cannot be null");
            }
        }
    }
}
