// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Storage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Storage
    {
        [TestMethod]
        public void TestStorageKey()
        {
            // Test data
            byte[] keyData = [0x00, 0x00, 0x00, 0x00, 0x12];
            var keyMemory = new ReadOnlyMemory<byte>(keyData);

            // Test implicit conversion from byte[] to StorageKey
            StorageKey storageKeyFromArray = keyData;
            Assert.AreEqual(0, storageKeyFromArray.Id);
            Assert.IsTrue(keyMemory.Span.ToArray().Skip(sizeof(int)).SequenceEqual(storageKeyFromArray.Key.Span.ToArray()));

            // Test implicit conversion from ReadOnlyMemory<byte> to StorageKey
            StorageKey storageKeyFromMemory = keyMemory;
            Assert.AreEqual(0, storageKeyFromMemory.Id);
            Assert.IsTrue(keyMemory.Span.ToArray().Skip(sizeof(int)).SequenceEqual(storageKeyFromMemory.Key.Span.ToArray()));

            // Test CreateSearchPrefix method
            byte[] prefix = { 0xAA };
            var searchPrefix = StorageKey.CreateSearchPrefix(0, prefix);
            var expectedPrefix = BitConverter.GetBytes(0).Concat(prefix).ToArray();
            Assert.IsTrue(expectedPrefix.SequenceEqual(searchPrefix));

            // Test Equals method
            var storageKey1 = new StorageKey { Id = 0, Key = keyMemory };
            var storageKey2 = new StorageKey { Id = 0, Key = keyMemory };
            var storageKeyDifferentId = new StorageKey { Id = 0 + 1, Key = keyMemory };
            var storageKeyDifferentKey = new StorageKey { Id = 0, Key = new ReadOnlyMemory<byte>([0x04]) };
            Assert.AreEqual(storageKey1, storageKey2);
            Assert.AreNotEqual(storageKey1, storageKeyDifferentId);
            Assert.AreNotEqual(storageKey1, storageKeyDifferentKey);
        }

        [TestMethod]
        public void TestStorageItem()
        {
            // Test data
            byte[] keyData = [0x00, 0x00, 0x00, 0x00, 0x12];
            BigInteger bigInteger = new BigInteger(1234567890);

            // Test implicit conversion from byte[] to StorageItem
            StorageItem storageItemFromArray = keyData;
            Assert.IsTrue(keyData.SequenceEqual(storageItemFromArray.Value.Span.ToArray()));

            // Test implicit conversion from BigInteger to StorageItem
            StorageItem storageItemFromBigInteger = bigInteger;
            Assert.AreEqual(bigInteger, (BigInteger)storageItemFromBigInteger);
        }
    }
}
