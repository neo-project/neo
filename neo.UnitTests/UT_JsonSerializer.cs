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
        public void JsonTest_WrongJson()
        {
            var json = "[    ]XXXXXXX";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "{   }XXXXXXX";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[,,,,]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "false,X";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "false@@@";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = @"{""length"":99999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999}";
            Assert.ThrowsException<OverflowException>(() => JObject.Parse(json));

            json = $"{{\"length\":{long.MaxValue}}}";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));
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
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));
        }

        [TestMethod]
        public void JsonTest_Numbers()
        {
            var json = "[  1, -2 , 3.5 ]";
            var parsed = JObject.Parse(json);

            Assert.AreEqual("[1,-2,3.5]", parsed.ToString());

            json = "[200.500000E+005,200.500000e+5,-1.1234e-100]";
            parsed = JObject.Parse(json);

            Assert.AreEqual("[20050000,20050000,-1.1234E-100]", parsed.ToString());

            json = "[-]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[1.]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[.123]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[--1.123]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[+1.123]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[1.12.3]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[e--1]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[e++1]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[E- 1]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[3e--1]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[2e++1]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = "[1E- 1]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));
        }

        [TestMethod]
        public void JsonTest_String()
        {
            var json = @" ["""" ,  ""\b\f\t\n\r\/\\"" ]";
            var parsed = JObject.Parse(json);

            Assert.AreEqual(@"["""",""\b\f\t\n\r\/\\""]", parsed.ToString());

            json = @"[""\uD834\uDD1E""]";
            parsed = JObject.Parse(json);

            Assert.AreEqual(json, parsed.ToString());

            json = @"[""\\x00""]";
            parsed = JObject.Parse(json);

            Assert.AreEqual(json, parsed.ToString());

            json = @"[""]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = @"[""\uaaa""]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = @"[""\uaa""]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = @"[""\ua""]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = @"[""\u""]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));
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
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));

            json = @"[""a"":]";
            Assert.ThrowsException<FormatException>(() => JObject.Parse(json));
        }

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
