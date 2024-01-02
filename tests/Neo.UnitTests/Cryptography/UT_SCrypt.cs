// Copyright (C) 2015-2024 The Neo Project.
//
// UT_SCrypt.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto.Generators;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_SCrypt
    {
        [TestMethod]
        public void DeriveKeyTest()
        {
            int N = 32, r = 2, p = 2;

            var derivedkey = SCrypt.Generate(new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x04, 0x05, 0x06 }, N, r, p, 64).ToHexString();
            Assert.AreEqual("b6274d3a81892c24335ab46a08ec16d040ac00c5943b212099a44b76a9b8102631ab988fa07fb35357cee7b0e3910098c0774c0e97399997676d890b2bf2bb25", derivedkey);
        }
    }
}
