using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using System.IO;

namespace Neo.UnitTests.Ledger
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
            manifest = TestUtils.CreateDefaultManifest(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"));
            contract = new ContractState
            {
                Script = script,
                Manifest = manifest
            };
        }

        [TestMethod]
        public void TestGetScriptHash()
        {
            // _scriptHash == null
            contract.ScriptHash.Should().Be(script.ToScriptHash());
            // _scriptHash != null
            contract.ScriptHash.Should().Be(script.ToScriptHash());
        }

        [TestMethod]
        public void TestClone()
        {
            ICloneable<ContractState> cloneable = contract;
            ContractState clone = cloneable.Clone();
            clone.ToJson().ToString().Should().Be(contract.ToJson().ToString());
        }

        [TestMethod]
        public void TestFromReplica()
        {
            ICloneable<ContractState> cloneable = new ContractState();
            cloneable.FromReplica(contract);
            ((ContractState)cloneable).ToJson().ToString().Should().Be(contract.ToJson().ToString());
        }

        [TestMethod]
        public void TestDeserialize()
        {
            ISerializable newContract = new ContractState();
            using (MemoryStream ms = new MemoryStream(1024))
            using (BinaryWriter writer = new BinaryWriter(ms))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((ISerializable)contract).Serialize(writer);
                ms.Seek(0, SeekOrigin.Begin);
                newContract.Deserialize(reader);
            }
            ((ContractState)newContract).Manifest.ToJson().ToString().Should().Be(contract.Manifest.ToJson().ToString());
            ((ContractState)newContract).Script.Should().BeEquivalentTo(contract.Script);
        }

        [TestMethod]
        public void TestGetSize()
        {
            ISerializable newContract = contract;
            newContract.Size.Should().Be(240);
        }

        [TestMethod]
        public void TestToJson()
        {
            JObject json = contract.ToJson();
            json["hash"].AsString().Should().Be("0x820944cfdc70976602d71b0091445eedbc661bc5");
            json["script"].AsString().Should().Be("AQ==");
            json["manifest"].AsString().Should().Be(manifest.ToJson().AsString());
        }

        [TestMethod]
        public void TestToJson2()
        {
            ContractState neocontract = contract;
            neocontract.Id = -1;
            JObject json = neocontract.ToJson();
            json["id"].AsString().Should().Be("-1");
            json["name"].AsString().Should().Be("NEO");
        }
    }
}
