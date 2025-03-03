// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ContractState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContractState
    {
        ContractState contract;
        readonly byte[] script = { 0x01 };
        ContractManifest manifest;

        [TestInitialize]
        public void TestSetup()
        {
            manifest = TestUtils.CreateDefaultManifest();
            contract = new ContractState
            {
                Nef = new NefFile
                {
                    Compiler = nameof(ScriptBuilder),
                    Source = string.Empty,
                    Tokens = Array.Empty<MethodToken>(),
                    Script = script
                },
                Hash = script.ToScriptHash(),
                Manifest = manifest
            };
            contract.Nef.CheckSum = NefFile.ComputeChecksum(contract.Nef);
        }

        [TestMethod]
        public void TestGetScriptHash()
        {
            // _scriptHash == null
            Assert.AreEqual(script.ToScriptHash(), contract.Hash);
            // _scriptHash != null
            Assert.AreEqual(script.ToScriptHash(), contract.Hash);
        }

        [TestMethod]
        public void TestIInteroperable()
        {
            IInteroperable newContract = new ContractState();
            newContract.FromStackItem(contract.ToStackItem(null));
            Assert.AreEqual(contract.Manifest.ToJson().ToString(), ((ContractState)newContract).Manifest.ToJson().ToString());
            Assert.IsTrue(((ContractState)newContract).Script.Span.SequenceEqual(contract.Script.Span));
        }

        [TestMethod]
        public void TestCanCall()
        {
            var temp = new ContractState() { Manifest = TestUtils.CreateDefaultManifest() };

            Assert.AreEqual(true, temp.CanCall(new ContractState() { Hash = UInt160.Zero, Manifest = TestUtils.CreateDefaultManifest() }, "AAA"));
        }

        [TestMethod]
        public void TestToJson()
        {
            JObject json = contract.ToJson();
            Assert.AreEqual("0x820944cfdc70976602d71b0091445eedbc661bc5", json["hash"].AsString());
            Assert.AreEqual("AQ==", json["nef"]["script"].AsString());
            Assert.AreEqual(manifest.ToJson().AsString(), json["manifest"].AsString());
        }
    }
}
