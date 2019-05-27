using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_JsonSerializer
    {
        [TestMethod]
        public void Deserialize_WrongJson()
        {
            Assert.ThrowsException<FormatException>(() => JsonSerializer.Deserialize(JObject.Parse("x")));
        }

        [TestMethod]
        public void Serialize_WrongJson()
        {
            Assert.ThrowsException<FormatException>(() => JsonSerializer.Serialize(StackItem.FromInterface(new object())));
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
            var entry = new VM.Types.Array { 1, 9007199254740992 };
            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "[1,\"9007199254740992\"]");
        }

        [TestMethod]
        public void Deserialize_EmptyObject()
        {
            var items = JsonSerializer.Deserialize(JObject.Parse("{}"));

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.AreEqual(((Map)items).Count, 0);
        }

        [TestMethod]
        public void Serialize_EmptyArray()
        {
            var entry = new VM.Types.Array();
            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "[]");
        }

        [TestMethod]
        public void Deserialize_EmptyArray()
        {
            var items = JsonSerializer.Deserialize(JObject.Parse("[]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 0);
        }

        [TestMethod]
        public void Serialize_Map_Test()
        {
            var entry = new Map
            {
                ["test"] = 123
            };

            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "{\"test\":123}");
        }

        [TestMethod]
        public void Deserialize_Map_Test()
        {
            var items = JsonSerializer.Deserialize(JObject.Parse("{\"test\":123}"));

            Assert.IsInstanceOfType(items, typeof(Map));
            Assert.AreEqual(((Map)items).Count, 1);

            var map = (Map)items;

            Assert.IsTrue(map.TryGetValue("test", out var value));
            Assert.AreEqual(value.GetBigInteger(), 123);
        }

        [TestMethod]
        public void Serialize_Array_Bool_Str_Num()
        {
            var entry = new VM.Types.Array { true, "test", 123 };

            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "[true,\"test\",123]");
        }

        [TestMethod]
        public void Deserialize_Array_Bool_Str_Num()
        {
            var items = JsonSerializer.Deserialize(JObject.Parse("[true,\"test\",123]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 3);

            var array = (VM.Types.Array)items;

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual(array[1].GetString(), "test");
            Assert.AreEqual(array[2].GetBigInteger(), 123);
        }

        [TestMethod]
        public void Serialize_Array_OfArray()
        {
            var entry = new VM.Types.Array
            {
                new VM.Types.Array { true, "test", 123 }
            };

            var json = JsonSerializer.Serialize(entry).ToString();

            Assert.AreEqual(json, "[[true,\"test\",123]]");
        }

        [TestMethod]
        public void Deserialize_Array_OfArray()
        {
            var items = JsonSerializer.Deserialize(JObject.Parse("[[true,\"test\",123]]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 1);

            var array = (VM.Types.Array)items;

            Assert.IsInstanceOfType(array[0], typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)array[0]).Count, 3);

            array = (VM.Types.Array)array[0];

            Assert.IsTrue(array[0].GetBoolean());
            Assert.AreEqual(array[1].GetString(), "test");
            Assert.AreEqual(array[2].GetBigInteger(), 123);
        }
    }
}
