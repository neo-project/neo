// Copyright (C) 2015-2026 The Neo Project.
//
// UT_HashAlgorithm.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_HashAlgorithm
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
            Assert.AreEqual(0x00, (byte)HashAlgorithm.SHA256);
            Assert.AreEqual(0x01, (byte)HashAlgorithm.Keccak256);
            Assert.AreEqual(0x02, (byte)HashAlgorithm.SHA512);
        }

        [TestMethod]
        public void AllValues_AreDefined()
        {
            foreach (HashAlgorithm algorithm in System.Enum.GetValues<HashAlgorithm>())
                Assert.IsTrue(System.Enum.IsDefined(algorithm));
        }
    }
}
