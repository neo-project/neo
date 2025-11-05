// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JObject.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable JSON001

using System.Text.Json.Nodes;

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JObject
    {
        private JsonObject _alice;
        private JsonObject _bob;

        [TestInitialize]
        public void SetUp()
        {
            _alice = new JsonObject()
            {
                ["name"] = "alice",
                ["age"] = 30,
                ["score"] = 100.001,
                ["gender"] = Foo.female.ToString(),
                ["isMarried"] = true,
            };

            var pet1 = new JsonObject(new Dictionary<string, JsonNode>()
            {
                ["name"] = "Tom",
                ["type"] = "cat",
            });
            _alice["pet"] = pet1;
            _bob = new JsonObject()
            {
                ["name"] = "bob",
                ["age"] = 100000,
                ["score"] = 0.001,
                ["gender"] = Foo.male.ToString(),
                ["isMarried"] = false,
            };
            var pet2 = new JsonObject()
            {
                ["name"] = "Paul",
                ["type"] = "dog",
            };
            _bob["pet"] = pet2;
        }

        [TestMethod]
        public void TestParse()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = JsonNode.Parse("", documentOptions: new() { MaxDepth = -1 }));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("aaa"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("hello world"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("100.a"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("100.+"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("\"\\s\""));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("\"a"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("{\"k1\":\"v1\",\"k1\":\"v2\"}"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("{\"k1\",\"k1\"}"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("{\"k1\":\"v1\""));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(new byte[] { 0x22, 0x01, 0x22 }));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("{\"color\":\"red\",\"\\uDBFF\\u0DFFF\":\"#f00\"}"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("{\"color\":\"\\uDBFF\\u0DFFF\"}"));
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse("\"\\uDBFF\\u0DFFF\""));

            Assert.IsNull(JsonNode.Parse("null"));
            Assert.IsTrue(JsonNode.Parse("true").GetValue<bool>());
            Assert.IsFalse(JsonNode.Parse("false").GetValue<bool>());
            Assert.AreEqual("hello world", JsonNode.Parse("\"hello world\"").AsString());
            Assert.AreEqual("\"\\/\b\f\n\r\t", JsonNode.Parse("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\"").AsString());
            Assert.AreEqual("0", JsonNode.Parse("\"\\u0030\"").AsString());
            Assert.AreEqual("{\"k1\":\"v1\"}", JsonNode.Parse("{\"k1\":\"v1\"}", documentOptions: new() { MaxDepth = 100 }).ToString(false));
        }

        [TestMethod]
        public void TestGetEnum()
        {
            Action action = () => _alice.GetEnum<Woo>();
            Assert.ThrowsExactly<InvalidCastException>(action);
        }

        [TestMethod]
        public void TestOpImplicitString()
        {
            JsonNode obj = null;
            Assert.IsNull(obj);

            obj = "{\"aaa\":\"111\"}";
            Assert.AreEqual("{\"aaa\":\"111\"}", obj.AsString());
        }

        [TestMethod]
        public void TestClone()
        {
            var bobClone = (JsonObject)_bob.DeepClone();
            Assert.AreNotSame(_bob, bobClone);
            foreach (var (key, clonedValue) in bobClone)
            {
                switch (_bob[key])
                {
                    case null:
                        Assert.IsNull(clonedValue);
                        break;
                    case JsonObject obj:
                        CollectionAssert.AreEqual(
                            ((JsonObject)_bob[key]).ToList(),
                            ((JsonObject)clonedValue).ToList());
                        break;
                    default:
                        Assert.AreEqual(_bob[key], bobClone[key]);
                        break;
                }
            }
        }
    }
}
