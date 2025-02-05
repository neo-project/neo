// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JObject.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json.UnitTests
{
    [TestClass]
    public class UT_JObject
    {
        private JObject alice;
        private JObject bob;

        [TestInitialize]
        public void SetUp()
        {
            alice = new JObject();
            alice["name"] = "alice";
            alice["age"] = 30;
            alice["score"] = 100.001;
            alice["gender"] = Foo.female;
            alice["isMarried"] = true;
            var pet1 = new JObject();
            pet1["name"] = "Tom";
            pet1["type"] = "cat";
            alice["pet"] = pet1;

            bob = new JObject();
            bob["name"] = "bob";
            bob["age"] = 100000;
            bob["score"] = 0.001;
            bob["gender"] = Foo.male;
            bob["isMarried"] = false;
            var pet2 = new JObject();
            pet2["name"] = "Paul";
            pet2["type"] = "dog";
            bob["pet"] = pet2;
        }

        [TestMethod]
        public void TestAsBoolean()
        {
            Assert.IsTrue(alice.AsBoolean());
        }

        [TestMethod]
        public void TestAsNumber()
        {
            Assert.AreEqual(double.NaN, alice.AsNumber());
        }

        [TestMethod]
        public void TestParse()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => JObject.Parse("", -1));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("aaa"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("hello world"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("100.a"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("100.+"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("\"\\s\""));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("\"a"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("{\"k1\":\"v1\",\"k1\":\"v2\"}"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("{\"k1\",\"k1\"}"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("{\"k1\":\"v1\""));
            Assert.ThrowsException<FormatException>(() => JObject.Parse(new byte[] { 0x22, 0x01, 0x22 }));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("{\"color\":\"red\",\"\\uDBFF\\u0DFFF\":\"#f00\"}"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("{\"color\":\"\\uDBFF\\u0DFFF\"}"));
            Assert.ThrowsException<FormatException>(() => JObject.Parse("\"\\uDBFF\\u0DFFF\""));

            Assert.IsNull(JObject.Parse("null"));
            Assert.IsTrue(JObject.Parse("true").AsBoolean());
            Assert.IsFalse(JObject.Parse("false").AsBoolean());
            Assert.AreEqual("hello world", JObject.Parse("\"hello world\"").AsString());
            Assert.AreEqual("\"\\/\b\f\n\r\t", JObject.Parse("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\"").AsString());
            Assert.AreEqual("0", JObject.Parse("\"\\u0030\"").AsString());
            Assert.AreEqual("{\"k1\":\"v1\"}", JObject.Parse("{\"k1\":\"v1\"}", 100).ToString());
        }

        [TestMethod]
        public void TestGetEnum()
        {
            Assert.AreEqual(Woo.Tom, alice.AsEnum<Woo>());

            Action action = () => alice.GetEnum<Woo>();
            Assert.ThrowsException<InvalidCastException>(action);
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
            JToken obj = null;
            Assert.IsNull(obj);

            obj = "{\"aaa\":\"111\"}";
            Assert.AreEqual("{\"aaa\":\"111\"}", obj.AsString());
        }

        [TestMethod]
        public void TestGetNull()
        {
            Assert.IsNull(JToken.Null);
        }

        [TestMethod]
        public void TestClone()
        {
            var bobClone = (JObject)bob.Clone();
            Assert.AreNotSame(bob, bobClone);
            foreach (var key in bobClone.Properties.Keys)
            {
                switch (bob[key])
                {
                    case JToken.Null:
                        Assert.IsNull(bobClone[key]);
                        break;
                    case JObject obj:
                        CollectionAssert.AreEqual(
                            ((JObject)bob[key]).Properties.ToList(),
                            ((JObject)bobClone[key]).Properties.ToList());
                        break;
                    default:
                        Assert.AreEqual(bob[key], bobClone[key]);
                        break;
                }
            }
        }
    }
}
