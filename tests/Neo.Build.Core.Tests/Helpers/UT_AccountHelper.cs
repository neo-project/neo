// Copyright (C) 2015-2025 The Neo Project.
//
// UT_AccountHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Helpers;
using Neo.SmartContract.Native;

namespace Neo.Build.Core.Tests.Helpers
{
    [TestClass]
    public class UT_AccountHelper
    {
        [TestMethod]
        public void TestGetBalance()
        {
            var balance = AccountHelper.GetBalance(TestNode.NeoSystem.StoreView, NativeContract.NEO.Hash, TestNode.Wallet.GetDefaultAccount().ScriptHash);

            Assert.AreNotEqual(0, balance.balance);
        }
    }
}
