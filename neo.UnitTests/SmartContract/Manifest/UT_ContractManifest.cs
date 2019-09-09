using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.SmartContract.Manifest;
using System.IO;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractManifest
    {
        [TestMethod]
        public void ParseFromJson_Default()
        {
            var json = @"{""groups"":[],""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Any""},""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);

            Assert.AreEqual(manifest.ToString(), json);
            Assert.AreEqual(manifest.ToString(), ContractManifest.CreateDefault(UInt160.Zero).ToString());
            Assert.IsTrue(manifest.IsValid(UInt160.Zero));
        }

        [TestMethod]
        public void ParseFromJson_Features()
        {
            var json = @"{""groups"":[],""features"":{""storage"":true,""payable"":true},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Any""},""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Features = ContractFeatures.HasStorage | ContractFeatures.Payable;
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Permissions()
        {
            var json = @"{""groups"":[],""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Any""},""methods"":[],""events"":[]},""permissions"":[{""contract"":""0x0000000000000000000000000000000000000000"",""methods"":[""method1"",""method2""]}],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Permissions = new[]
            {
                new ContractPermission()
                {
                    Contract = ContractPermissionDescriptor.Create(UInt160.Zero),
                    Methods = WildCardContainer<string>.Create("method1", "method2")
                }
            };
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_SafeMethods()
        {
            var json = @"{""groups"":[],""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Any""},""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safeMethods"":[""balanceOf""]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.SafeMethods = WildCardContainer<string>.Create("balanceOf");
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Trust()
        {
            var json = @"{""groups"":[],""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Any""},""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[""0x0000000000000000000000000000000000000001""],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Trusts = WildCardContainer<UInt160>.Create(UInt160.Parse("0x0000000000000000000000000000000000000001"));
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Groups()
        {
            var json = @"{""groups"":[{""pubKey"":""03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c"",""signature"":""41414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141""}],""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Any""},""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Groups = new ContractGroup[] { new ContractGroup() { PubKey = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1), Signature = "41414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141".HexToBytes() } };
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void TestDeserializeAndSerialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            var expected = ContractManifest.CreateDefault(UInt160.Zero);
            expected.SafeMethods = WildCardContainer<string>.Create(new string[] { "AAA" });
            expected.Serialize(writer);
            stream.Seek(0, SeekOrigin.Begin);
            var actual = ContractManifest.CreateDefault(UInt160.Zero);
            actual.Deserialize(reader);
            Assert.AreEqual(expected.SafeMethods.ToString(), actual.SafeMethods.ToString());
            Assert.AreEqual(expected.SafeMethods.Count, 1);
        }

        [TestMethod]
        public void TestGetHash()
        {
            var temp = ContractManifest.CreateDefault(UInt160.Zero);
            Assert.AreEqual(temp.Abi.Hash, temp.Hash);
        }

        [TestMethod]
        public void TestGetSize()
        {
            var temp = ContractManifest.CreateDefault(UInt160.Zero);
            Assert.AreEqual(353, temp.Size);
        }

        [TestMethod]
        public void TestGenerator()
        {
            ContractManifest contractManifest = new ContractManifest();
            Assert.IsNotNull(contractManifest);
        }

        [TestMethod]
        public void TestCanCall()
        {
            var temp = ContractManifest.CreateDefault(UInt160.Zero);
            temp.SafeMethods = WildCardContainer<string>.Create(new string[] { "AAA" });
            Assert.AreEqual(true, temp.CanCall(ContractManifest.CreateDefault(UInt160.Zero), "AAA"));
        }

        [TestMethod]
        public void TestClone()
        {
            var expected = ContractManifest.CreateDefault(UInt160.Zero);
            expected.SafeMethods = WildCardContainer<string>.Create(new string[] { "AAA" });
            var actual = expected.Clone();
            Assert.AreEqual(actual.SafeMethods.ToString(), expected.SafeMethods.ToString());
        }
    }
}
