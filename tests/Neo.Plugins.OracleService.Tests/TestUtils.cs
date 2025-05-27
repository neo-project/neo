// Copyright (C) 2015-2025 The Neo Project.
//
// TestUtils.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;

namespace Neo.Plugins.OracleService.Tests
{
    public static class TestUtils
    {
        public static readonly ProtocolSettings settings = ProtocolSettings.Load("config.json");
        public static readonly byte[] ValidatorScript = Contract.CreateSignatureRedeemScript(settings.StandbyCommittee[0]);
        public static readonly UInt160 ValidatorScriptHash = ValidatorScript.ToScriptHash();
        public static readonly string ValidatorAddress = ValidatorScriptHash.ToAddress(ProtocolSettings.Default.AddressVersion);
        public static readonly byte[] MultisigScript = Contract.CreateMultiSigRedeemScript(1, settings.StandbyCommittee);
        public static readonly UInt160 MultisigScriptHash = MultisigScript.ToScriptHash();
        public static readonly string MultisigAddress = MultisigScriptHash.ToAddress(ProtocolSettings.Default.AddressVersion);

        public static StorageKey CreateStorageKey(this NativeContract contract, byte prefix, ISerializableSpan key)
        {
            var k = new KeyBuilder(contract.Id, prefix);
            if (key != null) k = k.Add(key);
            return k;
        }

        public static StorageKey CreateStorageKey(this NativeContract contract, byte prefix, uint value)
        {
            return new KeyBuilder(contract.Id, prefix).AddBigEndian(value);
        }

        public static NEP6Wallet GenerateTestWallet(string password)
        {
            JObject wallet = new JObject();
            wallet["name"] = "noname";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = null;
            Assert.AreEqual("{\"name\":\"noname\",\"version\":\"1.0\",\"scrypt\":{\"n\":2,\"r\":1,\"p\":1},\"accounts\":[],\"extra\":null}", wallet.ToString());
            return new NEP6Wallet(null, password, settings, wallet);
        }
    }
}
