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

namespace Neo.Json.UnitTests;

[TestClass]
public class UT_JObject
{
    private JObject _alice = null!;
    private JObject _bob = null!;

    [TestInitialize]
    public void SetUp()
    {
        _alice = new JObject()
        {
            ["name"] = "alice",
            ["age"] = 30,
            ["score"] = 100.001,
            ["gender"] = Foo.female,
            ["isMarried"] = true,
        };

        var pet1 = new JObject(new Dictionary<string, JToken?>()
        {
            ["name"] = "Tom",
            ["type"] = "cat",
        });
        _alice["pet"] = pet1;
        _bob = new JObject()
        {
            ["name"] = "bob",
            ["age"] = 100000,
            ["score"] = 0.001,
            ["gender"] = Foo.male,
            ["isMarried"] = false,
        };
        var pet2 = new JObject()
        {
            ["name"] = "Paul",
            ["type"] = "dog",
        };
        _bob["pet"] = pet2;
    }

    [TestMethod]
    public void TestAsBoolean()
    {
        Assert.IsTrue(_alice.AsBoolean());
    }

    [TestMethod]
    public void TestAsNumber()
    {
        Assert.AreEqual(double.NaN, _alice.AsNumber());
    }

    [TestMethod]
    public void TestParse()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = JToken.Parse("", -1));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("aaa"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("hello world"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("100.a"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("100.+"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("\"\\s\""));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("\"a"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("{\"k1\":\"v1\",\"k1\":\"v2\"}"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("{\"k1\",\"k1\"}"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("{\"k1\":\"v1\""));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse(new byte[] { 0x22, 0x01, 0x22 }));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("{\"color\":\"red\",\"\\uDBFF\\u0DFFF\":\"#f00\"}"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("{\"color\":\"\\uDBFF\\u0DFFF\"}"));
        Assert.ThrowsExactly<FormatException>(() => _ = JToken.Parse("\"\\uDBFF\\u0DFFF\""));

        Assert.IsNull(JToken.Parse("null"));
        Assert.IsTrue(JToken.Parse("true")!.AsBoolean());
        Assert.IsFalse(JToken.Parse("false")!.AsBoolean());
        Assert.AreEqual("hello world", JToken.Parse("\"hello world\"")!.AsString());
        Assert.AreEqual("\"\\/\b\f\n\r\t", JToken.Parse("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\"")!.AsString());
        Assert.AreEqual("0", JToken.Parse("\"\\u0030\"")!.AsString());
        Assert.AreEqual("{\"k1\":\"v1\"}", JToken.Parse("{\"k1\":\"v1\"}", 100)!.ToString());
    }

    [TestMethod]
    public void TestGetEnum()
    {
        Assert.AreEqual(Woo.Tom, _alice.AsEnum<Woo>());
        Assert.ThrowsExactly<InvalidCastException>(() => _alice.GetEnum<Woo>());
    }

    [TestMethod]
    public void TestOpImplicitEnum()
    {
        JToken obj = Woo.Tom;
        Assert.AreEqual("Tom", obj.AsString());
    }

    [TestMethod]
    public void TestOpImplicitString()
    {
        JToken? obj = null;
        Assert.IsNull(obj);

        obj = "{\"aaa\":\"111\"}";
        Assert.AreEqual("{\"aaa\":\"111\"}", obj.AsString());
    }

    [TestMethod]
    public void TestClone()
    {
        var bobClone = (JObject)_bob.Clone();
        Assert.AreNotSame(_bob, bobClone);
        foreach (var key in bobClone.Properties.Keys)
        {
            switch (_bob[key])
            {
                case JToken.Null:
                    Assert.IsNull(bobClone[key]);
                    break;
                case JObject obj:
                    CollectionAssert.AreEqual(
                        obj.Properties.ToList(),
                        ((JObject)bobClone[key]!).Properties.ToList());
                    break;
                default:
                    Assert.AreEqual(_bob[key], bobClone[key]);
                    break;
            }
        }
    }
}
