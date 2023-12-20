// Copyright (C) 2015-2024 The Neo Project.
//
// UT_StorageIterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
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
            StorageIterator storageIterator = new(new List<(StorageKey, StorageItem)>().GetEnumerator(), 0, FindOptions.None);
            Assert.IsNotNull(storageIterator);
            Action action = () => storageIterator.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestKeyAndValueAndNext()
        {
            List<(StorageKey, StorageItem)> list = new();
            StorageKey storageKey = new()
            {
                Key = new byte[1]
            };
            StorageItem storageItem = new()
            {
                Value = new byte[1]
            };
            list.Add((storageKey, storageItem));
            StorageIterator storageIterator = new(list.GetEnumerator(), 0, FindOptions.ValuesOnly);
            storageIterator.Next();
            Assert.AreEqual(new ByteString(new byte[1]), storageIterator.Value(null));
        }
    }
}
