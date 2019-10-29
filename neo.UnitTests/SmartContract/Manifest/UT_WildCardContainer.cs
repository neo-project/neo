using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.SmartContract.Manifest;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_WildCardContainer
    {
        [TestMethod]
        public void TestFromJson()
        {
            JString jstring = new JString("*");
            WildCardContainer<string> s = WildCardContainer<string>.FromJson(jstring, u => u.AsString());
            s.Should().BeEmpty();

            jstring = new JString("hello world");
            Action action = () => WildCardContainer<string>.FromJson(jstring, u => u.AsString());
            action.Should().Throw<FormatException>();

            JObject alice = new JObject();
            alice["name"] = "alice";
            alice["age"] = 30;
            JArray jarray = new JArray { alice };
            WildCardContainer<string> r = WildCardContainer<string>.FromJson(jarray, u => u.AsString());
            r[0].Should().Be("{\"name\":\"alice\",\"age\":30}");

            JBoolean jbool = new JBoolean();
            action = () => WildCardContainer<string>.FromJson(jbool, u => u.AsString());
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestGetCount()
        {
            string[] s = new string[] { "hello", "world" };
            WildCardContainer<string> container = WildCardContainer<string>.Create(s);
            container.Count.Should().Be(2);

            s = null;
            container = WildCardContainer<string>.Create(s);
            container.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestGetItem()
        {
            string[] s = new string[] { "hello", "world" };
            WildCardContainer<string> container = WildCardContainer<string>.Create(s);
            container[0].Should().Be("hello");
            container[1].Should().Be("world");
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            string[] s = null;
            IReadOnlyList<string> rs = (IReadOnlyList<string>)new string[0];
            WildCardContainer<string> container = WildCardContainer<string>.Create(s);
            IEnumerator<string> enumerator = container.GetEnumerator();
            enumerator.Should().Be(rs.GetEnumerator());

            s = new string[] { "hello", "world" };
            container = WildCardContainer<string>.Create(s);
            enumerator = container.GetEnumerator();
            foreach (string _ in s)
            {
                enumerator.MoveNext();
                enumerator.Current.Should().Be(_);
            }
        }

        [TestMethod]
        public void TestIEnumerableGetEnumerator()
        {
            string[] s = new string[] { "hello", "world" };
            WildCardContainer<string> container = WildCardContainer<string>.Create(s);
            IEnumerable enumerable = container;
            var enumerator = enumerable.GetEnumerator();
            foreach (string _ in s)
            {
                enumerator.MoveNext();
                enumerator.Current.Should().Be(_);
            }
        }
    }
}
