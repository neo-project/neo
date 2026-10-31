// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Wallet_CreateOpen.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.IO;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_Wallet_CreateOpen
    {
        [TestMethod]
        public void Create_And_Open_JsonWallet()
        {
            var path = Path.Combine(Path.GetTempPath(), $"neo-ut-wallet-{Guid.NewGuid():N}.json");
            try
            {
                var created = Wallet.Create("name", path, "p@ss", TestProtocolSettings.Default);
                Assert.IsNotNull(created);
                Assert.IsInstanceOfType<NEP6Wallet>(created);

                var opened = Wallet.Open(path, "p@ss", TestProtocolSettings.Default);
                Assert.IsNotNull(opened);
                Assert.IsInstanceOfType<NEP6Wallet>(opened);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [TestMethod]
        public void Create_UnsupportedExtension_ReturnsNull()
        {
            var path = Path.Combine(Path.GetTempPath(), $"neo-ut-wallet-{Guid.NewGuid():N}.db3");
            Assert.IsNull(Wallet.Create("n", path, "p", TestProtocolSettings.Default));
            Assert.IsNull(Wallet.Open(path, "p", TestProtocolSettings.Default));
        }
    }
}
