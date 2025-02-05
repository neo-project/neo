// Copyright (C) 2015-2025 The Neo Project.
//
// UT_WalletAccount.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.Wallets;

namespace Neo.UnitTests.Wallets
{
    public class MyWalletAccount : WalletAccount
    {
        private KeyPair key = null;
        public override bool HasKey => key != null;

        public MyWalletAccount(UInt160 scriptHash)
            : base(scriptHash, TestProtocolSettings.Default)
        {
        }

        public override KeyPair GetKey()
        {
            return key;
        }

        public void SetKey(KeyPair inputKey)
        {
            key = inputKey;
        }
    }

    [TestClass]
    public class UT_WalletAccount
    {
        [TestMethod]
        public void TestGetAddress()
        {
            MyWalletAccount walletAccount = new MyWalletAccount(UInt160.Zero);
            Assert.AreEqual("NKuyBkoGdZZSLyPbJEetheRhMjeznFZszf", walletAccount.Address);
        }

        [TestMethod]
        public void TestGetWatchOnly()
        {
            MyWalletAccount walletAccount = new MyWalletAccount(UInt160.Zero);
            Assert.IsTrue(walletAccount.WatchOnly);
            walletAccount.Contract = new Contract();
            Assert.IsFalse(walletAccount.WatchOnly);
        }
    }
}
