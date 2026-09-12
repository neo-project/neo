// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Wallet_AddressAndXor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Wallets;
using System;
using WalletHelper = Neo.Wallets.Helper;

namespace Neo.UnitTests.Wallets
{
    /// <summary>
    /// Coverage for ToAddress and XOR. Does not re-test ToScriptHash paths already in UT_Wallets_Helper.
    /// </summary>
    [TestClass]
    public class UT_Wallet_AddressAndXor
    {
        [TestMethod]
        public void ToAddress_RoundTrip_WithDefaultVersion()
        {
            var scriptHash = new UInt160(Crypto.Hash160([0x01, 0x02, 0x03]));
            var version = TestProtocolSettings.Default.AddressVersion;
            var address = scriptHash.ToAddress(version);
            Assert.AreEqual(scriptHash, address.ToScriptHash(version));
        }

        [TestMethod]
        public void ToAddress_DifferentVersions_ProduceDifferentAddresses()
        {
            var scriptHash = UInt160.Zero;
            var a = scriptHash.ToAddress(0x17);
            var b = scriptHash.ToAddress(0x35);
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void XOR_EqualLength_XorsBytes()
        {
            byte[] x = [0x0F, 0xF0, 0xAA];
            byte[] y = [0xF0, 0x0F, 0x55];
            Assert.AreSequenceEqual((byte[])[0xFF, 0xFF, 0xFF], WalletHelper.XOR(x, y));
        }

        [TestMethod]
        public void XOR_mismatchedLength_Throws()
        {
            byte[] x = [0x01, 0x02];
            byte[] y = [0x01];
            var ex = Assert.ThrowsExactly<ArgumentException>(() => WalletHelper.XOR(x, y));
            Assert.IsTrue(ex.Message.Contains("must be equal"));
        }

        [TestMethod]
        public void XOR_empty_ReturnsEmpty()
        {
            Assert.IsEmpty(WalletHelper.XOR([], []));
        }
    }
}
