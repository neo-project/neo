// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TestWalletModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Models;
using Neo.Build.Core.Models.Wallets;
using Neo.Build.Core.Tests.Helpers;
using System.Linq;

namespace Neo.Build.Core.Tests.Models.Wallets
{
    [TestClass]
    public class UT_TestWalletModel
    {
        [TestMethod]
        public void PropertiesAndSubPropertiesWithoutExtraProperties()
        {
            var jsonTestString = "{\"name\":\"Unit Test Wallet\",\"version\":\"1.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[{\"address\":\"NhpxKrsHrFCizcupug2pTNkM7TnhyMzwEa\",\"label\":\"Main Test Account\",\"isDefault\":false,\"lock\":false,\"key\":\"3889c6201680d433ffdccb94a6b01c09863f5dc83aa85a003ae4dfe9460cf33f\",\"contract\":{\"script\":\"0c21028cd8520a4379f8bf84734fdc8063cc810932ae5f15d9d76362d7af35ca8371a84156e7b327\",\"parameters\":[{\"name\":\"Signature\",\"type\":\"Signature\"}],\"deployed\":false},\"extra\":null}],\"extra\":null}";

            var expectedTestWalletModel = TestObjectHelper.CreateTestWalletModel();

            var actualTestWalletModel = JsonModel.FromJson<TestWalletModel>(jsonTestString, TestDefaults.JsonDefaultSerializerOptions);

            Assert.IsNotNull(actualTestWalletModel);

            Assert.IsNotNull(actualTestWalletModel.Name);
            Assert.AreEqual(expectedTestWalletModel.Name, actualTestWalletModel.Name);

            Assert.IsNotNull(actualTestWalletModel.Version);
            Assert.AreEqual(expectedTestWalletModel.Version, actualTestWalletModel.Version);

            Assert.IsNotNull(actualTestWalletModel.Scrypt);
            Assert.AreEqual(expectedTestWalletModel.Scrypt!.N, actualTestWalletModel.Scrypt.N);
            Assert.AreEqual(expectedTestWalletModel.Scrypt!.R, actualTestWalletModel.Scrypt.R);
            Assert.AreEqual(expectedTestWalletModel.Scrypt!.P, actualTestWalletModel.Scrypt.P);

            Assert.IsNotNull(actualTestWalletModel.Accounts);
            Assert.AreEqual(1, actualTestWalletModel.Accounts.Count);

            var expectedTestWalletAccountModel = expectedTestWalletModel.Accounts!.Single();
            var actualTestWalletAccountModel = actualTestWalletModel.Accounts.SingleOrDefault();

            Assert.IsNotNull(actualTestWalletAccountModel);

            Assert.IsNotNull(actualTestWalletAccountModel.Address);
            Assert.AreEqual(expectedTestWalletAccountModel.Address, actualTestWalletAccountModel.Address);

            Assert.IsNotNull(actualTestWalletAccountModel.Label);
            Assert.AreEqual(expectedTestWalletAccountModel.Label, actualTestWalletAccountModel.Label);
            Assert.AreEqual(expectedTestWalletAccountModel.IsDefault, actualTestWalletAccountModel.IsDefault);
            Assert.AreEqual(expectedTestWalletAccountModel.Lock, actualTestWalletAccountModel.Lock);

            Assert.IsNotNull(actualTestWalletAccountModel.Key);
            CollectionAssert.AreEqual(expectedTestWalletAccountModel.Key!.PrivateKey, actualTestWalletAccountModel.Key.PrivateKey);
            Assert.AreEqual(expectedTestWalletAccountModel.Key!.PublicKey, actualTestWalletAccountModel.Key.PublicKey);
            Assert.AreEqual(expectedTestWalletAccountModel.Key!.PublicKeyHash, actualTestWalletAccountModel.Key.PublicKeyHash);

            Assert.IsNotNull(actualTestWalletAccountModel.Contract);
            CollectionAssert.AreEqual(expectedTestWalletAccountModel.Contract!.Script, actualTestWalletAccountModel.Contract.Script);

            Assert.IsNotNull(actualTestWalletAccountModel.Contract.Parameters);
            Assert.AreEqual(1, actualTestWalletAccountModel.Contract.Parameters.Count);

            var expectedContractParametersModel = expectedTestWalletAccountModel.Contract.Parameters!.Single();
            var actualContractParametersModel = actualTestWalletAccountModel.Contract.Parameters.SingleOrDefault();

            Assert.IsNotNull(actualContractParametersModel);

            Assert.IsNotNull(actualContractParametersModel.Name);
            Assert.AreEqual(expectedContractParametersModel.Name, actualContractParametersModel.Name);
            Assert.AreEqual(expectedContractParametersModel.Type, actualContractParametersModel.Type);
        }
    }
}
