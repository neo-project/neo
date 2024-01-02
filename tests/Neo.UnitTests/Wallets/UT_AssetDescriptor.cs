// Copyright (C) 2015-2024 The Neo Project.
//
// UT_AssetDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;
using System;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_AssetDescriptor
    {
        [TestMethod]
        public void TestConstructorWithNonexistAssetId()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            Action action = () =>
            {
                var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, TestProtocolSettings.Default, UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4"));
            };
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Check_GAS()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, TestProtocolSettings.Default, NativeContract.GAS.Hash);
            descriptor.AssetId.Should().Be(NativeContract.GAS.Hash);
            descriptor.AssetName.Should().Be(nameof(GasToken));
            descriptor.ToString().Should().Be(nameof(GasToken));
            descriptor.Symbol.Should().Be("GAS");
            descriptor.Decimals.Should().Be(8);
        }

        [TestMethod]
        public void Check_NEO()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var descriptor = new Neo.Wallets.AssetDescriptor(snapshot, TestProtocolSettings.Default, NativeContract.NEO.Hash);
            descriptor.AssetId.Should().Be(NativeContract.NEO.Hash);
            descriptor.AssetName.Should().Be(nameof(NeoToken));
            descriptor.ToString().Should().Be(nameof(NeoToken));
            descriptor.Symbol.Should().Be("NEO");
            descriptor.Decimals.Should().Be(0);
        }
    }
}
