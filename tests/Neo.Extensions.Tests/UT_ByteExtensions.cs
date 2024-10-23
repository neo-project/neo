// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ByteExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


using FluentAssertions;
using System;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_ByteExtensions
    {
        [TestMethod]
        public void TestToHexString()
        {
            byte[] nullStr = null;
            Assert.ThrowsException<NullReferenceException>(() => nullStr.ToHexString());
            byte[] empty = Array.Empty<byte>();
            empty.ToHexString().Should().Be("");
            empty.ToHexString(false).Should().Be("");
            empty.ToHexString(true).Should().Be("");

            byte[] str1 = new byte[] { (byte)'n', (byte)'e', (byte)'o' };
            str1.ToHexString().Should().Be("6e656f");
            str1.ToHexString(false).Should().Be("6e656f");
            str1.ToHexString(true).Should().Be("6f656e");
        }

        [TestMethod]
        public void TestReadOnlySpanToHexString()
        {
            byte[] input = { 0x0F, 0xA4, 0x3B };
            var span = new ReadOnlySpan<byte>(input);
            string result = span.ToHexString();
            result.Should().Be("0fa43b");

            input = Array.Empty<byte>();
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            result.Should().BeEmpty();

            input = new byte[] { 0x5A };
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            result.Should().Be("5a");

            input = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
            span = new ReadOnlySpan<byte>(input);
            result = span.ToHexString();
            result.Should().Be("0123456789abcdef");
        }
    }
}
