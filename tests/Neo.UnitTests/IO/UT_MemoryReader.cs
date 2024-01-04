// Copyright (C) 2015-2024 The Neo Project.
//
// UT_MemoryReader.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using System.IO;
using System.Text;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_MemoryReader
    {
        [TestMethod]
        public void TestReadFixedString()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.WriteFixedString("AA", Encoding.UTF8.GetBytes("AA").Length + 1);
            MemoryReader reader = new(stream.ToArray());
            string result = reader.ReadFixedString(Encoding.UTF8.GetBytes("AA").Length + 1);
            Assert.AreEqual("AA", result);
        }

        [TestMethod]
        public void TestReadVarString()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.WriteVarString("AAAAAAA");
            MemoryReader reader = new(stream.ToArray());
            string result = reader.ReadVarString(10);
            Assert.AreEqual("AAAAAAA", result);
        }

        [TestMethod]
        public void TestReadNullableArray()
        {
            byte[] bs = "0400000000".FromHexString();
            MemoryReader reader = new(bs);
            var n = reader.ReadNullableArray<UInt256>();
            Assert.AreEqual(5, reader.Position);
        }
    }
}
