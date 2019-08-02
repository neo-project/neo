using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_CosignerUssage
    {
        [TestMethod]
        public void Serialize_Deserialize_Global()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.Global,
                ScriptHash = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }

        [TestMethod]
        public void Serialize_Deserialize_EntryOnly()
        {
            var attr = new CosignerUsage()
            {
                Scope = new WitnessScope() { Type = WitnessScopeType.EntryOnly, ScopeData = new byte[0] },
                ScriptHash = UInt160.Zero
            };

            var hex = "010000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }

        [TestMethod]
        public void Serialize_Deserialize_CustomScriptHash()
        {
            var attr = new CosignerUsage()
            {
                Scope = new WitnessScope() { Type = WitnessScopeType.CustomScriptHash, ScopeData = new byte[1] { 0x55 } },
                ScriptHash = UInt160.Zero
            };

            var hex = "0201550000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }

        [TestMethod]
        public void Serialize_Deserialize_ExecutingGroupPubKey()
        {
            var attr = new CosignerUsage()
            {
                Scope = new WitnessScope() { Type = WitnessScopeType.ExecutingGroupPubKey, ScopeData = new byte[1] { 0x55 } },
                ScriptHash = UInt160.Zero
            };

            var hex = "0401550000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }

        [TestMethod]
        public void Json_Global()
        {
            var attr = new CosignerUsage()
            {
                Scope = new WitnessScope() { Type = WitnessScopeType.Global, ScopeData = new byte[0] },
                ScriptHash = UInt160.Zero
            };

            var json = "{\"scope\":{\"type\":\"00\",\"scopeData\":\"\"},\"scriptHash\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }

        [TestMethod]
        public void Json_EntryOnly()
        {
            var attr = new CosignerUsage()
            {
                Scope = new WitnessScope() { Type = WitnessScopeType.EntryOnly, ScopeData = new byte[0] },
                ScriptHash = UInt160.Zero
            };

            var json = "{\"scope\":{\"type\":\"01\",\"scopeData\":\"\"},\"scriptHash\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }

        [TestMethod]
        public void Json_CustomScriptHash()
        {
            var attr = new CosignerUsage()
            {
                Scope = new WitnessScope() { Type = WitnessScopeType.CustomScriptHash, ScopeData = new byte[1] { 0x55 } },
                ScriptHash = UInt160.Zero
            };

            var json = "{\"scope\":{\"type\":\"02\",\"scopeData\":\"55\"},\"scriptHash\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }

        [TestMethod]
        public void Json_ExecutingGroupPubKey()
        {
            var attr = new CosignerUsage()
            {
                Scope = new WitnessScope() { Type = WitnessScopeType.ExecutingGroupPubKey, ScopeData = new byte[1] { 0x55 } },
                ScriptHash = UInt160.Zero
            };

            var json = "{\"scope\":{\"type\":\"04\",\"scopeData\":\"55\"},\"scriptHash\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope.Type, copy.Scope.Type);
            CollectionAssert.AreEqual(attr.Scope.ScopeData, copy.Scope.ScopeData);
            Assert.AreEqual(attr.ScriptHash, copy.ScriptHash);
        }
    }
}
