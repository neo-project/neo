// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Nep17Token.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Tests.Helpers;
using Neo.Build.Core.Tokens;
using Neo.SmartContract.Native;

namespace Neo.Build.Core.Tests.Tokens
{
    [TestClass]
    public class UT_Nep17Token
    {
        [TestMethod]
        public void TestInitialize()
        {
            var nep17 = new Nep17Token(
                TestNode.NeoSystem.Settings,
                TestNode.NeoSystem.StoreView,
                NativeContract.GAS.Hash);

            Assert.AreEqual(NativeContract.GAS.Hash, nep17.ScriptHash);
            Assert.AreEqual(NativeContract.GAS.Symbol, nep17.Symbol);
            Assert.AreEqual(NativeContract.GAS.Decimals, nep17.Decimals);
        }

        [TestMethod]
        public void TestTotalSupply()
        {
            var nep17 = new Nep17Token(
                TestNode.NeoSystem.Settings,
                TestNode.NeoSystem.StoreView,
                NativeContract.GAS.Hash);

            Assert.AreEqual(5200000050000000uL, nep17.TotalSupply());
        }

        [TestMethod]
        public void TestBalanceOf()
        {
            var nep17 = new Nep17Token(
                TestNode.NeoSystem.Settings,
                TestNode.NeoSystem.StoreView,
                NativeContract.GAS.Hash);

            var walletAccount = TestNode.Wallet.GetDefaultAccount();

            Assert.AreEqual(50000000uL, nep17.BalanceOf(walletAccount.ScriptHash));
        }
    }
}
