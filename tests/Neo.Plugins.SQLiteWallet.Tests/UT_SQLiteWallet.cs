// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SQLiteWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Data.Sqlite;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.Wallets.NEP6;
using System.Security.Cryptography;

namespace Neo.Wallets.SQLite
{
    [TestClass]
    public class UT_SQLiteWallet
    {
        private const string TestPassword = "test_password_123";
        private static readonly ProtocolSettings TestSettings = ProtocolSettings.Default;
        private static int s_counter = 0;

        private static string GetTestWalletPath()
        {
            return $"test_wallet_{++s_counter}.db3";
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            var files = Directory.GetFiles(".", "test_wallet_*");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void TestCreateWallet()
        {
            var path = GetTestWalletPath();
            var wallet = SQLiteWallet.Create(path, TestPassword, TestSettings);

            Assert.IsNotNull(wallet);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(path), wallet.Name);
            Assert.IsTrue(File.Exists(path));

            // Test that wallet can be opened with correct password
            var openedWallet = SQLiteWallet.Open(path, TestPassword, TestSettings);
            Assert.IsNotNull(openedWallet);
            Assert.AreEqual(wallet.Name, openedWallet.Name);
        }

        [TestMethod]
        public void TestCreateWalletWithCustomScrypt()
        {
            var customScrypt = new ScryptParameters(16384, 8, 8);
            var path = GetTestWalletPath();
            var wallet = SQLiteWallet.Create(path, TestPassword, TestSettings, customScrypt);

            Assert.IsNotNull(wallet);
            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod]
        public void TestOpenWalletWithInvalidPassword()
        {
            var path = GetTestWalletPath();
            // Create wallet first
            SQLiteWallet.Create(path, TestPassword, TestSettings);

            // Try to open with wrong password
            Assert.ThrowsExactly<CryptographicException>(() => SQLiteWallet.Open(path, "wrong_password", TestSettings));
        }

        [TestMethod]
        public void TestOpenNonExistentWallet()
        {
            Assert.ThrowsExactly<InvalidOperationException>(
                () => SQLiteWallet.Open("test_non_existent.db3", TestPassword, TestSettings),
                "Wallet file test_non_existent.db3 not found");
        }

        [TestMethod]
        public void TestWalletName()
        {
            var path = GetTestWalletPath();
            var wallet = SQLiteWallet.Create(path, TestPassword, TestSettings);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(path), wallet.Name);
        }

