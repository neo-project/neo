using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using System;

namespace Neo.UnitTests.IO.Json
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
            alice.AsBoolean().Should().BeTrue();
        }

        [TestMethod]
        public void TestAsNumber()
        {
            alice.AsNumber().Should().Be(double.NaN);
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

            JObject.Parse("null").Should().BeNull();
            JObject.Parse("true").AsBoolean().Should().BeTrue();
            JObject.Parse("false").AsBoolean().Should().BeFalse();
            JObject.Parse("\"hello world\"").AsString().Should().Be("hello world");
            JObject.Parse("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\"").AsString().Should().Be("\"\\/\b\f\n\r\t");
            JObject.Parse("\"\\u0030\"").AsString().Should().Be("0");
            JObject.Parse("{\"k1\":\"v1\"}", 100).ToString().Should().Be("{\"k1\":\"v1\"}");
        }

        [TestMethod]
        public void TestTryGetEnum()
        {
            alice.TryGetEnum<Woo>().Should().Be(Woo.Tom);
        }

        [TestMethod]
        public void TestOpImplicitEnum()
        {
            var obj = new JObject();
            obj = Woo.Tom;
            obj.AsString().Should().Be("Tom");
        }

        [TestMethod]
        public void TestOpImplicitString()
        {
            var obj = new JObject();
            obj = null;
            obj.Should().BeNull();

            obj = "{\"aaa\":\"111\"}";
            obj.AsString().Should().Be("{\"aaa\":\"111\"}");
        }

        [TestMethod]
        public void TestGetNull()
        {
            JObject.Null.Should().BeNull();
        }

        [TestMethod]
        public void TestClone()
        {
            var bobClone = bob.Clone();
            bobClone.Should().NotBeSameAs(bob);
            foreach (var key in bobClone.Properties.Keys)
            {
                bobClone[key].Should().BeEquivalentTo(bob[key]);
            }
        }
    }
}
