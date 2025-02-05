// Copyright (C) 2015-2025 The Neo Project.
//
// UT_OpCodePrices.cs file belongs to the neo project and is free
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
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_OpCodePrices
    {
        [TestMethod]
        public void AllOpcodePriceAreSet()
        {
            foreach (OpCode opcode in Enum.GetValues(typeof(OpCode)))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.IsTrue(ApplicationEngine.OpCodePrices.ContainsKey(opcode), opcode.ToString(), $"{opcode} without price");
                Assert.AreEqual(ApplicationEngine.OpCodePrices[opcode], ApplicationEngine.OpCodePriceTable[(byte)opcode], $"{opcode} price mismatch");
#pragma warning restore CS0618 // Type or member is obsolete

                if (opcode == OpCode.RET ||
                    opcode == OpCode.SYSCALL ||
                    opcode == OpCode.ABORT ||
                    opcode == OpCode.ABORTMSG)
                    continue;

                Assert.AreNotEqual(0, ApplicationEngine.OpCodePriceTable[(byte)opcode], $"{opcode} without price");
            }
        }
    }
}
