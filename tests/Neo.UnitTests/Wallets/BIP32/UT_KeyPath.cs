// Copyright (C) 2015-2026 The Neo Project.
//
// UT_KeyPath.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.BIP32;
using System;

namespace Neo.UnitTests.Wallets.BIP32
{
    [TestClass]
    public class UT_KeyPath
    {
        [TestMethod]
        public void Master_IsEmpty()
        {
            Assert.IsEmpty(KeyPath.Master.Indices);
            Assert.AreEqual("m", KeyPath.Master.ToString());
        }

        [TestMethod]
        public void Parse_And_ToString_RoundTrip()
        {
            var path = KeyPath.Parse("m/44'/0'/0'/0/1");
            Assert.HasCount(5, path.Indices);
            Assert.AreEqual(0x80000000u | 44u, path.Indices[0]);
            Assert.AreEqual(0x80000000u | 0u, path.Indices[1]);
            Assert.AreEqual(0x80000000u | 0u, path.Indices[2]);
            Assert.AreEqual(0u, path.Indices[3]);
            Assert.AreEqual(1u, path.Indices[4]);
            Assert.AreEqual("m/44'/0'/0'/0/1", path.ToString());
        }

        [TestMethod]
        public void Derive_AppendsIndex()
        {
            var child = KeyPath.Master.Derive(5).Derive(0x80000000 | 7);
            Assert.HasCount(2, child.Indices);
            Assert.AreEqual(5u, child.Indices[0]);
            Assert.AreEqual(0x80000000u | 7u, child.Indices[1]);
            Assert.AreEqual("m/5/7'", child.ToString());
        }

        [TestMethod]
        public void Parse_Invalid_Throws()
        {
            Assert.ThrowsExactly<FormatException>(() => KeyPath.Parse("x/1"));
            Assert.ThrowsExactly<FormatException>(() => KeyPath.Parse("m/2147483648")); // >= 0x80000000 without hardened
        }
    }
}
