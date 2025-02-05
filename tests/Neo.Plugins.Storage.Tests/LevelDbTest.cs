// Copyright (C) 2015-2025 The Neo Project.
//
// LevelDbTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Storage.LevelDB;
using System.IO;

namespace Neo.Plugins.Storage.Tests
{
    [TestClass]
    public class LevelDbTest
    {
        [TestMethod]
        public void TestLevelDbDatabase()
        {
            using var db = DB.Open(Path.GetRandomFileName(), new() { CreateIfMissing = true });

            db.Put(WriteOptions.Default, [0x00, 0x00, 0x01], [0x01]);
            db.Put(WriteOptions.Default, [0x00, 0x00, 0x02], [0x02]);
            db.Put(WriteOptions.Default, [0x00, 0x00, 0x03], [0x03]);

            CollectionAssert.AreEqual(new byte[] { 0x01, }, db.Get(ReadOptions.Default, [0x00, 0x00, 0x01]));
            CollectionAssert.AreEqual(new byte[] { 0x02, }, db.Get(ReadOptions.Default, [0x00, 0x00, 0x02]));
            CollectionAssert.AreEqual(new byte[] { 0x03, }, db.Get(ReadOptions.Default, [0x00, 0x00, 0x03]));
        }
    }
}
