using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractManifest
    {
        [TestMethod]
        public void ParseFromJson_Default()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[{""name"":""testMethod"",""parameters"":[],""returntype"":""Void"",""offset"":0,""safe"":true}],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);

            Assert.AreEqual(manifest.ToJson().ToString(), json);
            Assert.AreEqual(manifest.ToJson().ToString(), TestUtils.CreateDefaultManifest().ToJson().ToString());
            Assert.IsTrue(manifest.IsValid(UInt160.Zero));
        }

        [TestMethod]
        public void ParseFromJson_Permissions()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[{""name"":""testMethod"",""parameters"":[],""returntype"":""Void"",""offset"":0,""safe"":true}],""events"":[]},""permissions"":[{""contract"":""0x0000000000000000000000000000000000000000"",""methods"":[""method1"",""method2""]}],""trusts"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Permissions = new[]
            {
                new ContractPermission()
                {
                    Contract = ContractPermissionDescriptor.Create(UInt160.Zero),
                    Methods = WildcardContainer<string>.Create("method1", "method2")
                }
            };
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void ParseFromJson_SafeMethods()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[{""name"":""testMethod"",""parameters"":[],""returntype"":""Void"",""offset"":0,""safe"":true}],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void ParseFromJson_Trust()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[{""name"":""testMethod"",""parameters"":[],""returntype"":""Void"",""offset"":0,""safe"":true}],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[""0x0000000000000000000000000000000000000001""],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Trusts = WildcardContainer<UInt160>.Create(UInt160.Parse("0x0000000000000000000000000000000000000001"));
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void ParseFromJson_Groups()
        {
            var json = @"{""name"":""testManifest"",""groups"":[{""pubkey"":""03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c"",""signature"":""QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQQ==""}],""supportedstandards"":[],""abi"":{""methods"":[{""name"":""testMethod"",""parameters"":[],""returntype"":""Void"",""offset"":0,""safe"":true}],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToJson().ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Groups = new ContractGroup[] { new ContractGroup() { PubKey = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1), Signature = "41414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141".HexToBytes() } };
            Assert.AreEqual(manifest.ToJson().ToString(), check.ToJson().ToString());
        }

        [TestMethod]
        public void ParseFromJson_Extra()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""methods"":[{""name"":""testMethod"",""parameters"":[],""returntype"":""Void"",""offset"":0,""safe"":true}],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""extra"":{""key"":""value""}}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(json, json);
            Assert.AreEqual("value", manifest.Extra["key"].AsString(), false);
        }

        [TestMethod]
        public void TestDeserializeAndSerialize()
        {
            var expected = TestUtils.CreateDefaultManifest();
            expected.Extra = JObject.Parse(@"{""a"":123}");

            var clone = new ContractManifest();
            ((IInteroperable)clone).FromStackItem(expected.ToStackItem(null));

            Assert.AreEqual(expected.Extra.ToString(), @"{""a"":123}");
            Assert.AreEqual(expected.ToString(), clone.ToString());

            expected.Extra = null;
            clone = new ContractManifest();
            ((IInteroperable)clone).FromStackItem(expected.ToStackItem(null));

            Assert.AreEqual(expected.Extra, clone.Extra);
            Assert.AreEqual(expected.ToString(), clone.ToString());
        }

        [TestMethod]
        public void TestGenerator()
        {
            ContractManifest contractManifest = new ContractManifest();
            Assert.IsNotNull(contractManifest);
        }
    }
}
