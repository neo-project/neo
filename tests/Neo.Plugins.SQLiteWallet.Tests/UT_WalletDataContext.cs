// Copyright (C) 2015-2025 The Neo Project.
//
// UT_WalletDataContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Neo.Wallets.SQLite
{
    [TestClass]
    public class UT_WalletDataContext
    {
        private static int s_counter = 0;

        private string GetTestDbPath()
        {
            return $"test_context_{++s_counter}.db3";
        }

        [TestCleanup]
        public void Cleanup()
        {
            SqliteConnection.ClearAllPools();
            var testFiles = Directory.GetFiles(".", "test_context_*");
            foreach (var file in testFiles)
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void TestContextCreation()
        {
            using var context = new WalletDataContext(GetTestDbPath());
            Assert.IsNotNull(context);
            Assert.IsNotNull(context.Accounts);
            Assert.IsNotNull(context.Addresses);
            Assert.IsNotNull(context.Contracts);
            Assert.IsNotNull(context.Keys);
        }

        [TestMethod]
        public void TestDatabaseCreation()
        {
            var path = GetTestDbPath();
            using var context = new WalletDataContext(path);
            context.Database.EnsureCreated();

            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod]
        public void TestAccountOperations()
        {
            using var context = new WalletDataContext(GetTestDbPath());
            context.Database.EnsureCreated();

            var account = new Account
            {
                PublicKeyHash = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20],
                Nep2key = "test_nep2_key"
            };

            context.Accounts.Add(account);
            context.SaveChanges();

            var retrievedAccount = context.Accounts.FirstOrDefault(a => a.PublicKeyHash.SequenceEqual(account.PublicKeyHash));
            Assert.IsNotNull(retrievedAccount);
            Assert.AreEqual(account.Nep2key, retrievedAccount.Nep2key);
        }

        [TestMethod]
        public void TestAddressOperations()
        {
            using var context = new WalletDataContext(GetTestDbPath());
            context.Database.EnsureCreated();

            var address = new Address { ScriptHash = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20] };

            context.Addresses.Add(address);
            context.SaveChanges();

            var retrievedAddress = context.Addresses.FirstOrDefault(a => a.ScriptHash.SequenceEqual(address.ScriptHash));
            Assert.IsNotNull(retrievedAddress);
        }

        [TestMethod]
        public void TestContractOperations()
        {
            using var context = new WalletDataContext(GetTestDbPath());
            context.Database.EnsureCreated();

            var hash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var contract = new Contract
            {
                RawData = [1, 2, 3, 4, 5],
                ScriptHash = hash,
                PublicKeyHash = hash
            };

            context.Contracts.Add(contract);
            Assert.ThrowsExactly<DbUpdateException>(() => context.SaveChanges()); // FOREIGN KEY constraint failed

            context.Accounts.Add(new Account { PublicKeyHash = hash, Nep2key = "" });
            context.Addresses.Add(new Address { ScriptHash = hash });
            context.SaveChanges();

            var retrievedContract = context.Contracts.FirstOrDefault(c => c.ScriptHash.SequenceEqual(contract.ScriptHash));
            Assert.IsNotNull(retrievedContract);
            Assert.AreEqual(contract.RawData.Length, retrievedContract.RawData.Length);
        }

        [TestMethod]
        public void TestKeyOperations()
        {
            using var context = new WalletDataContext(GetTestDbPath());
            context.Database.EnsureCreated();

            var key = new Key
            {
                Name = "test_key",
                Value = [1, 2, 3, 4, 5]
            };

            context.Keys.Add(key);
            context.SaveChanges();

            var retrievedKey = context.Keys.FirstOrDefault(k => k.Name == key.Name);
            Assert.IsNotNull(retrievedKey);
            Assert.AreEqual(key.Name, retrievedKey.Name);
            Assert.AreEqual(key.Value.Length, retrievedKey.Value.Length);
        }

        [TestMethod]
        public void TestDatabaseDeletion()
        {
            var path = GetTestDbPath();
            using var context = new WalletDataContext(path);
            context.Database.EnsureCreated();
            Assert.IsTrue(File.Exists(path));

            context.Database.EnsureDeleted();
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void TestMultipleOperations()
        {
            var path = GetTestDbPath();
            using var context = new WalletDataContext(path);
            context.Database.EnsureCreated();

            var hash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var account = new Account { PublicKeyHash = hash, Nep2key = "test_nep2_key" };
            var address = new Address { ScriptHash = hash };
            var key = new Key { Name = "test_key", Value = [1, 2, 3, 4, 5] };

            context.Accounts.Add(account);
            context.Addresses.Add(address);
            context.Keys.Add(key);
            context.SaveChanges();

            // Verify all entities were saved
            Assert.AreEqual(1, context.Accounts.Count());
            Assert.AreEqual(1, context.Addresses.Count());
            Assert.AreEqual(1, context.Keys.Count());
        }

        [TestMethod]
        public void TestUpdateOperations()
        {
            var path = GetTestDbPath();
            using var context = new WalletDataContext(path);
            context.Database.EnsureCreated();

            var key = new Key { Name = "test_key", Value = [1, 2, 3, 4, 5] };
            context.Keys.Add(key);
            context.SaveChanges();

            // Update the key
            key.Value = [6, 7, 8, 9, 10];
            context.SaveChanges();

            var retrievedKey = context.Keys.FirstOrDefault(k => k.Name == key.Name);
            Assert.IsNotNull(retrievedKey);
            Assert.AreEqual(5, retrievedKey.Value.Length);
            Assert.AreEqual(6, retrievedKey.Value[0]);
        }
    }
}
