using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropDescriptor
    {
        [TestMethod]
        public void TestGetMethod()
        {
            string method = @"System.ExecutionEngine.GetScriptContainer";
            long price = 0_00000250;
            TriggerType allowedTriggers = TriggerType.All;
            InteropDescriptor descriptor = new InteropDescriptor(method, TestHandler, price, allowedTriggers);
            descriptor.Method.Should().Be(method);
            descriptor.Price.Should().Be(price);
        }

        private bool TestHandler(ApplicationEngine engine)
        {
            return true;
        }
    }
}
