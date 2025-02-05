// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RIPEMD160Managed.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            Assert.AreEqual("98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f", hash.ToHexString());
        }

        [TestMethod]
        public void TestTryComputeHash()
        {
            using var ripemd160 = new RIPEMD160Managed();
            var buffer = new byte[ripemd160.HashSize / 8];
            var ok = ripemd160.TryComputeHash("hello world"u8.ToArray(), buffer, out _);
            Assert.IsTrue(ok);
            Assert.AreEqual("98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f", buffer.ToHexString());
        }
    }
}
