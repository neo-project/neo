// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MemoryReaderExtensions.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.Extensions
{
    [TestClass]
    public class UT_MemoryReaderExtensions
    {
        [TestMethod]
        public void ReadSerializableArray_Empty()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                writer.WriteVarInt(0);
            var reader = new MemoryReader(ms.ToArray());
            var array = reader.ReadSerializableArray<UInt160>();
            Assert.IsEmpty(array);
        }

        [TestMethod]
        public void ReadSerializableArray_TwoHashes()
        {
            UInt160[] hashes =
            [
                UInt160.Zero,
                UInt160.Parse("0x0000000000000000000000000000000000000001")
            ];
            var bytes = hashes.ToByteArray();
            var reader = new MemoryReader(bytes);
            var clone = reader.ReadSerializableArray<UInt160>();
            Assert.HasCount(2, clone);
            Assert.AreEqual(hashes[0], clone[0]);
            Assert.AreEqual(hashes[1], clone[1]);
        }

        [TestMethod]
        public void ReadNullableArray_WithNullEntry()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
            {
                writer.WriteVarInt(2);
                writer.Write(true);
                writer.Write(UInt256.Zero);
                writer.Write(false);
            }
            var reader = new MemoryReader(ms.ToArray());
            var array = reader.ReadNullableArray<UInt256>();
            Assert.HasCount(2, array);
            Assert.AreEqual(UInt256.Zero, array[0]);
            Assert.IsNull(array[1]);
        }

        [TestMethod]
        public void ReadSerializable_Single()
        {
            var hash = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314");
            var reader = new MemoryReader(hash.ToArray());
            Assert.AreEqual(hash, reader.ReadSerializable<UInt160>());
        }
    }
}
