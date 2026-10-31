// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ScriptBuilder_EmitPushParameter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.UnitTests.Extensions
{
    [TestClass]
    public class UT_ScriptBuilder_EmitPushParameter
    {
        [TestMethod]
        public void EmitPush_ContractParameter_NullValue_PushesNull()
        {
            using var sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter(ContractParameterType.Integer) { Value = null });
            Assert.AreEqual((byte)OpCode.PUSHNULL, sb.ToArray()[0]);
        }

        [TestMethod]
        public void EmitPush_ContractParameter_Primitives()
        {
            using var sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter { Type = ContractParameterType.Boolean, Value = true });
            sb.EmitPush(new ContractParameter { Type = ContractParameterType.Integer, Value = (BigInteger)7 });
            sb.EmitPush(new ContractParameter { Type = ContractParameterType.String, Value = "neo" });
            Assert.IsTrue(sb.ToArray().Length > 3);
        }

        [TestMethod]
        public void EmitPush_ContractParameter_ArrayAndMap()
        {
            using var sb = new ScriptBuilder();
            sb.EmitPush(new ContractParameter
            {
                Type = ContractParameterType.Array,
                Value = new List<ContractParameter>
                {
                    new() { Type = ContractParameterType.Integer, Value = (BigInteger)1 }
                }
            });
            Assert.AreEqual((byte)OpCode.PACK, sb.ToArray()[^1]);

            using var sbMap = new ScriptBuilder();
            sbMap.EmitPush(new ContractParameter
            {
                Type = ContractParameterType.Map,
                Value = new List<KeyValuePair<ContractParameter, ContractParameter>>()
            });
            Assert.AreEqual((byte)OpCode.NEWMAP, sbMap.ToArray()[0]);
        }

        [TestMethod]
        public void EmitPush_Object_Null_And_Unsupported()
        {
            using var sb = new ScriptBuilder();
            object obj = null;
            sb.EmitPush(obj);
            Assert.AreEqual((byte)OpCode.PUSHNULL, sb.ToArray()[0]);

            using var sb2 = new ScriptBuilder();
            Assert.ThrowsExactly<ArgumentException>(() => sb2.EmitPush(new object()));
        }

        [TestMethod]
        public void EmitSysCall_PushesArgsThenSyscall()
        {
            using var sb = new ScriptBuilder();
            sb.EmitSysCall(0x12345678, 1, "x");
            var bytes = sb.ToArray();
            Assert.IsTrue(bytes.Length > 5);
            Assert.AreEqual((byte)OpCode.SYSCALL, bytes[^5]);
        }

        [TestMethod]
        public void EmitPush_ISerializable()
        {
            using var sb = new ScriptBuilder();
            sb.EmitPush(UInt160.Zero);
            Assert.IsTrue(sb.ToArray().Length > 1);
        }
    }
}
