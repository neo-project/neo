// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ContractPermissionDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using System.Security.Cryptography;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractPermissionDescriptor
    {
        [TestMethod]
        public void TestCreateByECPointAndIsWildcard()
        {
            var privateKey = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            var key = new KeyPair(privateKey);
            var contractPermissionDescriptor = ContractPermissionDescriptor.Create(key.PublicKey);
            Assert.IsNotNull(contractPermissionDescriptor);
            Assert.AreEqual(key.PublicKey, contractPermissionDescriptor.Group);
            Assert.AreEqual(false, contractPermissionDescriptor.IsWildcard);
        }

        [TestMethod]
        public void TestFromAndToJson()
        {
            var privateKey = new byte[32];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            var key = new KeyPair(privateKey);
            var temp = ContractPermissionDescriptor.Create(key.PublicKey);
            var result = ContractPermissionDescriptor.FromJson(temp.ToJson());
            Assert.AreEqual(null, result.Hash);
            Assert.AreEqual(result.Group, result.Group);
        }
    }
}
