// Copyright (C) 2015-2025 The Neo Project.
//
// UT_JsonSerializer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable JSON001

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_JsonSerializer
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void JsonTest_WrongJson()
        {
            var json = "[    ]XXXXXXX";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "{   }XXXXXXX";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[,,,,]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "false,X";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "false@@@";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            // repeat "9" 974 times
            var longNumber = string.Concat(Enumerable.Repeat("9", 974));
            json = $"{{\"length\":{longNumber}}}";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));
        }

        [TestMethod]
        public void JsonTest_Array()
        {
            var json = "[    ]";
            var parsed = JsonNode.Parse(json);

            Assert.AreEqual("[]", parsed.StrictToString(false));

            json = "[1,\"a==\",    -1.3 ,null] ";
            parsed = JsonNode.Parse(json);

            Assert.AreEqual("[1,\"a==\",-1.3,null]", parsed.StrictToString(false));
        }

        [TestMethod]
        public void JsonTest_Bool()
        {
            var json = "[  true ,false ]";
            var parsed = JsonNode.Parse(json);

            Assert.AreEqual("[true,false]", parsed.StrictToString(false));

            json = "[True,FALSE] ";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));
        }

        [TestMethod]
        public void JsonTest_Numbers()
        {
            var json = "[  1, -2 , 3.5 ]";
            var parsed = JsonNode.Parse(json);

            Assert.AreEqual("[1,-2,3.5]", parsed.StrictToString(false));

            json = "[200.500000E+005,200.500000e+5,-1.1234e-100,9.05E+28]";
            parsed = JsonNode.Parse(json);

            Assert.AreEqual("[20050000,20050000,-1.1234E-100,9.05E+28]", parsed.StrictToString(false));

            json = "[-]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[1.]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[.123]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[--1.123]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[+1.123]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[1.12.3]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[e--1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[e++1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[E- 1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[3e--1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[2e++1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = "[1E- 1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));
        }

        [TestMethod]
        public void JsonTest_String()
        {
            var json = @" ["""" ,  ""\b\f\t\n\r\/\\"" ]";
            var parsed = JsonNode.Parse(json);

            Assert.AreEqual(@"["""",""\b\f\t\n\r/\\""]", parsed.StrictToString(false));

            json = @"[""\uD834\uDD1E""]";
            parsed = JsonNode.Parse(json);

            Assert.AreEqual(json, parsed.StrictToString(false));

            json = @"[""\\x00""]";
            parsed = JsonNode.Parse(json);

            Assert.AreEqual(json, parsed.StrictToString(false));

            json = @"[""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = @"[""\uaaa""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = @"[""\uaa""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = @"[""\ua""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = @"[""\u""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));
        }

        [TestMethod]
        public void JsonTest_Object()
        {
            var json = @" {""test"":   true}";
            var parsed = JsonNode.Parse(json);

            Assert.AreEqual(@"{""test"":true}", parsed.StrictToString(false));

            json = @" {""\uAAAA"":   true}";
            parsed = JsonNode.Parse(json);

            Assert.AreEqual(@"{""\uAAAA"":true}", parsed.StrictToString(false));

            json = @"{""a"":}";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = @"{NULL}";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));

            json = @"[""a"":]";
            Assert.ThrowsExactly<FormatException>(() => _ = JsonNode.Parse(json));
        }

        [TestMethod]
        public void Deserialize_WrongJson()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            Assert.ThrowsExactly<FormatException>(() => _ = JsonSerializer.Deserialize(engine, JsonNode.Parse("x"), ExecutionEngineLimits.Default));
        }

        [TestMethod]
        public void Deserialize_EmptyObject()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            var items = JsonSerializer.Deserialize(engine, JsonNode.Parse("{}"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.IsEmpty((Map)items);
        }

        [TestMethod]
        public void Deserialize_EmptyArray()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            var items = JsonSerializer.Deserialize(engine, JsonNode.Parse("[]"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.IsEmpty((Array)items);
        }

        [TestMethod]
        public void Deserialize_Map_Test()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JsonNode.Parse("{\"test1\":123,\"test2\":321}"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.HasCount(2, (Map)items);

            var map = (Map)items;

            Assert.IsTrue(map.TryGetValue("test1", out var value));
            Assert.AreEqual(123, value.GetInteger());

            Assert.IsTrue(map.TryGetValue("test2", out value));
            Assert.AreEqual(321, value.GetInteger());

            CollectionAssert.AreEqual(map.Values.Select(u => u.GetInteger()).ToArray(), new BigInteger[] { 123, 321 });
        }

        [TestMethod]
        public void Deserialize_Array_Bool_Str_Num()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JsonNode.Parse("[true,\"test\",123,9.05E+28]"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.HasCount(4, (Array)items);

            var array = (Array)items;

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual("test", array[1].GetString());
            Assert.AreEqual(123, array[2].GetInteger());
            Assert.AreEqual(array[3].GetInteger(), BigInteger.Parse("90500000000000000000000000000"));
        }

        [TestMethod]
        public void Deserialize_Array_OfArray()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JsonNode.Parse("[[true,\"test1\",123],[true,\"test2\",321]]"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.HasCount(2, (Array)items);

            var array = (Array)items;

            Assert.IsInstanceOfType(array[0], typeof(Array));
            Assert.HasCount(3, (Array)array[0]);

            array = (Array)array[0];
            Assert.HasCount(3, array);

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual("test1", array[1].GetString());
            Assert.AreEqual(123, array[2].GetInteger());

            array = (Array)items;
            array = (Array)array[1];
            Assert.HasCount(3, array);

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual("test2", array[1].GetString());
            Assert.AreEqual(321, array[2].GetInteger());
        }
    }
}
