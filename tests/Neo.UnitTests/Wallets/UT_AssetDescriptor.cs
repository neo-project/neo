// Copyright (C) 2015-2025 The Neo Project.
//
// UT_AssetDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using Neo.Wallets;
using System;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_AssetDescriptor
    {
        [TestMethod]
        public void TestConstructorWithNonexistAssetId()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            Action action = () =>
            {
                var descriptor = new AssetDescriptor(snapshotCache, TestProtocolSettings.Default, UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4"));
            };
            Assert.ThrowsException<ArgumentException>(action);
        }

        [TestMethod]
        public void Check_GAS()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var descriptor = new AssetDescriptor(snapshotCache, TestProtocolSettings.Default, NativeContract.GAS.Hash);
            Assert.AreEqual(NativeContract.GAS.Hash, descriptor.AssetId);
            Assert.AreEqual(nameof(GasToken), descriptor.AssetName);
            Assert.AreEqual(nameof(GasToken), descriptor.ToString());
            Assert.AreEqual("GAS", descriptor.Symbol);
            Assert.AreEqual(8, descriptor.Decimals);
        }

        [TestMethod]
        public void Check_NEO()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var descriptor = new AssetDescriptor(snapshotCache, TestProtocolSettings.Default, NativeContract.NEO.Hash);
            Assert.AreEqual(NativeContract.NEO.Hash, descriptor.AssetId);
            Assert.AreEqual(nameof(NeoToken), descriptor.AssetName);
            Assert.AreEqual(nameof(NeoToken), descriptor.ToString());
            Assert.AreEqual("NEO", descriptor.Symbol);
            Assert.AreEqual(0, descriptor.Decimals);
        }
    }
}
