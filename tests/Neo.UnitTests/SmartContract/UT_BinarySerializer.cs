// Copyright (C) 2015-2024 The Neo Project.
//
// UT_BinarySerializer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_BinarySerializer
    {
        [TestMethod]
        public void TestSerialize()
        {
            var result1 = BinarySerializer.Serialize(new byte[5], ExecutionEngineLimits.Default);
            var expectedArray1 = new byte[] {
                        0x28, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray1), Encoding.Default.GetString(result1));

            var result2 = BinarySerializer.Serialize(true, ExecutionEngineLimits.Default);
            var expectedArray2 = new byte[] {
                        0x20, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray2), Encoding.Default.GetString(result2));

            var result3 = BinarySerializer.Serialize(1, ExecutionEngineLimits.Default);
            var expectedArray3 = new byte[] {
                        0x21, 0x01, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray3), Encoding.Default.GetString(result3));

            StackItem stackItem4 = new InteropInterface(new object());
            Action action4 = () => BinarySerializer.Serialize(stackItem4, ExecutionEngineLimits.Default);
            action4.Should().Throw<NotSupportedException>();

            var list6 = new List<StackItem> { 1 };
            StackItem stackItem62 = new VM.Types.Array(list6);
            var result6 = BinarySerializer.Serialize(stackItem62, ExecutionEngineLimits.Default);
            var expectedArray6 = new byte[] {
                        0x40,0x01,0x21,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray6), Encoding.Default.GetString(result6));

            var list7 = new List<StackItem> { 1 };
            StackItem stackItem72 = new Struct(list7);
            var result7 = BinarySerializer.Serialize(stackItem72, ExecutionEngineLimits.Default);
            var expectedArray7 = new byte[] {
                        0x41,0x01,0x21,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray7), Encoding.Default.GetString(result7));

            StackItem stackItem82 = new Map { [2] = 1 };
            var result8 = BinarySerializer.Serialize(stackItem82, ExecutionEngineLimits.Default);
            var expectedArray8 = new byte[] {
                        0x48,0x01,0x21,0x01,0x02,0x21,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray8), Encoding.Default.GetString(result8));

            var stackItem91 = new Map();
            stackItem91[1] = stackItem91;
            Action action9 = () => BinarySerializer.Serialize(stackItem91, ExecutionEngineLimits.Default);
            action9.Should().Throw<NotSupportedException>();

            var stackItem10 = new VM.Types.Array();
            stackItem10.Add(stackItem10);
            Action action10 = () => BinarySerializer.Serialize(stackItem10, ExecutionEngineLimits.Default);
            action10.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void TestDeserializeStackItem()
        {
            StackItem stackItem1 = new ByteString(new byte[5]);
            var byteArray1 = BinarySerializer.Serialize(stackItem1, ExecutionEngineLimits.Default);
            var result1 = BinarySerializer.Deserialize(byteArray1, ExecutionEngineLimits.Default);
            Assert.AreEqual(stackItem1, result1);

            StackItem stackItem2 = StackItem.True;
            var byteArray2 = BinarySerializer.Serialize(stackItem2, ExecutionEngineLimits.Default);
            var result2 = BinarySerializer.Deserialize(byteArray2, ExecutionEngineLimits.Default);
            Assert.AreEqual(stackItem2, result2);

            StackItem stackItem3 = new Integer(1);
            var byteArray3 = BinarySerializer.Serialize(stackItem3, ExecutionEngineLimits.Default);
            var result3 = BinarySerializer.Deserialize(byteArray3, ExecutionEngineLimits.Default);
            Assert.AreEqual(stackItem3, result3);

            var byteArray4 = BinarySerializer.Serialize(1, ExecutionEngineLimits.Default);
            byteArray4[0] = 0x40;
            Action action4 = () => BinarySerializer.Deserialize(byteArray4, ExecutionEngineLimits.Default);
            action4.Should().Throw<FormatException>();

            var list5 = new List<StackItem> { 1 };
            StackItem stackItem52 = new VM.Types.Array(list5);
            var byteArray5 = BinarySerializer.Serialize(stackItem52, ExecutionEngineLimits.Default);
            var result5 = BinarySerializer.Deserialize(byteArray5, ExecutionEngineLimits.Default);
            Assert.AreEqual(((VM.Types.Array)stackItem52).Count, ((VM.Types.Array)result5).Count);
            Assert.AreEqual(((VM.Types.Array)stackItem52).GetEnumerator().Current, ((VM.Types.Array)result5).GetEnumerator().Current);

            var list6 = new List<StackItem> { 1 };
            StackItem stackItem62 = new Struct(list6);
            var byteArray6 = BinarySerializer.Serialize(stackItem62, ExecutionEngineLimits.Default);
            var result6 = BinarySerializer.Deserialize(byteArray6, ExecutionEngineLimits.Default);
            Assert.AreEqual(((Struct)stackItem62).Count, ((Struct)result6).Count);
            Assert.AreEqual(((Struct)stackItem62).GetEnumerator().Current, ((Struct)result6).GetEnumerator().Current);

            StackItem stackItem72 = new Map { [2] = 1 };
            var byteArray7 = BinarySerializer.Serialize(stackItem72, ExecutionEngineLimits.Default);
            var result7 = BinarySerializer.Deserialize(byteArray7, ExecutionEngineLimits.Default);
            Assert.AreEqual(((Map)stackItem72).Count, ((Map)result7).Count);
            CollectionAssert.AreEqual(((Map)stackItem72).Keys.ToArray(), ((Map)result7).Keys.ToArray());
            CollectionAssert.AreEqual(((Map)stackItem72).Values.ToArray(), ((Map)result7).Values.ToArray());
        }
    }
}
