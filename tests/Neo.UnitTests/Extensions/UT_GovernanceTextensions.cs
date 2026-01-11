// Copyright (C) 2015-2026 The Neo Project.
//
// UT_GovernanceTextensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.SmartContract;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.Extensions;

[TestClass]
public class UT_GovernanceExtensions
{
    private NeoSystem _system = null!;

    [TestInitialize]
    public void Setup()
    {
        _system = TestBlockchain.GetSystem();
    }

    [TestMethod]
    public void TestGetAccounts()
    {
        UInt160 expected = "0x9f8f056a53e39585c7bb52886418c7bed83d126b";

        var accounts = NativeContract.Governance.GetAccounts(_system.StoreView);
        var (address, balance) = accounts.FirstOrDefault();

        Assert.AreEqual(expected, address);
        Assert.AreEqual(5200000000000000, balance);
    }
}
