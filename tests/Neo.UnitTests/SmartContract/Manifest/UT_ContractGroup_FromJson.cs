// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractGroup_FromJson.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions.Factories;
using Neo.Json;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using System;

namespace Neo.UnitTests.SmartContract.Manifest
{
    /// <summary>
    /// FromJson coverage not covered by UT_ContractGroup clone/IsValid tests.
    /// </summary>
    [TestClass]
    public class UT_ContractGroup_FromJson
    {
        [TestMethod]
        public void FromJson_ToJson_RoundTrip()
        {
            var key = new KeyPair(RandomNumberFactory.NextBytes(32));
            var group = new ContractGroup
            {
                PubKey = key.PublicKey,
                Signature = new byte[64]
            };

            var json = group.ToJson();
            var clone = ContractGroup.FromJson(json);
            Assert.AreEqual(group.PubKey, clone.PubKey);
            CollectionAssert.AreEqual(group.Signature, clone.Signature);
        }

        [TestMethod]
        public void FromJson_InvalidSignatureLength_Throws()
        {
            var key = new KeyPair(RandomNumberFactory.NextBytes(32));
            var json = new JObject
            {
                ["pubkey"] = key.PublicKey.ToString(),
                ["signature"] = Convert.ToBase64String(new byte[20])
            };
            Assert.ThrowsExactly<FormatException>(() => ContractGroup.FromJson(json));
        }
    }
}
