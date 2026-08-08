// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Contract_CreateByHash.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Security.Cryptography;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Coverage for Contract.Create(UInt160) and multisig parameter validation
    /// not duplicated from UT_Contract multi-sig script layout tests.
    /// </summary>
    [TestClass]
    public class UT_Contract_CreateByHash
    {
        [TestMethod]
        public void Create_ByScriptHash_UsesEmptyScriptAndPreservesHash()
        {
            var hash = UInt160.Parse("0xd2a4cff31913016155e38e474a2c06d08be276cf");
            var contract = Contract.Create(hash, ContractParameterType.Signature, ContractParameterType.Boolean);

            Assert.IsEmpty(contract.Script);
            Assert.AreEqual(hash, contract.ScriptHash);
            Assert.HasCount(2, contract.ParameterList);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
            Assert.AreEqual(ContractParameterType.Boolean, contract.ParameterList[1]);
        }

        [TestMethod]
        public void CreateMultiSigRedeemScript_InvalidM_Throws()
        {
            var keys = new[]
            {
                NewKey().PublicKey,
                NewKey().PublicKey
            };

            Assert.ThrowsExactly<ArgumentException>(() => Contract.CreateMultiSigRedeemScript(0, keys));
            Assert.ThrowsExactly<ArgumentException>(() => Contract.CreateMultiSigRedeemScript(3, keys));
            Assert.ThrowsExactly<ArgumentException>(() => Contract.CreateMultiSigContract(0, keys));
        }

        [TestMethod]
        public void GetBFTAddress_IsDeterministic()
        {
            var keys = new[]
            {
                NewKey().PublicKey,
                NewKey().PublicKey,
                NewKey().PublicKey,
                NewKey().PublicKey
            };

            var a = Contract.GetBFTAddress(keys);
            var b = Contract.GetBFTAddress(keys);
            Assert.AreEqual(a, b);
            Assert.AreNotEqual(UInt160.Zero, a);
        }

        private static KeyPair NewKey()
        {
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            return new KeyPair(privateKey);
        }
    }
}
