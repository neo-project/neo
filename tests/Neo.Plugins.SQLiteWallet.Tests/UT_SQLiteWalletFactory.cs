// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SQLiteWalletFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

namespace Neo.Wallets.SQLite
{
    [TestClass]
    public class UT_SQLiteWalletFactory
    {
        private const string TestPassword = "test_password_123";
        private static readonly ProtocolSettings TestSettings = ProtocolSettings.Default;
        private static int s_counter = 0;

        private string GetTestWalletPath()
        {
            return $"test_factory_wallet_{++s_counter}.db3";
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            // Clean up any remaining test database files
            var testFiles = Directory.GetFiles(".", "test_factory_wallet_*");
            foreach (var file in testFiles)
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void TestFactoryName()
        {
            var factory = new SQLiteWalletFactory();
            Assert.AreEqual("SQLiteWallet", factory.Name);
        }

        [TestMethod]
        public void TestFactoryDescription()
        {
            var factory = new SQLiteWalletFactory();
            Assert.AreEqual("A SQLite-based wallet provider that supports wallet files with .db3 suffix.", factory.Description);
        }

        [TestMethod]
        public void TestHandleWithDb3Extension()
        {
            var factory = new SQLiteWalletFactory();

            // Test with .db3 extension
            Assert.IsTrue(factory.Handle("wallet.db3"));
            Assert.IsTrue(factory.Handle("test.db3"));
            Assert.IsTrue(factory.Handle("path/to/wallet.db3"));

            // Test case insensitive
            Assert.IsTrue(factory.Handle("wallet.DB3"));
            Assert.IsTrue(factory.Handle("wallet.Db3"));
        }

        [TestMethod]
        public void TestHandleWithNonDb3Extension()
        {
            var factory = new SQLiteWalletFactory();
            Assert.IsFalse(factory.Handle("wallet.json"));
            Assert.IsFalse(factory.Handle("wallet.dat"));
            Assert.IsFalse(factory.Handle("wallet"));
            Assert.IsFalse(factory.Handle(""));
        }

        [TestMethod]
        public void TestCreateWallet()
        {
            var factory = new SQLiteWalletFactory();
            var path = GetTestWalletPath();
            var wallet = factory.CreateWallet("TestWallet", path, TestPassword, TestSettings);

            Assert.IsNotNull(wallet);
            Assert.IsInstanceOfType(wallet, typeof(SQLiteWallet));
            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod]
        public void TestOpenWallet()
        {
            var factory = new SQLiteWalletFactory();
            var path = GetTestWalletPath();
            factory.CreateWallet("TestWallet", path, TestPassword, TestSettings);

            var wallet = factory.OpenWallet(path, TestPassword, TestSettings);
            Assert.IsNotNull(wallet);
            Assert.IsInstanceOfType(wallet, typeof(SQLiteWallet));
        }

        [TestMethod]
        public void TestOpenWalletWithInvalidPassword()
        {
            var factory = new SQLiteWalletFactory();
            var path = GetTestWalletPath();
            factory.CreateWallet("TestWallet", path, TestPassword, TestSettings);
            Assert.ThrowsExactly<CryptographicException>(() => factory.OpenWallet(path, "wrong_password", TestSettings));
        }
    }
}
