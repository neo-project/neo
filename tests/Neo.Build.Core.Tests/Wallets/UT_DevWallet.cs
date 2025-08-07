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
using Neo.SmartContract;
using System.IO;
using System.Security.Cryptography;

namespace Neo.Build.Core.Tests.Wallets
{
    [TestClass]
    public class UT_DevWallet
    {
        [TestMethod]
        public void TestCreateAccountWithPrivateKey()
        {
            var testWalletModel = TestObjectHelper.CreateTestWalletModel()!;
            var devWallet = new DevWallet(testWalletModel, ProtocolSettings.Default);

            var expectedPrivateKey = RandomNumberGenerator.GetBytes(32);
            var expectedDevWalletAccount = devWallet.CreateAccount(expectedPrivateKey);

            Assert.IsTrue(devWallet.Contains(expectedDevWalletAccount!.ScriptHash));

            var actualDevWalletAccount = devWallet.GetAccount(expectedDevWalletAccount!.ScriptHash);

            Assert.IsNotNull(actualDevWalletAccount);
            Assert.IsInstanceOfType<DevWalletAccount>(actualDevWalletAccount);
            Assert.IsTrue(actualDevWalletAccount.HasKey);
            CollectionAssert.AreEqual(expectedPrivateKey, actualDevWalletAccount.GetKey().PrivateKey);
        }

        [TestMethod]
        public void TestOpenAndSaveFile()
        {
            var walletModel = TestObjectHelper.CreateTestWalletModel()!;
            var devWallet = new DevWallet(walletModel);

            var expectedFilename = "devwallet.json";
            var expectedPrivateKey = RandomNumberGenerator.GetBytes(32);
            var expectedDevWalletAccount = devWallet.CreateAccount(expectedPrivateKey);
            devWallet.Save(expectedFilename);

            Assert.IsTrue(devWallet.Contains(expectedDevWalletAccount!.ScriptHash));
            Assert.IsTrue(File.Exists(expectedFilename));

            devWallet = new DevWallet(expectedFilename, walletModel.Extra!.ProtocolConfiguration!.ToObject());

            var actualDevWalletAccount = devWallet.GetAccount(expectedDevWalletAccount!.ScriptHash);

            Assert.IsNotNull(actualDevWalletAccount);
            Assert.IsInstanceOfType<DevWalletAccount>(actualDevWalletAccount);
            Assert.IsTrue(actualDevWalletAccount.HasKey);
            CollectionAssert.AreEqual(expectedPrivateKey, actualDevWalletAccount.GetKey().PrivateKey);

            File.Delete(expectedFilename);
        }

        [TestMethod]
        public void TestCreateGetDeleteAccount()
        {
            var expectedWallet = new DevWallet();
            var expectedPrivateKey = RandomNumberGenerator.GetBytes(32);
            var expectedAccountName = "bob";

            var expectedWalletAccount = expectedWallet.CreateAccount(expectedPrivateKey, expectedAccountName);

            Assert.IsNotNull(expectedWalletAccount);

            var actualWalletAccount = expectedWallet.GetAccount("bob");

            Assert.IsNotNull(actualWalletAccount);
            Assert.AreEqual(expectedWalletAccount.ScriptHash, actualWalletAccount.ScriptHash);

            Assert.IsTrue(expectedWallet.DeleteAccount(expectedAccountName));
        }

        [TestMethod]
        public void TestCreateMultiSigAccount()
        {
            var expectedWallet = new DevWallet();
            var expectedPrivateKey = RandomNumberGenerator.GetBytes(32);
            var expectedAccountName = "bob";

            var expectedWalletAccount = expectedWallet.CreateAccount(expectedPrivateKey, expectedAccountName);
            var expectedAccountKey = expectedWalletAccount.GetKey();
            var actualMultiSigAccount = expectedWallet.CreateMultiSigAccount([expectedAccountKey.PublicKey]);

            Assert.IsNotNull(actualMultiSigAccount);
            Assert.AreNotEqual(expectedWalletAccount.ScriptHash, actualMultiSigAccount.ScriptHash);
            Assert.AreEqual(expectedAccountKey.PublicKey, actualMultiSigAccount.GetKey().PublicKey);
            Assert.IsTrue(Helper.IsMultiSigContract(actualMultiSigAccount.Contract.Script));
        }

        // TODO: Add more tests for this class
    }
}
