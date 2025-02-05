// Copyright (C) 2015-2025 The Neo Project.
//
// UT_WildCardContainer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            var s = WildcardContainer<string>.FromJson(jstring, u => u.AsString());
            Assert.IsTrue(s.IsWildcard);
            Assert.AreEqual(0, s.Count);

            jstring = new JString("hello world");
            Assert.ThrowsException<FormatException>(() => WildcardContainer<string>.FromJson(jstring, u => u.AsString()));

            JObject alice = new JObject();
            alice["name"] = "alice";
            alice["age"] = 30;
            JArray jarray = new JArray { alice };
            WildcardContainer<string> r = WildcardContainer<string>.FromJson(jarray, u => u.AsString());
            Assert.AreEqual("{\"name\":\"alice\",\"age\":30}", r[0]);

            JBoolean jbool = new JBoolean();
            Assert.ThrowsException<FormatException>(() => WildcardContainer<string>.FromJson(jbool, u => u.AsString()));
        }

        [TestMethod]
        public void TestGetCount()
        {
            string[] s = new string[] { "hello", "world" };
            WildcardContainer<string> container = WildcardContainer<string>.Create(s);
            Assert.AreEqual(2, container.Count);

            s = null;
            container = WildcardContainer<string>.Create(s);
            Assert.AreEqual(0, container.Count);
        }

        [TestMethod]
        public void TestGetItem()
        {
            string[] s = new string[] { "hello", "world" };
            WildcardContainer<string> container = WildcardContainer<string>.Create(s);
            Assert.AreEqual("hello", container[0]);
            Assert.AreEqual("world", container[1]);
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            string[] s = null;
            IReadOnlyList<string> rs = new string[0];
            WildcardContainer<string> container = WildcardContainer<string>.Create(s);
            IEnumerator<string> enumerator = container.GetEnumerator();
            Assert.AreEqual(rs.GetEnumerator(), enumerator);

            s = new string[] { "hello", "world" };
            container = WildcardContainer<string>.Create(s);
            enumerator = container.GetEnumerator();
            foreach (string _ in s)
            {
                enumerator.MoveNext();
                Assert.AreEqual(_, enumerator.Current);
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
                Assert.AreEqual(_, enumerator.Current);
            }
        }
    }
}
