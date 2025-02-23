// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DevWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Tests.Helpers;
using Neo.Build.Core.Wallets;
using System.Security.Cryptography;

namespace Neo.Build.Core.Tests.Wallets
{
    [TestClass]
    public class UT_DevWallet
    {
        [TestMethod]
        public void CreateAccountWithPrivateKey()
        {
            var testWalletModel = TestObjectHelper.CreateTestWalletModel()!;
            var devWallet = new DevWallet(testWalletModel);

            var expectedPrivateKey = RandomNumberGenerator.GetBytes(32);
            var expectedDevWalletAccount = devWallet.CreateAccount(expectedPrivateKey);

            Assert.IsTrue(devWallet.Contains(expectedDevWalletAccount!.ScriptHash));

            var actualDevWalletAccount = devWallet.GetAccount(expectedDevWalletAccount!.ScriptHash);

            Assert.IsNotNull(actualDevWalletAccount);
            Assert.IsInstanceOfType<DevWalletAccount>(actualDevWalletAccount);
            Assert.IsTrue(actualDevWalletAccount.HasKey);
            CollectionAssert.AreEqual(expectedPrivateKey, actualDevWalletAccount.GetKey().PrivateKey);
        }

        // TODO: Add more tests for this class
    }
}
