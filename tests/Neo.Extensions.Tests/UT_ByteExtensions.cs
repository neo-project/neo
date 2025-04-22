// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ByteExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO.Hashing;
using System.Linq;
using System.Text;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_ByteExtensions
    {
        [TestMethod]
        public void TestToHexString()
        {
            byte[]? nullStr = null;
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullStr.ToHexString());
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullStr.ToHexString(false));
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullStr.ToHexString(true));

            byte[] empty = Array.Empty<byte>();
            Assert.AreEqual("", empty.ToHexString());
            Assert.AreEqual("", empty.ToHexString(false));
            Assert.AreEqual("", empty.ToHexString(true));

            byte[] str1 = [(byte)'n', (byte)'e', (byte)'o'];
            Assert.AreEqual("6e656f", str1.ToHexString());
            Assert.AreEqual("6e656f", str1.ToHexString(false));
            Assert.AreEqual("6f656e", str1.ToHexString(true));
        }

        [TestMethod]
        public void TestXxHash3()
        {
            byte[] data = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("Hello, World!^_^", 16 * 1024)));
            Assert.AreEqual(HashCode.Combine(XxHash3.HashToUInt64(data, 40343)), data.XxHash3_32());
        }

        [TestMethod]
        public void TestReadOnlySpanToHexString()
        {
            byte[] input = { 0x0F, 0xA4, 0x3B };
            var span = new ReadOnlySpan<byte>(input);
            string result = span.ToHexString();
            Assert.AreEqual("0fa43b", result);

            input = Array.Empty<byte>();
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            Assert.AreEqual(0, result.Length);

            input = [0x5A];
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            Assert.AreEqual("5a", result);

            input = [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF];
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            Assert.AreEqual("0123456789abcdef", result);
        }

        [TestMethod]
        public void TestNotZero()
        {
            Assert.IsFalse(new ReadOnlySpan<byte>(Array.Empty<byte>()).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[4]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[7]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[8]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[9]).NotZero());
            Assert.IsFalse(new ReadOnlySpan<byte>(new byte[11]).NotZero());

            Assert.IsTrue(new ReadOnlySpan<byte>([0x00, 0x00, 0x00, 0x01]).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01]).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01]).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00]).NotZero());
            Assert.IsTrue(new ReadOnlySpan<byte>([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01]).NotZero());

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
