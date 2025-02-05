// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ContractGroup.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractGroup
    {
        [TestMethod]
        public void TestClone()
        {
            Random random = new();
            byte[] privateKey = new byte[32];
            random.NextBytes(privateKey);
            KeyPair keyPair = new(privateKey);
            ContractGroup contractGroup = new()
            {
                PubKey = keyPair.PublicKey,
                Signature = new byte[20]
            };

            ContractGroup clone = new();
            ((IInteroperable)clone).FromStackItem(contractGroup.ToStackItem(null));
            Assert.AreEqual(clone.ToJson().ToString(), contractGroup.ToJson().ToString());
        }

        [TestMethod]
        public void TestIsValid()
        {
            Random random = new();
            var privateKey = new byte[32];
            random.NextBytes(privateKey);
            KeyPair keyPair = new(privateKey);
            ContractGroup contractGroup = new()
            {
                PubKey = keyPair.PublicKey,
                Signature = new byte[20]
            };
            Assert.AreEqual(false, contractGroup.IsValid(UInt160.Zero));


            var message = new byte[] {  0x01,0x01,0x01,0x01,0x01,
                                           0x01,0x01,0x01,0x01,0x01,
                                           0x01,0x01,0x01,0x01,0x01,
                                           0x01,0x01,0x01,0x01,0x01 };
            var signature = Crypto.Sign(message, keyPair.PrivateKey);
            contractGroup = new ContractGroup
            {
                PubKey = keyPair.PublicKey,
                Signature = signature
            };
            Assert.AreEqual(true, contractGroup.IsValid(new UInt160(message)));
        }
    }
}
