using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.VM;
using System;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_JsonParser
    {
        [TestMethod]
        public void WrongJson()
        {
            Assert.ThrowsException<FormatException>(() => JsonParser.JsonToStackItem(JObject.Parse("x")));
        }

        [TestMethod]
        public void EmptyObject()
        {
            var items = JsonParser.JsonToStackItem(JObject.Parse("{}"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Map));
            Assert.AreEqual(((VM.Types.Map)items).Count, 0);
        }

        [TestMethod]
        public void EmptyArray()
        {
            var items = JsonParser.JsonToStackItem(JObject.Parse("[]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 0);
        }

        [TestMethod]
        public void Map_Test()
        {
            var items = JsonParser.JsonToStackItem(JObject.Parse("{\"test\":123}"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Map));
            Assert.AreEqual(((VM.Types.Map)items).Count, 1);

            var map = (VM.Types.Map)items;

            Assert.IsTrue(map.TryGetValue(new VM.Types.ByteArray(Encoding.UTF8.GetBytes("test")), out var value));
            Assert.AreEqual(((VM.Types.Integer)value).GetBigInteger(), 123);
        }

        [TestMethod]
        public void Array_Bool_Str_Num()
        {
            var items = JsonParser.JsonToStackItem(JObject.Parse("[true,\"test\",123]"));

            Assert.IsInstanceOfType(items, typeof(VM.Types.Array));
            Assert.AreEqual(((VM.Types.Array)items).Count, 3);

            var array = (VM.Types.Array)items;

            Assert.IsTrue(((VM.Types.Boolean)array[0]).GetBoolean());
            Assert.AreEqual(((VM.Types.ByteArray)array[1]).GetString(), "test");
            Assert.AreEqual(((VM.Types.Integer)array[2]).GetBigInteger(), 123);
        }

        [TestMethod]
        public void Array_OfArray()
        {
            var items = JsonParser.JsonToStackItem(JObject.Parse("[[true,\"test\",123]]"));

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