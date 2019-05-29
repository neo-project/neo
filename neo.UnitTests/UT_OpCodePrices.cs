using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_OpCodePrices
    {
        [TestMethod]
        public void AllOpcodePriceAreSet()
        {
            foreach (OpCode opcode in Enum.GetValues(typeof(OpCode)))
                Assert.IsTrue(GasControl.OpCodePrices.ContainsKey(opcode));
        }
    }
}
