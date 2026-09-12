// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BinaryWriterExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using System;
using System.IO;

namespace Neo.UnitTests.Extensions
{
    /// <summary>
    /// BinaryWriterExtensions edges not fully covered by UT_IOHelper.
    /// </summary>
    [TestClass]
    public class UT_BinaryWriterExtensions
    {
        [TestMethod]
        public void Write_ISerializable_And_Collection()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
            {
                writer.Write(UInt160.Zero);
                writer.Write<UInt160>([UInt160.Zero, UInt160.Parse("0x0000000000000000000000000000000000000001")]);
            }

            var reader = new Neo.IO.MemoryReader(ms.ToArray());
            Assert.AreEqual(UInt160.Zero, reader.ReadSerializable<UInt160>());
            var array = reader.ReadSerializableArray<UInt160>();
            Assert.HasCount(2, array);
            Assert.AreEqual(UInt160.Zero, array[0]);
        }

        [TestMethod]
        public void Write_Collection_Null_Throws()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            Assert.ThrowsExactly<ArgumentNullException>(() => writer.Write<UInt160>(null!));
        }

        [TestMethod]
        public void WriteVarString_RoundTrip()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                writer.WriteVarString("neo-test");

            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            var bytes = reader.ReadVarBytes();
            Assert.AreEqual("neo-test", System.Text.Encoding.UTF8.GetString(bytes));
        }

        [TestMethod]
        public void WriteNullableArray_WithNulls()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                writer.WriteNullableArray([UInt256.Zero, null, UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000002")]);

            var mem = new Neo.IO.MemoryReader(ms.ToArray());
            var array = mem.ReadNullableArray<UInt256>();
            Assert.HasCount(3, array);
            Assert.AreEqual(UInt256.Zero, array[0]);
            Assert.IsNull(array[1]);
            Assert.IsNotNull(array[2]);
        }
    }
}
