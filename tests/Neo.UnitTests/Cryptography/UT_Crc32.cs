// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Crc32.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Crc32
    {
        private const string SimpleString = @"The quick brown fox jumps over the lazy dog.";
        private readonly byte[] _simpleBytesAscii = Encoding.ASCII.GetBytes(SimpleString);

        private const string SimpleString2 = @"Life moves pretty fast. If you don't stop and look around once in a while, you could miss it.";
        private readonly byte[] _simpleBytes2Ascii = Encoding.ASCII.GetBytes(SimpleString2);

        [TestMethod]
        public void StaticDefaultSeedAndPolynomialWithShortAsciiString()
        {
            var actual = Crc32.Compute(_simpleBytesAscii);

            actual.Should().Be(0x519025e9U);
        }

        [TestMethod]
        public void StaticDefaultSeedAndPolynomialWithShortAsciiString2()
        {
            var actual = Crc32.Compute(_simpleBytes2Ascii);

            actual.Should().Be(0x6ee3ad88U);
        }
    }
}
