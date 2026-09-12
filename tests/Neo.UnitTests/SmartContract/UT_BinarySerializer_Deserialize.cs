// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BinarySerializer_Deserialize.cs file belongs to the neo project and is free
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
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    /// <summary>
    /// Deserialize / round-trip cases not covered by UT_BinarySerializer.TestSerialize.
    /// </summary>
    [TestClass]
    public class UT_BinarySerializer_Deserialize
    {
        [TestMethod]
        public void RoundTrip_Null_Boolean_Integer_ByteString()
        {
            Assert.IsTrue(RoundTrip(StackItem.Null).IsNull);
            Assert.IsTrue(RoundTrip(StackItem.True).GetBoolean());
            Assert.IsFalse(RoundTrip(StackItem.False).GetBoolean());
            Assert.AreEqual(123, (int)RoundTrip(new Integer(123)).GetInteger());
            Assert.AreEqual("neo", RoundTrip((ByteString)"neo").GetString());
        }

        [TestMethod]
        public void RoundTrip_Array_And_Map()
        {
            var array = new Array { 1, StackItem.Null, "x" };
            var array2 = (Array)RoundTrip(array);
            Assert.HasCount(3, array2);
            Assert.AreEqual(1, (int)array2[0].GetInteger());
            Assert.IsTrue(array2[1].IsNull);
            Assert.AreEqual("x", array2[2].GetString());

            var map = new Map { [(Integer)1] = (ByteString)"a" };
            var map2 = (Map)RoundTrip(map);
            Assert.HasCount(1, map2);
            Assert.AreEqual("a", map2[1].GetString());
        }

        [TestMethod]
        public void Deserialize_InvalidType_Throws()
        {
            Assert.ThrowsExactly<FormatException>(() =>
                BinarySerializer.Deserialize(new byte[] { 0xFF }, ExecutionEngineLimits.Default));
        }

        [TestMethod]
        public void Deserialize_Empty_Throws()
        {
            Assert.ThrowsExactly<FormatException>(() =>
                BinarySerializer.Deserialize(ReadOnlyMemory<byte>.Empty, ExecutionEngineLimits.Default));
        }

        private static StackItem RoundTrip(StackItem item)
        {
            var bytes = BinarySerializer.Serialize(item, ExecutionEngineLimits.Default);
            return BinarySerializer.Deserialize(bytes, ExecutionEngineLimits.Default);
        }
    }
}
