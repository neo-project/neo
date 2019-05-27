using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.VM;
using System;
using System.Numerics;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_JsonParser
    {
        [TestMethod]
        public void Deserialize_WrongJson()
        {
            Assert.ThrowsException<FormatException>(() => JsonParser.Deserialize(JObject.Parse("x")));
        }

        [TestMethod]
        public void Serialize_WrongJson()
        {
            Assert.ThrowsException<FormatException>(() => JsonParser.Serialize(VM.Types.InteropInterface.FromInterface<object>(new object())));
        }

        [TestMethod]
        public void Serialize_EmptyObject()
        {
            var entry = new VM.Types.Map();
            var json = JsonParser.Serialize(entry).ToString();

            Assert.AreEqual(json, "{}");
        }

        [TestMethod]
        public void Serialize_Number()
        {
            var entry = new VM.Types.Array()
            {
                new VM.Types.Integer(1),
                new VM.Types.Integer(new BigInteger(double.MaxValue)+1),
            };
            var json = JsonParser.Serialize(entry).ToString();

            Assert.AreEqual(json, "[1,\"179769313486231570814527423731704356798070567525844996598917476803157260780028538760589558632766878171540458953514382464234321326889464182768467546703537516986049910576551282076245490090389328944075868508455133942304583236903222948165808559332123348274797826204144723168738177180919299881250404026184124858369\"]");
        }

        [TestMethod]
        public void Deserialize_EmptyObject()
        {
            var items = JsonParser.Deserialize(JObject.Parse("{}"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Map));
            Assert.AreEqual(((VM.Types.Map)items).Count, 0);
        }

        [TestMethod]
        public void Serialize_EmptyArray()
        {
            var entry = new VM.Types.Array();
            var json = JsonParser.Serialize(entry).ToString();

            Assert.AreEqual(json, "[]");
        }

        [TestMethod]
        public void Deserialize_EmptyArray()
        {
            var items = JsonParser.Deserialize(JObject.Parse("[]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 0);
        }

        [TestMethod]
        public void Serialize_Map_Test()
        {
            var entry = new VM.Types.Map();
            entry[new VM.Types.ByteArray(Encoding.UTF8.GetBytes("test"))] = new VM.Types.Integer(123);

            var json = JsonParser.Serialize(entry).ToString();

            Assert.AreEqual(json, "{\"test\":123}");
        }

        [TestMethod]
        public void Deserialize_Map_Test()
        {
            var items = JsonParser.Deserialize(JObject.Parse("{\"test\":123}"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Map));
            Assert.AreEqual(((VM.Types.Map)items).Count, 1);

            var map = (VM.Types.Map)items;

            Assert.IsTrue(map.TryGetValue(new VM.Types.ByteArray(Encoding.UTF8.GetBytes("test")), out var value));
            Assert.AreEqual(((VM.Types.Integer)value).GetBigInteger(), 123);
        }

        [TestMethod]
        public void Serialize_Array_Bool_Str_Num()
        {
            var entry = new VM.Types.Array
            {
                new VM.Types.Boolean(true),
                new VM.Types.ByteArray(Encoding.UTF8.GetBytes("test")),
                new VM.Types.Integer(123)
            };

            var json = JsonParser.Serialize(entry).ToString();

            Assert.AreEqual(json, "[true,\"test\",123]");
        }

        [TestMethod]
        public void Deserialize_Array_Bool_Str_Num()
        {
            var items = JsonParser.Deserialize(JObject.Parse("[true,\"test\",123]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 3);

            var array = (VM.Types.Array)items;

            Assert.IsTrue(((VM.Types.Boolean)array[0]).GetBoolean());
            Assert.AreEqual(((VM.Types.ByteArray)array[1]).GetString(), "test");
            Assert.AreEqual(((VM.Types.Integer)array[2]).GetBigInteger(), 123);
        }

        [TestMethod]
        public void Serialize_Array_OfArray()
        {
            var entry = new VM.Types.Array
            {
                new VM.Types.Array
                {
                    new VM.Types.Boolean(true),
                    new VM.Types.ByteArray(Encoding.UTF8.GetBytes("test")),
                    new VM.Types.Integer(123)
                }
            };

            var json = JsonParser.Serialize(entry).ToString();

            Assert.AreEqual(json, "[[true,\"test\",123]]");
        }

        [TestMethod]
        public void Deserialize_Array_OfArray()
        {
            var items = JsonParser.Deserialize(JObject.Parse("[[true,\"test\",123]]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 1);

            var array = (VM.Types.Array)items;

            Assert.IsInstanceOfType(array[0], typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)array[0]).Count, 3);

            array = (VM.Types.Array)array[0];

            Assert.IsTrue(((VM.Types.Boolean)array[0]).GetBoolean());
            Assert.AreEqual(((VM.Types.ByteArray)array[1]).GetString(), "test");
            Assert.AreEqual(((VM.Types.Integer)array[2]).GetBigInteger(), 123);
        }
    }
}