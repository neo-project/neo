// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ByteExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
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
            Assert.ThrowsException<ArgumentNullException>(nullStr.ToHexString);
            Assert.ThrowsException<ArgumentNullException>(() => nullStr.ToHexString(false));
            Assert.ThrowsException<ArgumentNullException>(() => nullStr.ToHexString(true));

            byte[] empty = Array.Empty<byte>();
            Assert.AreEqual("", empty.ToHexString());
            Assert.AreEqual("", empty.ToHexString(false));
            Assert.AreEqual("", empty.ToHexString(true));

            byte[] str1 = new byte[] { (byte)'n', (byte)'e', (byte)'o' };
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
            Assert.IsTrue(result.Length == 0);

            input = new byte[] { 0x5A };
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            Assert.AreEqual("5a", result);

            input = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            Assert.AreEqual("0123456789abcdef", result);
        }
    }
}
