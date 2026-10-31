// Copyright (C) 2015-2026 The Neo Project.
//
// UT_NEP6WalletFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets.NEP6;
using System;
using System.IO;

namespace Neo.UnitTests.Wallets.NEP6
{
    [TestClass]
    public class UT_NEP6WalletFactory
    {
        [TestMethod]
        public void Handle_AcceptsJsonExtension_CaseInsensitive()
        {
            var factory = NEP6WalletFactory.Instance;
            Assert.IsTrue(factory.Handle("wallet.json"));
            Assert.IsTrue(factory.Handle("wallet.JSON"));
            Assert.IsFalse(factory.Handle("wallet.db3"));
            Assert.IsFalse(factory.Handle("wallet"));
        }

        [TestMethod]
        public void CreateWallet_And_OpenWallet_RoundTrip()
        {
            var path = Path.Combine(Path.GetTempPath(), $"neo-ut-nep6-{Guid.NewGuid():N}.json");
            try
            {
                var created = NEP6WalletFactory.Instance.CreateWallet("ut", path, "pass", TestProtocolSettings.Default);
                Assert.IsInstanceOfType<NEP6Wallet>(created);
                Assert.IsTrue(File.Exists(path));

                Assert.ThrowsExactly<InvalidOperationException>(() =>
                    NEP6WalletFactory.Instance.CreateWallet("ut", path, "pass", TestProtocolSettings.Default));

                var opened = NEP6WalletFactory.Instance.OpenWallet(path, "pass", TestProtocolSettings.Default);
                Assert.IsInstanceOfType<NEP6Wallet>(opened);
                Assert.AreEqual(path, opened.Path);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
