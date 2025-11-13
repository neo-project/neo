// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MLDsa.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions.Factories;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_MLDsa
    {
        [TestMethod]
        public void TestMLDsa65()
        {
            var privateKey = MLDsaPrivateKey.CreateMLDsa65();
            var publicKey = privateKey.PublicKey;

            var message = RandomNumberFactory.NextBytes(RandomNumberFactory.NextInt32(10, 65536), false);
            var sign = privateKey.Sign(message);
            Assert.AreEqual(3309, sign.Length);

            var ok = publicKey.Verify(message, sign);
            Assert.IsTrue(ok);
        }
    }
}
