// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Cryptography;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.UnitTests.Wallets
{
    internal class MyWallet : Wallet
    {
        public override string Name => "MyWallet";

        public override Version Version => Version.Parse("0.0.1");

        private readonly Dictionary<UInt160, WalletAccount> accounts = new();

        public MyWallet() : base(null, TestProtocolSettings.Default)
        {
        }

        public override bool ChangePassword(string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(UInt160 scriptHash)
        {
            return accounts.ContainsKey(scriptHash);
        }

        public void AddAccount(WalletAccount account)
        {
            accounts.Add(account.ScriptHash, account);
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            KeyPair key = new(privateKey);
            var contract = new Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            MyWalletAccount account = new(contract.ScriptHash);
            account.SetKey(key);
            account.Contract = contract;
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
        {
            MyWalletAccount account = new(contract.ScriptHash)
            {
                Contract = contract
            };
            account.SetKey(key);
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            MyWalletAccount account = new(scriptHash);
            AddAccount(account);
            return account;
        }

        public override void Delete() { }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            return accounts.Remove(scriptHash);
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            accounts.TryGetValue(scriptHash, out WalletAccount account);
            return account;
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            return accounts.Values;
        }

        public override bool VerifyPassword(string password)
        {
            return true;
        }

        public override void Save() { }
    }

    [TestClass]
    public class UT_Wallet
    {
        private static KeyPair glkey;
        private static string nep2Key;

        [ClassInitialize]
        public static void ClassInit(TestContext ctx)
        {
            glkey = UT_Crypto.GenerateCertainKey(32);
            nep2Key = glkey.Export("pwd", TestProtocolSettings.Default.AddressVersion, 2, 1, 1);
        }

        [TestMethod]
        public void TestContains()
        {
            MyWallet wallet = new();
            try
            {
                wallet.Contains(UInt160.Zero);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestCreateAccount1()
        {
            MyWallet wallet = new();
            Assert.IsNotNull(wallet.CreateAccount(new byte[32]));
        }

        [TestMethod]
        public void TestCreateAccount2()
        {
            MyWallet wallet = new();
            Contract contract = Contract.Create([ContractParameterType.Boolean], [1]);
            WalletAccount account = wallet.CreateAccount(contract, UT_Crypto.GenerateCertainKey(32).PrivateKey);
            Assert.IsNotNull(account);

            wallet = new();
            account = wallet.CreateAccount(contract, (byte[])(null));
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestCreateAccount3()
        {
            MyWallet wallet = new();
            Contract contract = Contract.Create([ContractParameterType.Boolean], [1]);
            Assert.IsNotNull(wallet.CreateAccount(contract, glkey));
        }

        [TestMethod]
        public void TestCreateAccount4()
        {
            MyWallet wallet = new();
            Assert.IsNotNull(wallet.CreateAccount(UInt160.Zero));
        }

        [TestMethod]
        public void TestGetName()
        {
            MyWallet wallet = new();
            Assert.AreEqual("MyWallet", wallet.Name);
        }

        [TestMethod]
        public void TestGetVersion()
        {
            MyWallet wallet = new();
            Assert.AreEqual(Version.Parse("0.0.1"), wallet.Version);
        }

        [TestMethod]
        public void TestGetAccount1()
        {
            MyWallet wallet = new();
            wallet.CreateAccount(UInt160.Parse("0x7efe7ee0d3e349e085388c351955e5172605de66"));
            WalletAccount account = wallet.GetAccount(ECCurve.Secp256r1.G);
            Assert.AreEqual(UInt160.Parse("0x7efe7ee0d3e349e085388c351955e5172605de66"), account.ScriptHash);
        }

        [TestMethod]
        public void TestGetAccount2()
        {
            MyWallet wallet = new();

            try
            {
                wallet.GetAccount(UInt160.Zero);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            MyWallet wallet = new();
            try
            {
                wallet.GetAccounts();
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestGetAvailable()
        {
            MyWallet wallet = new();
            Contract contract = Contract.Create([ContractParameterType.Boolean], [1]);
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            // Fake balance
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var key = NativeContract.GAS.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

            Assert.AreEqual(new BigDecimal(new BigInteger(1000000000000M), 8), wallet.GetAvailable(snapshotCache, NativeContract.GAS.Hash));

            entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 0;
        }

        [TestMethod]
        public void TestGetBalance()
        {
            MyWallet wallet = new();
            Contract contract = Contract.Create([ContractParameterType.Boolean], [1]);
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            // Fake balance
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var key = NativeContract.GAS.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

            Assert.AreEqual(new BigDecimal(BigInteger.Zero, 0),
                wallet.GetBalance(snapshotCache, UInt160.Zero, [account.ScriptHash]));
            Assert.AreEqual(new BigDecimal(new BigInteger(1000000000000M), 8),
                wallet.GetBalance(snapshotCache, NativeContract.GAS.Hash, [account.ScriptHash]));

            entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 0;
        }

        [TestMethod]
        public void TestGetPrivateKeyFromNEP2()
        {
            Action action = () => Wallet.GetPrivateKeyFromNEP2("3vQB7B6MrGQZaxCuFg4oh", "TestGetPrivateKeyFromNEP2",
                ProtocolSettings.Default.AddressVersion, 2, 1, 1);
            Assert.ThrowsException<FormatException>(action);

            action = () => Wallet.GetPrivateKeyFromNEP2(nep2Key, "Test", ProtocolSettings.Default.AddressVersion, 2, 1, 1);
            Assert.ThrowsException<FormatException>(action);

            CollectionAssert.AreEqual("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f".HexToBytes(),
                Wallet.GetPrivateKeyFromNEP2(nep2Key, "pwd", ProtocolSettings.Default.AddressVersion, 2, 1, 1));
        }

        [TestMethod]
        public void TestGetPrivateKeyFromWIF()
        {
            Action action = () => Wallet.GetPrivateKeyFromWIF(null);
            Assert.ThrowsException<ArgumentNullException>(action);

            action = () => Wallet.GetPrivateKeyFromWIF("3vQB7B6MrGQZaxCuFg4oh");
            Assert.ThrowsException<FormatException>(action);

            CollectionAssert.AreEqual("c7134d6fd8e73d819e82755c64c93788d8db0961929e025a53363c4cc02a6962".HexToBytes(),
                Wallet.GetPrivateKeyFromWIF("L3tgppXLgdaeqSGSFw1Go3skBiy8vQAM7YMXvTHsKQtE16PBncSU"));
        }

        [TestMethod]
        public void TestImport1()
        {
            MyWallet wallet = new();
            Assert.IsNotNull(wallet.Import("L3tgppXLgdaeqSGSFw1Go3skBiy8vQAM7YMXvTHsKQtE16PBncSU"));
        }

        [TestMethod]
        public void TestImport2()
        {
            MyWallet wallet = new();
            Assert.IsNotNull(wallet.Import(nep2Key, "pwd", 2, 1, 1));
        }

        [TestMethod]
        public void TestMakeTransaction1()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            MyWallet wallet = new();
            Contract contract = Contract.Create([ContractParameterType.Boolean], [1]);
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            Action action = () => wallet.MakeTransaction(snapshotCache, [
                new()
                {
                    AssetId = NativeContract.GAS.Hash,
                    ScriptHash = account.ScriptHash,
                    Value = new BigDecimal(BigInteger.One, 8),
                    Data = "Dec 12th"
                }
            ], UInt160.Zero);
            Assert.ThrowsException<InvalidOperationException>(action);

            action = () => wallet.MakeTransaction(snapshotCache, [
                new()
                {
                    AssetId = NativeContract.GAS.Hash,
                    ScriptHash = account.ScriptHash,
                    Value = new BigDecimal(BigInteger.One, 8),
                    Data = "Dec 12th"
                }
            ], account.ScriptHash);
            Assert.ThrowsException<InvalidOperationException>(action);

            action = () => wallet.MakeTransaction(snapshotCache, [
                new()
                {
                     AssetId = UInt160.Zero,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(BigInteger.One,8),
                     Data = "Dec 12th"
                }
            ], account.ScriptHash);
            Assert.ThrowsException<InvalidOperationException>(action);

            // Fake balance
            var key = NativeContract.GAS.CreateStorageKey(20, account.ScriptHash);
            var entry1 = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry1.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

            key = NativeContract.NEO.CreateStorageKey(20, account.ScriptHash);
            var entry2 = snapshotCache.GetAndChange(key, () => new StorageItem(new NeoToken.NeoAccountState()));
            entry2.GetInteroperable<NeoToken.NeoAccountState>().Balance = 10000 * NativeContract.NEO.Factor;

            var tx = wallet.MakeTransaction(snapshotCache, [
                new()
                {
                     AssetId = NativeContract.GAS.Hash,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(BigInteger.One,8)
                }
            ]);
            Assert.IsNotNull(tx);

            tx = wallet.MakeTransaction(snapshotCache, [
                new()
                {
                     AssetId = NativeContract.NEO.Hash,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(BigInteger.One,8),
                     Data = "Dec 12th"
                }
            ]);
            Assert.IsNotNull(tx);

            entry1 = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry2 = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry1.GetInteroperable<AccountState>().Balance = 0;
            entry2.GetInteroperable<NeoToken.NeoAccountState>().Balance = 0;
        }

        [TestMethod]
        public void TestMakeTransaction2()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            MyWallet wallet = new();
            Action action = () => wallet.MakeTransaction(snapshotCache, Array.Empty<byte>(), null, null, []);
            Assert.ThrowsException<InvalidOperationException>(action);

            Contract contract = Contract.Create([ContractParameterType.Boolean], [1]);
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            // Fake balance
            var key = NativeContract.GAS.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 1000000 * NativeContract.GAS.Factor;

            var tx = wallet.MakeTransaction(snapshotCache, Array.Empty<byte>(), account.ScriptHash, [
                new()
                {
                    Account = account.ScriptHash,
                    Scopes = WitnessScope.CalledByEntry
                }
            ], []);

            Assert.IsNotNull(tx);

            tx = wallet.MakeTransaction(snapshotCache, Array.Empty<byte>(), null, null, []);
            Assert.IsNotNull(tx);

            entry = snapshotCache.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 0;
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            MyWallet wallet = new();
            try
            {
                wallet.VerifyPassword("Test");
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestSign()
        {
            MyWallet wallet = new();
            wallet.CreateAccount(glkey.PrivateKey);

            var signature = wallet.Sign([0xa, 0xb, 0xc, 0xd], glkey.PublicKey);
            Assert.IsNotNull(signature);
            Assert.AreEqual(signature.Length, 64);

            var isValid = Crypto.VerifySignature([0xa, 0xb, 0xc, 0xd], signature, glkey.PublicKey);
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void TestIndexOf()
        {
            MyWallet wallet = new();
            var index = wallet.GetMyIndex([glkey.PublicKey]);
            Assert.AreEqual(-1, index);

            wallet.CreateAccount(glkey.PrivateKey);

            index = wallet.GetMyIndex([glkey.PublicKey]);
            Assert.AreEqual(0, index);

            var key = new byte[32];
            Array.Fill(key, (byte)0x01);

            var pair = new KeyPair(key);
            index = wallet.GetMyIndex([pair.PublicKey]);
            Assert.AreEqual(-1, index);

            index = wallet.GetMyIndex([pair.PublicKey, glkey.PublicKey]);
            Assert.AreEqual(1, index);

            wallet.CreateAccount(pair.PrivateKey);
            index = wallet.GetMyIndex([pair.PublicKey, glkey.PublicKey]);
            Assert.AreEqual(0, index);
        }
    }
}
