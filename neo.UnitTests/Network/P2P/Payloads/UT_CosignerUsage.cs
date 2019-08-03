using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_CosignerUsage
    {
        [TestMethod]
        public void Serialize_Deserialize_Global()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.Global,
                ScopeData = new byte[0],
                Account = UInt160.Zero
            };

            var hex = "000000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_CalledByEntry()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.CalledByEntry,
                ScopeData = new byte[0],
                Account = UInt160.Zero
            };

            var hex = "010000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_CustomScriptHash()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.CustomScriptHash,
                ScopeData = new byte[1] { 0x55 },
                Account = UInt160.Zero
            };

            var hex = "0201550000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Serialize_Deserialize_ExecutingGroupPubKey()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.ExecutingGroupPubKey,
                ScopeData = new byte[1] { 0x55 },
                Account = UInt160.Zero
            };

            var hex = "0401550000000000000000000000000000000000000000";
            attr.ToArray().ToHexString().Should().Be(hex);

            var copy = hex.HexToBytes().AsSerializable<CosignerUsage>();

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Json_Global()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.Global,
                ScopeData = new byte[0],
                Account = UInt160.Zero
            };

            var json = "{\"scope\":\"00\",\"scopeData\":\"\",\"account\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Json_CalledByEntry()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.CalledByEntry,
                ScopeData = new byte[0],
                Account = UInt160.Zero
            };

            var json = "{\"scope\":\"01\",\"scopeData\":\"\",\"account\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Json_CustomScriptHash()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.CustomScriptHash,
                ScopeData = new byte[1] { 0x55 },
                Account = UInt160.Zero
            };

            var json = "{\"scope\":\"02\",\"scopeData\":\"55\",\"account\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }

        [TestMethod]
        public void Json_ExecutingGroupPubKey()
        {
            var attr = new CosignerUsage()
            {
                Scope = WitnessScope.ExecutingGroupPubKey,
                ScopeData = new byte[1] { 0x55 },
                Account = UInt160.Zero
            };

            var json = "{\"scope\":\"04\",\"scopeData\":\"55\",\"account\":\"0x0000000000000000000000000000000000000000\"}";
            attr.ToJson().ToString().Should().Be(json);

            var copy = CosignerUsage.FromJson(JObject.Parse(json));

            Assert.AreEqual(attr.Scope, copy.Scope);
            CollectionAssert.AreEqual(attr.ScopeData, copy.ScopeData);
            Assert.AreEqual(attr.Account, copy.Account);
        }
    }
}
