using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using System;
using System.IO;
using System.Reflection;

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
            Action action = () => JObject.Parse(new StringReader(""), -1);
            action.ShouldThrow<FormatException>();
        }

        [TestMethod]
        public void TestParseNull()
        {
            Action action = () => JObject.Parse(new StringReader("naaa"));
            action.ShouldThrow<FormatException>();

            JObject.Parse(new StringReader("null")).Should().BeNull();
        }

        [TestMethod]
        public void TestParseObject()
        {
            Action action1 = () => JObject.Parse(new StringReader("{\"k1\":\"v1\",\"k1\":\"v2\"}"), 100);
            action1.ShouldThrow<FormatException>();

            Action action2 = () => JObject.Parse(new StringReader("{\"k1\",\"k1\"}"), 100);
            action2.ShouldThrow<FormatException>();

            Action action3 = () => JObject.Parse(new StringReader("{\"k1\":\"v1\""), 100);
            action3.ShouldThrow<FormatException>();

            Action action4 = () => JObject.Parse(new StringReader("aaa"), 100);
            action4.ShouldThrow<FormatException>();

            JObject.Parse(new StringReader("{\"k1\":\"v1\"}"), 100).ToString().Should().Be("{\"k1\":\"v1\"}");
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
    }
}