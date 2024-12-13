// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Unsafe.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using System;

namespace Neo.Test
{
    [TestClass]
    public class UT_Unsafe
    {
        [TestMethod]
        public void TestNotZero()
        {
            Assert.IsFalse(new ReadOnlySpan<byte>(System.Array.Empty<byte>()).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[4]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[7]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[8]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[9]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[11]).NotZero());

            Assert.IsTrue(new ReadOnlySpan<byte>(new byte[4] { 0x00, 0x00, 0x00, 0x01 }).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>(new byte[7] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>(new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>(new byte[9] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 }).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>(new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }).NotZero());

            var bytes = new byte[64];
            for (int i = 0; i < bytes.Length; i++)
            {
                ReadOnlySpan<byte> span = bytes.AsSpan();
                Assert.IsFalse(span[i..].NotZero());

                for (int j = i; j < bytes.Length; j++)
                {
                    bytes[j] = 0x01;
                    Assert.IsTrue(span[i..].NotZero());
                    bytes[j] = 0x00;
                }
            }
        }
    }
}
