// Copyright (C) 2015-2025 The Neo Project.
//
// UT_VerificationContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.SmartContract;
using Neo.Wallets;
using System.Security.Cryptography;

namespace Neo.Wallets.SQLite
{
    [TestClass]
    public class UT_VerificationContract
    {
        [TestMethod]
        public void TestContractCreation()
        {
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);
            var keyPair = new KeyPair(privateKey);
            var script = SmartContract.Contract.CreateSignatureRedeemScript(keyPair.PublicKey);
            var parameters = new[] { ContractParameterType.Signature };

            var contract = new VerificationContract
            {
                Script = script,
                ParameterList = parameters
            };

            Assert.IsNotNull(contract);
            Assert.AreEqual(script, contract.Script);
            Assert.AreEqual(parameters, contract.ParameterList);
            Assert.AreEqual(script.ToScriptHash(), contract.ScriptHash);
        }

        [TestMethod]
        public void TestSerializeDeserialize()
        {
            var privateKey = new byte[32];
            RandomNumberGenerator.Fill(privateKey);

            var keyPair = new KeyPair(privateKey);
            var script = SmartContract.Contract.CreateSignatureRedeemScript(keyPair.PublicKey);
            var originalContract = new VerificationContract
            {
                Script = script,
                ParameterList = [ContractParameterType.Signature]
            };

            // Serialize
            var data = originalContract.ToArray();
            Assert.IsNotNull(data);
            Assert.IsTrue(data.Length > 0);

            // Deserialize
            var deserializedContract = data.AsSerializable<VerificationContract>();
            Assert.IsNotNull(deserializedContract);
            Assert.AreEqual(originalContract.ScriptHash, deserializedContract.ScriptHash);
            Assert.AreEqual(originalContract.Script.Length, deserializedContract.Script.Length);
            Assert.AreEqual(originalContract.ParameterList.Length, deserializedContract.ParameterList.Length);
        }
    }
}
