using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
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
        byte[] script = { 0x01 };
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
            contract.Hash.Should().Be(script.ToScriptHash());
            // _scriptHash != null
            contract.Hash.Should().Be(script.ToScriptHash());
        }

        [TestMethod]
        public void TestIInteroperable()
        {
            IInteroperable newContract = new ContractState();
            newContract.FromStackItem(contract.ToStackItem(null));
            ((ContractState)newContract).Manifest.ToJson().ToString().Should().Be(contract.Manifest.ToJson().ToString());
            ((ContractState)newContract).Script.Span.SequenceEqual(contract.Script.Span).Should().BeTrue();
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
            json["hash"].AsString().Should().Be("0x820944cfdc70976602d71b0091445eedbc661bc5");
            json["nef"]["script"].AsString().Should().Be("AQ==");
            json["manifest"].AsString().Should().Be(manifest.ToJson().AsString());
        }
    }
}
