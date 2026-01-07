// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AssetDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract.Native;
using Neo.Wallets;

namespace Neo.UnitTests.Wallets;

[TestClass]
public class UT_AssetDescriptor
{
    [TestMethod]
    public void TestConstructorWithNonexistAssetId()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        Assert.ThrowsExactly<ArgumentException>(() => new AssetDescriptor(snapshotCache, UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4")));
    }

    [TestMethod]
    public void Check_GAS()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var descriptor = new AssetDescriptor(snapshotCache, NativeContract.Governance.GasTokenId);
        Assert.AreEqual(NativeContract.Governance.GasTokenId, descriptor.AssetId);
        Assert.AreEqual(Governance.GasTokenName, descriptor.AssetName);
        Assert.AreEqual(Governance.GasTokenName, descriptor.ToString());
        Assert.AreEqual("GAS", descriptor.Symbol);
        Assert.AreEqual(8, descriptor.Decimals);
    }

    [TestMethod]
    public void Check_NEO()
    {
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var descriptor = new AssetDescriptor(snapshotCache, NativeContract.Governance.NeoTokenId);
        Assert.AreEqual(NativeContract.Governance.NeoTokenId, descriptor.AssetId);
        Assert.AreEqual(Governance.NeoTokenName, descriptor.AssetName);
        Assert.AreEqual(Governance.NeoTokenName, descriptor.ToString());
        Assert.AreEqual(Governance.NeoTokenSymbol, descriptor.Symbol);
        Assert.AreEqual(Governance.NeoTokenDecimals, descriptor.Decimals);
    }
}
