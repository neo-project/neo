// Copyright (C) 2015-2024 The Neo Project.
//
// LeveldbStoreTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Neo.Plugins.Storage.Tests
{
    using FluentAssertions;
    using Neo.IO.Storage.LevelDB;

    [TestClass]
    public class LevelDbStoreTests
    {
        private static Options _options = new() { CreateIfMissing = true };

        [TestMethod]
        public void Test_Database_Put_And_Get_Key()
        {
            byte[] expectedKey = [0x01, 0x02, 0x03, 0x04];
            byte[] expectedValue = [0x01];

            using var db = new DB(CreateTempDirectory(), _options);
            db.Put(expectedKey, expectedValue);

            var actualValue = db.Get(expectedKey);

            Assert.IsNotNull(actualValue);
            CollectionAssert.AreEqual(expectedValue, actualValue);
        }

        [TestMethod]
        public void Test_Iterator_SeekToFirst()
        {
            byte[] expectedKey = [0x01, 0x02, 0x03, 0x04];
            byte[] expectedValue = [0x01];

            using var db = new DB(CreateTempDirectory(), _options);
            db.Put(expectedKey, expectedValue);

            using var iter = db.CreateIterator();
            iter.SeekToFirst();

            Assert.IsTrue(iter.IsValid());
            CollectionAssert.AreEqual(expectedKey, iter.Key());
            CollectionAssert.AreEqual(expectedValue, iter.Value());
        }

        [TestMethod]
        public void Test_Iterator_SeekToLast()
        {
            byte[] expectedKey = [0x01, 0x02, 0x03, 0x04];
            byte[] expectedValue = [0x01];

            using var db = new DB(CreateTempDirectory(), _options);
            db.Put([], []);
            db.Put([0x01], [0x01]);
            db.Put(expectedKey, expectedValue);

            using var iter = db.CreateIterator();
            iter.SeekToLast();

            Assert.IsTrue(iter.IsValid());
            CollectionAssert.AreEqual(expectedKey, iter.Key());
            CollectionAssert.AreEqual(expectedValue, iter.Value());
        }

        string CreateTempDirectory()
        {
            var filename = Path.GetTempFileName();
            File.Delete(filename);
            return Directory.CreateDirectory(filename).FullName;
        }
    }
}
