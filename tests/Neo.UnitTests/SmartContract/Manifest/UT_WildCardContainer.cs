// Copyright (C) 2015-2024 The Neo Project.
//
// UT_WildCardContainer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
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
            WildcardContainer<string> s = WildcardContainer<string>.FromJson(jstring, u => u.AsString());
            s.Should().BeEmpty();

            jstring = new JString("hello world");
            Action action = () => WildcardContainer<string>.FromJson(jstring, u => u.AsString());
            action.Should().Throw<FormatException>();

            JObject alice = new JObject();
            alice["name"] = "alice";
            alice["age"] = 30;
            JArray jarray = new JArray { alice };
            WildcardContainer<string> r = WildcardContainer<string>.FromJson(jarray, u => u.AsString());
            r[0].Should().Be("{\"name\":\"alice\",\"age\":30}");

            JBoolean jbool = new JBoolean();
            action = () => WildcardContainer<string>.FromJson(jbool, u => u.AsString());
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestGetCount()
        {
            string[] s = new string[] { "hello", "world" };
            WildcardContainer<string> container = WildcardContainer<string>.Create(s);
            container.Count.Should().Be(2);

            s = null;
            container = WildcardContainer<string>.Create(s);
            container.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestGetItem()
        {
            string[] s = new string[] { "hello", "world" };
            WildcardContainer<string> container = WildcardContainer<string>.Create(s);
            container[0].Should().Be("hello");
            container[1].Should().Be("world");
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            string[] s = null;
            IReadOnlyList<string> rs = new string[0];
            WildcardContainer<string> container = WildcardContainer<string>.Create(s);
            IEnumerator<string> enumerator = container.GetEnumerator();
            enumerator.Should().Be(rs.GetEnumerator());

            s = new string[] { "hello", "world" };
            container = WildcardContainer<string>.Create(s);
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
            WildcardContainer<string> container = WildcardContainer<string>.Create(s);
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
