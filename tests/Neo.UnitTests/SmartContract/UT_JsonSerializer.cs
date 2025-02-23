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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
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
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "{   }XXXXXXX";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[,,,,]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "false,X";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "false@@@";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = @"{""length"":99999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999}";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));
        }

        [TestMethod]
        public void JsonTest_Array()
        {
            var json = "[    ]";
            var parsed = JObject.Parse(json);

            Assert.AreEqual("[]", parsed.ToString());

            json = "[1,\"a==\",    -1.3 ,null] ";
            parsed = JObject.Parse(json);

            Assert.AreEqual("[1,\"a==\",-1.3,null]", parsed.ToString());
        }

        [TestMethod]
        public void JsonTest_Serialize_Map_Test()
        {
            var entry = new Map
            {
                [new byte[] { 0xC1 }] = 1,
                [new byte[] { 0xC2 }] = 2,
            };
            Assert.ThrowsExactly<DecoderFallbackException>(() => _ = JsonSerializer.Serialize(entry));
        }

        [TestMethod]
        public void JsonTest_Bool()
        {
            var json = "[  true ,false ]";
            var parsed = JObject.Parse(json);

            Assert.AreEqual("[true,false]", parsed.ToString());

            json = "[True,FALSE] ";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));
        }

        [TestMethod]
        public void JsonTest_Numbers()
        {
            var json = "[  1, -2 , 3.5 ]";
            var parsed = JObject.Parse(json);

            Assert.AreEqual("[1,-2,3.5]", parsed.ToString());

            json = "[200.500000E+005,200.500000e+5,-1.1234e-100,9.05E+28]";
            parsed = JObject.Parse(json);

            Assert.AreEqual("[20050000,20050000,-1.1234E-100,9.05E+28]", parsed.ToString());

            json = "[-]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[1.]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[.123]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[--1.123]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[+1.123]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[1.12.3]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[e--1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[e++1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[E- 1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[3e--1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[2e++1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = "[1E- 1]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));
        }

        [TestMethod]
        public void JsonTest_String()
        {
            var json = @" ["""" ,  ""\b\f\t\n\r\/\\"" ]";
            var parsed = JObject.Parse(json);

            Assert.AreEqual(@"["""",""\b\f\t\n\r/\\""]", parsed.ToString());

            json = @"[""\uD834\uDD1E""]";
            parsed = JObject.Parse(json);

            Assert.AreEqual(json, parsed.ToString());

            json = @"[""\\x00""]";
            parsed = JObject.Parse(json);

            Assert.AreEqual(json, parsed.ToString());

            json = @"[""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = @"[""\uaaa""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = @"[""\uaa""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = @"[""\ua""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = @"[""\u""]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));
        }

        [TestMethod]
        public void JsonTest_Object()
        {
            var json = @" {""test"":   true}";
            var parsed = JObject.Parse(json);

            Assert.AreEqual(@"{""test"":true}", parsed.ToString());

            json = @" {""\uAAAA"":   true}";
            parsed = JObject.Parse(json);

            Assert.AreEqual(@"{""\uAAAA"":true}", parsed.ToString());

            json = @"{""a"":}";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = @"{NULL}";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));

            json = @"[""a"":]";
            Assert.ThrowsExactly<FormatException>(() => _ = JObject.Parse(json));
        }

        [TestMethod]
        public void Deserialize_WrongJson()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            Assert.ThrowsExactly<FormatException>(() => _ = JsonSerializer.Deserialize(engine, JObject.Parse("x"), ExecutionEngineLimits.Default));
        }

        [TestMethod]
        public void Serialize_WrongJson()
        {
            Assert.ThrowsExactly<FormatException>(() => _ = JsonSerializer.Serialize(StackItem.FromInterface(new object())));
        }

        [TestMethod]
        public void Serialize_EmptyObject()
        {
            var entry = new Map();
            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "{}");
        }

        [TestMethod]
        public void Serialize_Number()
        {
            var entry = new Array { 1, 9007199254740992 };
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = JsonSerializer.Serialize(entry));
        }

        [TestMethod]
        public void Serialize_Null()
        {
            Assert.AreEqual(JObject.Null, JsonSerializer.Serialize(StackItem.Null));
        }

        [TestMethod]
        public void Deserialize_EmptyObject()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("{}"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.AreEqual(((Map)items).Count, 0);
        }

        [TestMethod]
        public void Serialize_EmptyArray()
        {
            var entry = new Array();
            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "[]");
        }

        [TestMethod]
        public void Deserialize_EmptyArray()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("[]"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.AreEqual(((Array)items).Count, 0);
        }

        [TestMethod]
        public void Serialize_Map_Test()
        {
            var entry = new Map
            {
                ["test1"] = 1,
                ["test3"] = 3,
                ["test2"] = 2
            };

            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "{\"test1\":1,\"test3\":3,\"test2\":2}");
        }

        [TestMethod]
        public void Deserialize_Map_Test()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("{\"test1\":123,\"test2\":321}"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.AreEqual(((Map)items).Count, 2);

            var map = (Map)items;

            Assert.IsTrue(map.TryGetValue("test1", out var value));
            Assert.AreEqual(value.GetInteger(), 123);

            Assert.IsTrue(map.TryGetValue("test2", out value));
            Assert.AreEqual(value.GetInteger(), 321);

            CollectionAssert.AreEqual(map.Values.Select(u => u.GetInteger()).ToArray(), new BigInteger[] { 123, 321 });
        }

        [TestMethod]
        public void Serialize_Array_Bool_Str_Num()
        {
            var entry = new Array { true, "test", 123 };

            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "[true,\"test\",123]");
        }

        [TestMethod]
        public void Deserialize_Array_Bool_Str_Num()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("[true,\"test\",123,9.05E+28]"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.AreEqual(((Array)items).Count, 4);

            var array = (Array)items;

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual(array[1].GetString(), "test");
            Assert.AreEqual(array[2].GetInteger(), 123);
            Assert.AreEqual(array[3].GetInteger(), BigInteger.Parse("90500000000000000000000000000"));
        }

        [TestMethod]
        public void Serialize_Array_OfArray()
        {
            var entry = new Array
            {
                new Array { true, "test1", 123 },
                new Array { true, "test2", 321 }
            };

            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "[[true,\"test1\",123],[true,\"test2\",321]]");
        }

        [TestMethod]
        public void Deserialize_Array_OfArray()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("[[true,\"test1\",123],[true,\"test2\",321]]"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.AreEqual(((Array)items).Count, 2);

            var array = (Array)items;

            Assert.IsInstanceOfType(array[0], typeof(Array));
            Assert.AreEqual(((Array)array[0]).Count, 3);

            array = (Array)array[0];
            Assert.AreEqual(array.Count, 3);

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual(array[1].GetString(), "test1");
            Assert.AreEqual(array[2].GetInteger(), 123);

            array = (Array)items;
            array = (Array)array[1];
            Assert.AreEqual(array.Count, 3);

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual(array[1].GetString(), "test2");
            Assert.AreEqual(array[2].GetInteger(), 321);
        }
    }
}
