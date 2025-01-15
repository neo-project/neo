// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RIPEMD160Managed.cs file belongs to the neo project and is free
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
using Neo.Extensions;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_RIPEMD160Managed
    {
        [TestMethod]
        public void TestHashCore()
        {
            using var ripemd160 = new RIPEMD160Managed();
            var hash = ripemd160.ComputeHash("hello world"u8.ToArray());
            hash.ToHexString().Should().Be("98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f");
        }

        [TestMethod]
        public void TestTryComputeHash()
        {
            using var ripemd160 = new RIPEMD160Managed();
            var buffer = new byte[ripemd160.HashSize / 8];
            var ok = ripemd160.TryComputeHash("hello world"u8.ToArray(), buffer, out _);
            ok.Should().BeTrue();
            buffer.ToHexString().Should().Be("98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f");
        }
    }
}
