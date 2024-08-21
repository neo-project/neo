// Copyright (C) 2015-2024 The Neo Project.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_KeyedCollectionSlim
    {
        [TestMethod]
        public void Add_ShouldAddItem()
        {
            // Arrange
            var collection = new TestKeyedCollectionSlim();
            var item = new TestItem { Id = 1, Name = "Item1" };

            // Act
            collection.Add(item);

            // Assert
            Assert.AreEqual(1, collection.Count);
            Assert.IsTrue(collection.Contains(1));
            Assert.AreEqual(item, collection.First);
        }

        [TestMethod]
        public void Add_ShouldThrowException_WhenKeyAlreadyExists()
        {
            // Arrange
            var collection = new TestKeyedCollectionSlim();
            var item1 = new TestItem { Id = 1, Name = "Item1" };
            var item2 = new TestItem { Id = 1, Name = "Item2" }; // Same ID as item1

            // Act
            collection.Add(item1);

            // Assert
            Assert.ThrowsException<ArgumentException>(() => collection.Add(item2));
        }

        [TestMethod]
        public void Remove_ShouldRemoveItem()
        {
            // Arrange
            var collection = new TestKeyedCollectionSlim();
            var item = new TestItem { Id = 1, Name = "Item1" };
            collection.Add(item);

            // Act
            collection.Remove(1);

            // Assert
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
            collection.Add(item1);
            collection.Add(item2);

            // Act
            collection.RemoveFirst();

            // Assert
            Assert.AreEqual(1, collection.Count);
            Assert.IsFalse(collection.Contains(1));
            Assert.IsTrue(collection.Contains(2));
        }

        public class TestItem : IStructuralEquatable, IStructuralComparable, IComparable
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public int CompareTo(object? obj)
            {
                if (obj is not TestItem other) throw new ArgumentException("Object is not a TestItem");
                return Id.CompareTo(other.Id);
            }

            public bool Equals(object? other, IEqualityComparer comparer)
            {
                return other is TestItem item && Id == item.Id && Name == item.Name;
            }

            public int GetHashCode(IEqualityComparer comparer)
            {
                return HashCode.Combine(Id, Name);
            }

            public int CompareTo(TestItem other)
            {
                return Id.CompareTo(other.Id);
            }

            public int CompareTo(object other, IComparer comparer)
            {
                throw new NotImplementedException();
            }
        }

        internal class TestKeyedCollectionSlim : KeyedCollectionSlim<int, TestItem>
        {
            protected override int GetKeyForItem(TestItem? item)
            {
                return item?.Id ?? throw new ArgumentNullException(nameof(item), "Item cannot be null");
            }
        }
    }
}
