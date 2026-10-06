// Copyright (C) 2015-2026 The Neo Project.
//
// UT_StackItemKeepAlive.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.Reflection;
using Array = Neo.VM.Types.Array;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_StackItemKeepAlive
    {
        [TestMethod]
        public void Keep_CyclicArray_DoesNotStackOverflow()
        {
            var array = new Array();
            array.Add(array);
            StackItemKeepAlive.Keep(array);
            Assert.HasCount(1, array);
            Assert.AreSame(array, array[0]);
        }

        [TestMethod]
        public void Keep_CyclicMap_DoesNotStackOverflow()
        {
            var map = new Map();
            map[1] = map;
            StackItemKeepAlive.Keep(map);
            Assert.HasCount(1, map);
            Assert.AreSame(map, map[1]);
        }

        [TestMethod]
        public void Keep_DeeplyNestedArray_DoesNotStackOverflow()
        {
            StackItem item = new Buffer([(byte)0xA5]);
            for (var i = 0; i < ExecutionEngineLimits.Default.MaxStackSize; i++)
                item = new Array { item };

            StackItemKeepAlive.Keep(item);

            for (var i = 0; i < ExecutionEngineLimits.Default.MaxStackSize; i++)
                item = ((Array)item)[0];
            Assert.IsInstanceOfType<Buffer>(item);
            InvokeCleanup(item);
            Assert.AreEqual((byte)0xA5, item.GetSpan()[0]);
        }

        [TestMethod]
        public void Keep_Buffer_SurvivesCleanup()
        {
            var buffer = new Buffer([(byte)0xA5]);
            StackItemKeepAlive.Keep(buffer);
            InvokeCleanup(buffer);
            Assert.AreEqual((byte)0xA5, buffer.GetSpan()[0]);
        }

        [TestMethod]
        public void Keep_ByteString_DoesNotRequireCleanup()
        {
            var bytes = new ByteString(new byte[] { 0x3C, 0x7E });
            StackItemKeepAlive.Keep(bytes);
            Assert.AreEqual((byte)0x3C, bytes.GetSpan()[0]);
            Assert.AreEqual((byte)0x7E, bytes.GetSpan()[1]);
        }

        private static void InvokeCleanup(StackItem item)
        {
            var method = typeof(StackItem).GetMethod("Cleanup", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(item, null);
        }
    }
}
