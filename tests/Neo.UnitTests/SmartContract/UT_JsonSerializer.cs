// Copyright (C) 2015-2026 The Neo Project.
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
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
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

            // repeat "9" 974 times
            var longNumber = string.Concat(Enumerable.Repeat("9", 974));
            json = $"{{\"length\":{longNumber}}}";
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

            json = "[200.500000E+005,200.500000e+5,-1.1234e-100,9.05E+28,1e3,10e3,1000e-3]";
            parsed = JObject.Parse(json);

            // Default parse: double semantics (pre-HF_Huyao / exactIntegers: false).
            Assert.AreEqual("[20050000,20050000,-1.1234E-100,9.05E+28,1000,10000,1]", parsed.ToString());

            // exactIntegers: integer-valued scientific tokens keep full precision.
            parsed = JToken.Parse(json, exactIntegers: true);
            Assert.AreEqual("[20050000,20050000,-1.1234E-100,90500000000000000000000000000,1000,10000,1]", parsed.ToString());

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

            json = @"{""a"":1,""a"":2}";
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
        public void Deserialize_EmptyObject()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("{}"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.IsEmpty((Map)items);
        }

        [TestMethod]
        public void Deserialize_EmptyArray()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("[]"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.IsEmpty((Array)items);
        }

        [TestMethod]
        public void Deserialize_Map_Test()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("{\"test1\":123,\"test2\":321}"), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.HasCount(2, (Map)items);

            var map = (Map)items;

            Assert.IsTrue(map.TryGetValue("test1", out var value));
            Assert.AreEqual(123, value.GetInteger());

            Assert.IsTrue(map.TryGetValue("test2", out value));
            Assert.AreEqual(321, value.GetInteger());

            Assert.AreSequenceEqual(map.Values.Select(u => u.GetInteger()).ToArray(), new BigInteger[] { 123, 321 });
        }

        [TestMethod]
        public void Deserialize_Array_Bool_Str_Num()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            // exactIntegers + HF_Huyao (enabled in Default via EnsureOmmitedHardforks at height 0)
            var items = JsonSerializer.Deserialize(engine, JToken.Parse("[true,\"test\",123,9.05E+28]", exactIntegers: true), ExecutionEngineLimits.Default);

            Assert.IsInstanceOfType(items, typeof(Array));
            Assert.HasCount(4, (Array)items);

            var array = (Array)items;

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual("test", array[1].GetString());
            Assert.AreEqual(123, array[2].GetInteger());
            // Exact integer from scientific notation (9.05E+28), not the imprecise double cast.
            Assert.AreEqual(BigInteger.Parse("90500000000000000000000000000"), array[3].GetInteger());
        }

        [TestMethod]
        public void Deserialize_ExactBigInteger_OnlyUnderHuyao()
        {
            const long unsafeInt = 9007199254740993L; // 2^53+1 — not exact as double
            var exactJson = JToken.Parse($"[{unsafeInt}]", exactIntegers: true)!;
            Assert.IsTrue(((JNumber)((JArray)exactJson)[0]!).HasExactBigInteger);

            // Pre-Huyao: exact storage is ignored; historical double/Basilisk path applies.
            var snapshot = _snapshotCache.CloneCache();
            var preSettings = ProtocolSettings.Default with
            {
                Hardforks = Enum.GetValues<Hardfork>()
                    .Where(hf => hf < Hardfork.HF_Huyao)
                    .ToDictionary(hf => hf, _ => 0u)
                    .ToImmutableDictionary()
            };
            var preEngine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, preSettings);
            Assert.IsFalse(preEngine.IsHardforkEnabled(Hardfork.HF_Huyao));
            var preItems = (Array)JsonSerializer.Deserialize(preEngine, exactJson, ExecutionEngineLimits.Default);
            Assert.AreNotEqual(new BigInteger(unsafeInt), preItems[0].GetInteger());

            // Post-Huyao: only exact-storage numbers take the new path.
            var postEngine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            Assert.IsTrue(postEngine.IsHardforkEnabled(Hardfork.HF_Huyao));
            var postItems = (Array)JsonSerializer.Deserialize(postEngine, exactJson, ExecutionEngineLimits.Default);
            Assert.AreEqual(new BigInteger(unsafeInt), postItems[0].GetInteger());

            // Double-backed numbers still use the historical path even after Huyao.
            var doubleJson = JToken.Parse("[42]")!; // exactIntegers: false (default)
            Assert.IsFalse(((JNumber)((JArray)doubleJson)[0]!).HasExactBigInteger);
            var doubleItems = (Array)JsonSerializer.Deserialize(postEngine, doubleJson, ExecutionEngineLimits.Default);
            Assert.AreEqual(42, doubleItems[0].GetInteger());
        }

        [TestMethod]
        public void Deserialize_Array_OfArray()
        {
            var snapshot = _snapshotCache.CloneCache();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
            var items = JsonSerializer.Deserialize(engine, JObject.Parse("[[true,\"test1\",123],[true,\"test2\",321]]"), ExecutionEngineLimits.Default);

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
