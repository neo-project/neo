using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.SmartContract.Manifest;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ContractManifest
    {
        [TestMethod]
        public void ParseFromJson_Default()
        {
            var json = @"{""groups"":null,""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Array""},""methods"":[],""events"":[]},""permissions"":[],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);

            Assert.AreEqual(manifest.ToString(), json);
            Assert.AreEqual(manifest.ToString(), ContractManifest.CreateDefault(UInt160.Zero).ToString());
            Assert.IsTrue(manifest.IsValid());
        }

        [TestMethod]
        public void ParseFromJson_Features()
        {
            var json = @"{""groups"":null,""features"":{""storage"":true,""payable"":true},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Array""},""methods"":[],""events"":[]},""permissions"":[],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Features = ContractFeatures.HasStorage | ContractFeatures.Payable;
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Permissions()
        {
            var json = @"{""groups"":null,""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Array""},""methods"":[],""events"":[]},""permissions"":[{""contract"":""0x0000000000000000000000000000000000000000"",""methods"":[""method1"",""method2""]}],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Permissions = new WildCardContainer<ContractPermission>(new ContractPermission()
            {
                Contract = UInt160.Zero,
                Methods = new WildCardContainer<string>("method1", "method2")
            });
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_SafeMethods()
        {
            var json = @"{""groups"":null,""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Array""},""methods"":[],""events"":[]},""permissions"":[],""trusts"":[],""safeMethods"":[""balanceOf""]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.SafeMethods = new WildCardContainer<string>("balanceOf");
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Trust()
        {
            var json = @"{""groups"":null,""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Array""},""methods"":[],""events"":[]},""permissions"":[],""trusts"":[""0x0000000000000000000000000000000000000001""],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Trusts = new WildCardContainer<UInt160>(UInt160.Parse("0x0000000000000000000000000000000000000001"));
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Groups()
        {
            var json = @"{""groups"":[{""pubKey"":""03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c"",""signature"":""41414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141""}],""features"":{""storage"":false,""payable"":false},""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""entryPoint"":{""name"":""Main"",""parameters"":[{""name"":""operation"",""type"":""String""},{""name"":""args"",""type"":""Array""}],""returnType"":""Array""},""methods"":[],""events"":[]},""permissions"":[],""trusts"":[],""safeMethods"":[]}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = ContractManifest.CreateDefault(UInt160.Zero);
            check.Groups = new ContractGroup[] { new ContractGroup() { PubKey = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1), Signature = "41414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141".HexToBytes() } };
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }
    }
}