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
            var expectedAccount = TestNode.Wallet.GetDefaultAccount();

            var actualAccountBalance = AccountHelper.GetBalance(
                TestNode.NeoSystem,
                NativeContract.GAS.Hash,
                expectedAccount.ScriptHash);

            Assert.AreEqual(expectedAccount.ScriptHash, actualAccountBalance.AccountHash);
            Assert.AreEqual(NativeContract.GAS.Hash, actualAccountBalance.ContractHash);
            Assert.AreEqual(NativeContract.GAS.Symbol, actualAccountBalance.Symbol);
            Assert.AreEqual(50000000, actualAccountBalance.Balance);
            Assert.AreEqual(8, actualAccountBalance.Decimals);
        }
    }
}
