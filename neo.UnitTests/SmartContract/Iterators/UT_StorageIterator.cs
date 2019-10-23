using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Iterators
{
    [TestClass]
    public class UT_StorageIterator
    {
        [TestMethod]
        public void TestGeneratorAndDispose()
        {
            StorageIterator storageIterator = new StorageIterator(new List<KeyValuePair<StorageKey, StorageItem>>().GetEnumerator());
            Assert.IsNotNull(storageIterator);
            Action action = () => storageIterator.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestKeyAndValueAndNext()
        {
            List<KeyValuePair<StorageKey, StorageItem>> list = new List<KeyValuePair<StorageKey, StorageItem>>();
            StorageKey storageKey = new StorageKey();
            storageKey.Key = new byte[1];
            StorageItem storageItem = new StorageItem();
            storageItem.Value = new byte[1];
            list.Add(new KeyValuePair<StorageKey, StorageItem>(storageKey, storageItem));
            StorageIterator storageIterator = new StorageIterator(list.GetEnumerator());
            storageIterator.Next();
            Assert.AreEqual(new ByteArray(new byte[1]), storageIterator.Key());
            Assert.AreEqual(new ByteArray(new byte[1]), storageIterator.Value());
        }
    }
}
