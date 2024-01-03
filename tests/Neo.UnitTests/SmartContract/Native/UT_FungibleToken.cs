// Copyright (C) 2015-2024 The Neo Project.
//
// UT_FungibleToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_FungibleToken : TestKit
    {
        [TestMethod]
        public void TestTotalSupply()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            NativeContract.GAS.TotalSupply(snapshot).Should().Be(5200000050000000);
        }
    }
}
