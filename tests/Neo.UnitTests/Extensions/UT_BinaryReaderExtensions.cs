// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BinaryReaderExtensions.cs file belongs to the neo project and is free
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
    [TestClass]
    public class UT_BinaryReaderExtensions
    {
        [TestMethod]
        public void ReadFixedBytes_ExactSize()
        {
            using var ms = new MemoryStream([1, 2, 3, 4]);
            using var reader = new BinaryReader(ms);
            var data = reader.ReadFixedBytes(4);
            Assert.IsTrue(new byte[] { 1, 2, 3, 4 }.AsSpan().SequenceEqual(data));
        }

        [TestMethod]
        public void ReadFixedBytes_TruncatedStream_Throws()
        {
            using var ms = new MemoryStream([1, 2]);
            using var reader = new BinaryReader(ms);
            Assert.ThrowsExactly<FormatException>(() => reader.ReadFixedBytes(4));
        }

        [TestMethod]
        public void ReadVarInt_Encodings()
        {
            static ulong Read(params byte[] data)
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms);
                return reader.ReadVarInt();
            }

            Assert.AreEqual(0xFCul, Read(0xFC));
            Assert.AreEqual(0x0100ul, Read(0xFD, 0x00, 0x01));
            Assert.AreEqual(0x10000ul, Read(0xFE, 0x00, 0x00, 0x01, 0x00));
        }

        [TestMethod]
        public void ReadVarInt_ExceedsMax_Throws()
        {
            using var ms = new MemoryStream([0x10]);
            using var reader = new BinaryReader(ms);
            Assert.ThrowsExactly<FormatException>(() => reader.ReadVarInt(max: 0x0F));
        }

        [TestMethod]
        public void ReadVarBytes_RoundTrip()
        {
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                writer.WriteVarBytes([9, 8, 7]);
            ms.Position = 0;
            using var reader = new BinaryReader(ms);
            Assert.IsTrue(new byte[] { 9, 8, 7 }.AsSpan().SequenceEqual(reader.ReadVarBytes()));
        }
    }
}
