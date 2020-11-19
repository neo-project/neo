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
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safemethods"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);

            Assert.AreEqual(manifest.ToString(), json);
            Assert.AreEqual(manifest.ToString(), TestUtils.CreateDefaultManifest().ToString());
            Assert.IsTrue(manifest.IsValid(UInt160.Zero));
        }

        [TestMethod]
        public void ParseFromJson_Permissions()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[],""events"":[]},""permissions"":[{""contract"":""0x0000000000000000000000000000000000000000"",""methods"":[""method1"",""method2""]}],""trusts"":[],""safemethods"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Permissions = new[]
            {
                new ContractPermission()
                {
                    Contract = ContractPermissionDescriptor.Create(UInt160.Zero),
                    Methods = WildcardContainer<string>.Create("method1", "method2")
                }
            };
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_SafeMethods()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safemethods"":[""balanceOf""],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.SafeMethods = WildcardContainer<string>.Create("balanceOf");
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Trust()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[""0x0000000000000000000000000000000000000001""],""safemethods"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Trusts = WildcardContainer<UInt160>.Create(UInt160.Parse("0x0000000000000000000000000000000000000001"));
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Groups()
        {
            var json = @"{""name"":""testManifest"",""groups"":[{""pubkey"":""03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c"",""signature"":""QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQQ==""}],""supportedstandards"":[],""abi"":{""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safemethods"":[],""extra"":null}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(manifest.ToString(), json);

            var check = TestUtils.CreateDefaultManifest();
            check.Groups = new ContractGroup[] { new ContractGroup() { PubKey = ECPoint.Parse("03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c", ECCurve.Secp256r1), Signature = "41414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141".HexToBytes() } };
            Assert.AreEqual(manifest.ToString(), check.ToString());
        }

        [TestMethod]
        public void ParseFromJson_Extra()
        {
            var json = @"{""name"":""testManifest"",""groups"":[],""supportedstandards"":[],""abi"":{""hash"":""0x0000000000000000000000000000000000000000"",""methods"":[],""events"":[]},""permissions"":[{""contract"":""*"",""methods"":""*""}],""trusts"":[],""safemethods"":[],""extra"":{""key"":""value""}}";
            var manifest = ContractManifest.Parse(json);
            Assert.AreEqual(json, json);
            Assert.AreEqual("value", manifest.Extra["key"].AsString(), false);
        }

        [TestMethod]
        public void TestDeserializeAndSerialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            var expected = TestUtils.CreateDefaultManifest();
            expected.SafeMethods = WildcardContainer<string>.Create(new string[] { "AAA" });
            expected.Serialize(writer);
            stream.Seek(0, SeekOrigin.Begin);
            var actual = TestUtils.CreateDefaultManifest();
            actual.Deserialize(reader);
            Assert.AreEqual(expected.SafeMethods.ToString(), actual.SafeMethods.ToString());
            Assert.AreEqual(expected.SafeMethods.Count, 1);
        }

        [TestMethod]
        public void TestGetSize()
        {
            var temp = TestUtils.CreateDefaultManifest();
            Assert.AreEqual(182, temp.Size);
        }

        [TestMethod]
        public void TestGenerator()
        {
            ContractManifest contractManifest = new ContractManifest();
            Assert.IsNotNull(contractManifest);
        }

        [TestMethod]
        public void TestClone()
        {
            var expected = TestUtils.CreateDefaultManifest();
            expected.SafeMethods = WildcardContainer<string>.Create(new string[] { "AAA" });
            var actual = expected.Clone();
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }
    }
}