        [TestMethod]
        public void TestWalletVersion()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            var version = wallet.Version;
            Assert.IsNotNull(version);
            Assert.IsTrue(version.Major >= 0);
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);

            Assert.IsTrue(wallet.VerifyPassword(TestPassword));
            Assert.IsFalse(wallet.VerifyPassword("wrong_password"));
            Assert.IsFalse(wallet.VerifyPassword(""));
        }

        [TestMethod]
        public void TestChangePassword()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            const string newPassword = "new_password_456";

            // Test successful password change
            Assert.IsTrue(wallet.ChangePassword(TestPassword, newPassword));
            Assert.IsTrue(wallet.VerifyPassword(newPassword));
            Assert.IsFalse(wallet.VerifyPassword(TestPassword));

            // Test password change with wrong old password
            Assert.IsFalse(wallet.ChangePassword("wrong_old_password", "another_password"));
        }

        [TestMethod]
        public void TestCreateAccountWithPrivateKey()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);

            var account = wallet.CreateAccount(privateKey);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.HasKey);
            Assert.IsNotNull(account.GetKey());
            Assert.IsTrue(wallet.Contains(account.ScriptHash));
        }

        [TestMethod]
        public void TestCreateAccountWithContract()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            var keyPair = new KeyPair(privateKey);
            var contract = new VerificationContract
            {
                Script = SmartContract.Contract.CreateSignatureRedeemScript(keyPair.PublicKey),
                ParameterList = [ContractParameterType.Signature]
            };

            var account = wallet.CreateAccount(contract, keyPair);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.HasKey);
            Assert.AreEqual(contract.ScriptHash, account.ScriptHash);
            Assert.IsTrue(wallet.Contains(account.ScriptHash));
        }

        [TestMethod]
        public void TestCreateAccountWithScriptHash()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            var scriptHash = UInt160.Zero;
            var account = wallet.CreateAccount(scriptHash);
            Assert.IsNotNull(account);
            Assert.IsFalse(account.HasKey);
            Assert.AreEqual(scriptHash, account.ScriptHash);
            Assert.IsTrue(wallet.Contains(scriptHash));
        }

        [TestMethod]
        public void TestGetAccount()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            var account = wallet.CreateAccount(privateKey);

            var retrievedAccount = wallet.GetAccount(account.ScriptHash);
            Assert.IsNotNull(retrievedAccount);
            Assert.AreEqual(account.ScriptHash, retrievedAccount.ScriptHash);

            // Test getting non-existent account
            var nonExistentAccount = wallet.GetAccount(UInt160.Zero);
            Assert.IsNull(nonExistentAccount);
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);

            // Initially no accounts
            var accounts = wallet.GetAccounts().ToArray();
            Assert.AreEqual(0, accounts.Length);

            // Add some accounts
            var privateKey1 = new byte[32];
            var privateKey2 = new byte[32];
            RandomNumberGenerator.Fill(privateKey1);
            RandomNumberGenerator.Fill(privateKey2);

            var account1 = wallet.CreateAccount(privateKey1);
            var account2 = wallet.CreateAccount(privateKey2);

            accounts = wallet.GetAccounts().ToArray();
            Assert.AreEqual(2, accounts.Length);
            Assert.IsTrue(accounts.Any(a => a.ScriptHash == account1.ScriptHash));
            Assert.IsTrue(accounts.Any(a => a.ScriptHash == account2.ScriptHash));
        }

        [TestMethod]
        public void TestContains()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            var account = wallet.CreateAccount(privateKey);

            Assert.IsTrue(wallet.Contains(account.ScriptHash));
            Assert.IsFalse(wallet.Contains(UInt160.Zero));
        }

        [TestMethod]
        public void TestDeleteAccount()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            var account = wallet.CreateAccount(privateKey);

            Assert.IsTrue(wallet.Contains(account.ScriptHash));

            // Delete account
            Assert.IsTrue(wallet.DeleteAccount(account.ScriptHash));
            Assert.IsFalse(wallet.Contains(account.ScriptHash));

            // Try to delete non-existent account
            Assert.IsFalse(wallet.DeleteAccount(UInt160.Zero));
        }

        [TestMethod]
        public void TestDeleteWallet()
        {
            var path = GetTestWalletPath();
            var wallet = SQLiteWallet.Create(path, TestPassword, TestSettings);
            Assert.IsTrue(File.Exists(path));

            wallet.Delete();
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void TestSave()
        {
            var wallet = SQLiteWallet.Create(GetTestWalletPath(), TestPassword, TestSettings);

            // Save should not throw exception (it's a no-op for SQLiteWallet)
            wallet.Save();
        }

        [TestMethod]
        public void TestEncryptDecrypt()
        {
            var data = new byte[32];
            var key = new byte[32];
            var iv = new byte[16];
            RandomNumberGenerator.Fill(data);
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(iv);

            // Test encryption
            var encrypted = SQLiteWallet.Encrypt(data, key, iv);
            Assert.IsNotNull(encrypted);
            Assert.AreEqual(data.Length, encrypted.Length);
            Assert.IsFalse(data.SequenceEqual(encrypted));

            // Test decryption
            var decrypted = SQLiteWallet.Decrypt(encrypted, key, iv);
            Assert.IsTrue(data.SequenceEqual(decrypted));
        }

        [TestMethod]
        public void TestEncryptWithInvalidParameters()
        {
            var data = new byte[15]; // Not multiple of 16
            var key = new byte[32];
            var iv = new byte[16];
            Assert.ThrowsExactly<ArgumentException>(() => SQLiteWallet.Encrypt(data, key, iv));

            data = new byte[32];
            key = new byte[31]; // Wrong key length
            Assert.ThrowsExactly<ArgumentException>(() => SQLiteWallet.Encrypt(data, key, iv));

            key = new byte[32];
            iv = new byte[15]; // Wrong IV length
            Assert.ThrowsExactly<ArgumentException>(() => SQLiteWallet.Encrypt(data, key, iv));
        }

        [TestMethod]
        public void TestToAesKey()
        {
            const string password = "test_password";
            var key1 = SQLiteWallet.ToAesKey(password);
            var key2 = SQLiteWallet.ToAesKey(password);

            Assert.IsNotNull(key1);
            Assert.AreEqual(32, key1.Length);
            Assert.IsTrue(key1.SequenceEqual(key2)); // Should be deterministic

            // Test with different password
            var key3 = SQLiteWallet.ToAesKey("different_password");
            Assert.IsFalse(key1.SequenceEqual(key3));
        }

        [TestMethod]
        public void TestAccountPersistence()
        {
            // Create wallet and add account
            var path = GetTestWalletPath();
            var wallet1 = SQLiteWallet.Create(path, TestPassword, TestSettings);
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            var account1 = wallet1.CreateAccount(privateKey);

            // Close and reopen wallet
            var wallet2 = SQLiteWallet.Open(path, TestPassword, TestSettings);

            // Verify account still exists
            Assert.IsTrue(wallet2.Contains(account1.ScriptHash));
            var account2 = wallet2.GetAccount(account1.ScriptHash);
            Assert.IsNotNull(account2);
            Assert.AreEqual(account1.ScriptHash, account2.ScriptHash);
            Assert.IsTrue(account2.HasKey);
        }

        [TestMethod]
        public void TestMultipleAccounts()
        {
            var path = GetTestWalletPath();
            var wallet = SQLiteWallet.Create(path, TestPassword, TestSettings);

            // Create multiple accounts
            var accounts = new WalletAccount[5];
            for (int i = 0; i < 5; i++)
            {
                var privateKey = new byte[32];
                RandomNumberGenerator.Fill(privateKey);
                accounts[i] = wallet.CreateAccount(privateKey);
            }

            // Verify all accounts exist
            var retrievedAccounts = wallet.GetAccounts().ToArray();
            Assert.AreEqual(5, retrievedAccounts.Length);

            foreach (var account in accounts)
            {
                Assert.IsTrue(wallet.Contains(account.ScriptHash));
                var retrievedAccount = wallet.GetAccount(account.ScriptHash);
                Assert.IsNotNull(retrievedAccount);
                Assert.AreEqual(account.ScriptHash, retrievedAccount.ScriptHash);
            }
        }

        [TestMethod]
        public void TestAccountWithContractPersistence()
        {
            var path = GetTestWalletPath();
            var wallet1 = SQLiteWallet.Create(path, TestPassword, TestSettings);
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            var keyPair = new KeyPair(privateKey);
            var contract = new VerificationContract
            {
                Script = SmartContract.Contract.CreateSignatureRedeemScript(keyPair.PublicKey),
                ParameterList = [ContractParameterType.Signature]
            };
            var account1 = wallet1.CreateAccount(contract, keyPair);

            // Reopen wallet
            var wallet2 = SQLiteWallet.Open(path, TestPassword, TestSettings);
            var account2 = wallet2.GetAccount(account1.ScriptHash);

            Assert.IsNotNull(account2);
            Assert.IsTrue(account2.HasKey);
            Assert.IsNotNull(account2.Contract);
        }
    }
}
