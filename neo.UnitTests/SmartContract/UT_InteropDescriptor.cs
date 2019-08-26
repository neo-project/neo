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
            Func<ApplicationEngine, bool> handler = TestEngine;
            long price = 0_00000250;
            TriggerType allowedTriggers = TriggerType.All;
            InteropDescriptor descriptor = new InteropDescriptor(method, TestEngine, price, allowedTriggers);
            descriptor.Method.Should().Be(method);
        }

        private bool TestEngine(ApplicationEngine engine)
        {
            return true;
        }
    }
}
