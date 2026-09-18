// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine.PriceTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void PriceTableOverrideIsIsolatedAndNullRestoresStaticPricing()
        {
            byte[] script = [(byte)OpCode.PUSH1];
            using var first = CreateHuyaoGatingEngine(true, script);
            using var second = CreateHuyaoGatingEngine(true, script);
            first.DynamicPriceTable[OpCode.PUSH1] = _ => 5000;
            using var third = CreateHuyaoGatingEngine(true, script);
            Assert.AreEqual(1500000000L, first.OpcodeV1(300000, OpCode.PUSH1, new RunStats()));
            Assert.IsNull(second.DynamicPriceTable[OpCode.PUSH1]);
            Assert.IsNull(third.DynamicPriceTable[OpCode.PUSH1]);
            first.DynamicPriceTable[OpCode.PUSH1] = null;
            Assert.AreEqual(656100000L, first.OpcodeV1(300000, OpCode.PUSH1, new RunStats()));
            Assert.AreEqual(VMState.HALT, first.Execute());
            Assert.AreEqual(VMState.HALT, second.Execute());
            Assert.AreEqual(VMState.HALT, third.Execute());
            Assert.AreEqual(66L, first.FeeConsumed);
            Assert.AreEqual(66L, second.FeeConsumed);
            Assert.AreEqual(66L, third.FeeConsumed);
        }
    }
}
