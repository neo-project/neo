// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PrivateKeyHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Helpers;
using Neo.Wallets;
using System;

namespace Neo.Build.Core.Tests.Helpers
{
    [TestClass]
    public class UT_PrivateKeyHelper
    {
        [TestMethod]
        public void TestNep2Format()
        {
            var expectedPrivateKey = new byte[32];
            Array.Fill<byte>(expectedPrivateKey, 0x66);

            var expectedPassword = "12345678";

            var expectedKeyPair = new KeyPair(expectedPrivateKey);
            var actualNep2String = PrivateKeyHelper.ToNep2Format(expectedKeyPair, expectedPassword);

            Assert.AreEqual("6PYK755SwuTHaZ19G4jF1eG7wUo1nzvPPcT9UrBNaw9NC4C9aSTcreNKRx", actualNep2String);

            var actualKeyPair = PrivateKeyHelper.FromNep2Format(actualNep2String, expectedPassword);
            CollectionAssert.AreEqual(expectedPrivateKey, actualKeyPair.PrivateKey);
        }
    }
}
