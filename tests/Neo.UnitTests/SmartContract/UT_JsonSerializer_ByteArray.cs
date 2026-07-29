// Copyright (C) 2015-2026 The Neo Project.
//
// UT_JsonSerializer_ByteArray.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Numerics;
using System.Text;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Coverage for <see cref="JsonSerializer.SerializeToByteArray"/> (not covered by UT_JsonSerializer).
    /// </summary>
    [TestClass]
    public class UT_JsonSerializer_ByteArray
    {
        [TestMethod]
        public void SerializeToByteArray_Primitives()
        {
            var nullJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToByteArray(StackItem.Null, 64));
            Assert.AreEqual("null", nullJson);

            var trueJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToByteArray(StackItem.True, 64));
            Assert.AreEqual("true", trueJson);

            var falseJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToByteArray(StackItem.False, 64));
            Assert.AreEqual("false", falseJson);

            var numJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToByteArray(new Integer(42), 64));
            Assert.AreEqual("42", numJson);

            var strJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToByteArray((ByteString)"hi", 64));
            Assert.AreEqual("\"hi\"", strJson);
        }

        [TestMethod]
        public void SerializeToByteArray_Array_And_Map()
        {
            var array = new Array { new Integer(1), StackItem.Null, StackItem.True };
            var arrayJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToByteArray(array, 256));
            Assert.AreEqual("[1,null,true]", arrayJson);

            var map = new Map
            {
                [(ByteString)"a"] = new Integer(1),
                [(ByteString)"b"] = StackItem.False
            };
            var mapJson = Encoding.UTF8.GetString(JsonSerializer.SerializeToByteArray(map, 256));
            Assert.IsTrue(mapJson.Contains("\"a\":1"));
            Assert.IsTrue(mapJson.Contains("\"b\":false"));
            Assert.IsTrue(mapJson.StartsWith('{') && mapJson.EndsWith('}'));
        }

        [TestMethod]
        public void SerializeToByteArray_IntegerTooLarge_Throws()
        {
            var huge = new Integer(BigInteger.Parse("9007199254740993")); // > MAX_SAFE_INTEGER
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                JsonSerializer.SerializeToByteArray(huge, 256));
        }

        [TestMethod]
        public void SerializeToByteArray_ExceedsMaxSize_Throws()
        {
            var item = (ByteString)"hello";
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                JsonSerializer.SerializeToByteArray(item, 2));
        }

        [TestMethod]
        public void SerializeToByteArray_MapKeyNotByteString_Throws()
        {
            var map = new Map
            {
                [new Integer(1)] = StackItem.True
            };
            Assert.ThrowsExactly<FormatException>(() =>
                JsonSerializer.SerializeToByteArray(map, 256));
        }
    }
}
