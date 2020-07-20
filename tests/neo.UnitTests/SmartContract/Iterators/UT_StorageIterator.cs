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
            StorageIterator storageIterator = new StorageIterator(new List<(StorageKey, StorageItem)>().GetEnumerator());
            Assert.IsNotNull(storageIterator);
            Action action = () => storageIterator.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestKeyAndValueAndNext()
        {
            List<(StorageKey, StorageItem)> list = new List<(StorageKey, StorageItem)>();
            StorageKey storageKey = new StorageKey();
            storageKey.Key = new byte[1];
            StorageItem storageItem = new StorageItem();
            storageItem.Value = new byte[1];
            list.Add((storageKey, storageItem));
            StorageIterator storageIterator = new StorageIterator(list.GetEnumerator());
            storageIterator.Next();
            Assert.AreEqual(new ByteString(new byte[1]), storageIterator.Key());
            Assert.AreEqual(new ByteString(new byte[1]), storageIterator.Value());
        }
    }
}
