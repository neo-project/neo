// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Hasher.cs file belongs to the neo project and is free
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
    public class UT_Hasher
    {
        [TestMethod]
        public void Values_MatchSpecification()
        {
#pragma warning disable CS0618 // Hasher is obsolete; still present for compatibility
            Assert.AreEqual(0x00, (byte)Hasher.SHA256);
            Assert.AreEqual(0x01, (byte)Hasher.Keccak256);
            Assert.AreEqual((byte)HashAlgorithm.SHA256, (byte)Hasher.SHA256);
            Assert.AreEqual((byte)HashAlgorithm.Keccak256, (byte)Hasher.Keccak256);
#pragma warning restore CS0618
        }
    }
}
