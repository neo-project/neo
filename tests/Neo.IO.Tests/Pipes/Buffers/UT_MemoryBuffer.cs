// Copyright (C) 2015-2024 The Neo Project.
//
// UT_MemoryBuffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Pipes.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.IO.Tests.Pipes.Buffers
{
    [TestClass]
    public class UT_MemoryBuffer
    {
        [TestMethod]
        public void ReadWrite_Integers()
        {
            using var mb = new MemoryBuffer();
            mb.Write<byte>(1);
            mb.Write<sbyte>(2);
            mb.Write<short>(3);
            mb.Write<ushort>(4);
            mb.Write(5);
            mb.Write(6u);
            mb.Write(7L);
            mb.Write(8UL);

            var expectedBytes = new byte[]
            {
                0x01,
                0x02,
                0x03, 0x00,
                0x04, 0x00,
                0x05, 0x00, 0x00, 0x00,
                0x06, 0x00, 0x00, 0x00,
                0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            };

            var actualBytes = mb.ToArray();
            using var actualBuffer = new MemoryBuffer(actualBytes);

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(1, actualBuffer.Read<byte>());
            Assert.AreEqual(2, actualBuffer.Read<sbyte>());
            Assert.AreEqual(3, actualBuffer.Read<short>());
            Assert.AreEqual(4, actualBuffer.Read<ushort>());
            Assert.AreEqual(5, actualBuffer.Read<int>());
            Assert.AreEqual(6u, actualBuffer.Read<uint>());
            Assert.AreEqual(7L, actualBuffer.Read<long>());
            Assert.AreEqual(8UL, actualBuffer.Read<ulong>());
        }

        [TestMethod]
        public void ReadWrite_String()
        {
            using var mb = new MemoryBuffer();
            mb.WriteString("Hello, World!");

            var expectedString = "Hello, World!";
            byte[] expectedBytes =
            [
                .. BitConverter.GetBytes(Encoding.UTF8.GetByteCount(expectedString)),
                .. Encoding.UTF8.GetBytes(expectedString)
            ];
            var actualBytes = mb.ToArray();
            using var actualBuffer = new MemoryBuffer(actualBytes);

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(expectedString, actualBuffer.ReadString());
        }

        [TestMethod]
        public void ReadWrite_Array()
        {
            using var mb = new MemoryBuffer();
            mb.WriteArray(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });

            var expectedArray = new byte[]
            {
                0x05, 0x00, 0x00, 0x00,
                0x01,
                0x02,
                0x03,
                0x04,
                0x05
            };
            var actualArray = mb.ToArray();
            using var actualBuffer = new MemoryBuffer(actualArray);

            CollectionAssert.AreEqual(expectedArray, actualArray);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, actualBuffer.ReadArray<byte>());
        }
    }
}
