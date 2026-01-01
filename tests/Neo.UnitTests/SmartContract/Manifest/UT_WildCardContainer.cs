// Copyright (C) 2015-2026 The Neo Project.
//
// UT_WildCardContainer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract.Manifest;
using System.Collections;

namespace Neo.UnitTests.SmartContract.Manifest;

[TestClass]
public class UT_WildCardContainer
{
    [TestMethod]
    public void TestFromJson()
    {
        var jstring = new JString("*");
        var s = WildcardContainer<string>.FromJson(jstring, u => u.AsString());
        Assert.IsTrue(s.IsWildcard);
        Assert.IsEmpty(s);

        jstring = new JString("hello world");
        Assert.ThrowsExactly<FormatException>(() => _ = WildcardContainer<string>.FromJson(jstring, u => u.AsString()));

        var alice = new JObject()
        {
            ["name"] = "alice",
            ["age"] = 30
        };
        var jarray = new JArray { alice };
        var r = WildcardContainer<string>.FromJson(jarray, u => u.AsString());
        Assert.AreEqual("{\"name\":\"alice\",\"age\":30}", r[0]);

        var jbool = new JBoolean();
        Assert.ThrowsExactly<FormatException>(() => _ = WildcardContainer<string>.FromJson(jbool, u => u.AsString()));
    }

    [TestMethod]
    public void TestGetCount()
    {
        string[]? s = ["hello", "world"];
        var container = WildcardContainer<string>.Create(s);
        Assert.HasCount(2, container);

        container = WildcardContainer<string>.CreateWildcard();
        Assert.IsEmpty(container);
    }

    [TestMethod]
    public void TestGetItem()
    {
        string[] s = ["hello", "world"];
        WildcardContainer<string> container = WildcardContainer<string>.Create(s);
        Assert.AreEqual("hello", container[0]);
        Assert.AreEqual("world", container[1]);
    }

    [TestMethod]
    public void TestGetEnumerator()
    {
        WildcardContainer<string> container = WildcardContainer<string>.CreateWildcard();
        IEnumerator<string> enumerator = container.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());

        string[] s = ["hello", "world"];
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
        string[] s = ["hello", "world"];
        var container = WildcardContainer<string>.Create(s);
        IEnumerable enumerable = container;
        var enumerator = enumerable.GetEnumerator();
        foreach (string _ in s)
        {
            enumerator.MoveNext();
            Assert.AreEqual(_, enumerator.Current);
        }
    }
}
